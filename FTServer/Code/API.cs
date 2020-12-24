using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using iBoxDB.LocalServer;
using static FTServer.App;
using System.Runtime.CompilerServices;

namespace FTServer
{

    public class IndexPage
    {

        public static void addSearchTerm(String keywords)
        {
            if (keywords.length() < PageSearchTerm.MAX_TERM_LENGTH)
            {
                PageSearchTerm pst = new PageSearchTerm();
                pst.time = DateTime.Now;
                pst.keywords = keywords;
                pst.uid = Guid.NewGuid();
                App.Item.Insert("/PageSearchTerm", pst);
            }
        }


        public static List<PageSearchTerm> getSearchTerm(int len)
        {
            return App.Item.Select<PageSearchTerm>("from /PageSearchTerm limit 0 , ?", len);
        }

        public static String getDesc(String str, KeyWord kw, int length)
        {
            if (kw.I == -1)
            {
                return str;
            }
            return IndexAPI.ENGINE.getDesc(str, kw, length);
        }
        public static List<String> discover()
        {
            using (var box = App.Auto.Cube())
            {
                return IndexAPI.engine.discover(box, 'a', 'z', 2,
                    '\u2E80', '\u9fa5', 2).ToList();
            }
        }

        public static Page getPage(String url)
        {
            return App.Auto.Get<Page>("Page", url);
        }

        public static void removePage(String url)
        {
            IndexAPI.removePage(url);
        }

        public static String addPage(String url, bool isKeyPage)
        {
            if (!isKeyPage)
            {
                if (App.Auto.Get<Object>("Page", url) != null)
                {
                    return null;
                }
            }

            HashSet<String> subUrls = new HashSet<String>();

            DateTime begin = DateTime.Now;
            Page p = Html.Get(url, subUrls);
            DateTime ioend = DateTime.Now;

            if (p == null)
            {
                return "temporarily unreachable";
            }
            else
            {

                IndexAPI.removePage(url);
                p.isKeyPage = isKeyPage;
                IndexAPI.addPage(p);
                IndexAPI.addPageIndex(url);

                long textOrder = App.Auto.NewId(0, 0);
                DateTime indexend = DateTime.Now;
                Log("TIME IO:" + (ioend - begin).TotalSeconds
                    + " INDEX:" + (indexend - ioend).TotalSeconds + "  TEXTORDER:" + textOrder + " ");

                subUrls.remove(url);
                subUrls.remove(url + "/");
                subUrls.remove(url.substring(0, url.length() - 1));

                runBGTask(subUrls);

                return url;
            }
        }

        public static void addPageCustomText(String url, String title, String content)
        {
            if (url == null || title == null || content == null)
            {
                return;
            }
            Page page = App.Auto.Get<Page>("Page", url);

            PageText text = new PageText();
            text.textOrder = page.textOrder;
            text.priority = PageText.userPriority;
            text.url = page.url;
            text.title = title;
            text.text = content;
            text.keywords = "";

            IndexAPI.addPageTextIndex(text);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void runBGTask(String url, String customTitle = null, String customContent = null)
        {
            backgroundThreadStack.Push(() =>
             {
                 lock (typeof(App))
                 {
                     var bc = Console.BackgroundColor;
                     Console.BackgroundColor = ConsoleColor.DarkRed;
                     Log("(KeyPage) For:" + url + " ," + backgroundThreadStack.Count);
                     Console.BackgroundColor = bc;
                     String r = addPage(url, true);
                     //addPageCustomText(url, customTitle, customContent);
                     backgroundLog(url, r);
                 }
             });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void runBGTask(HashSet<String> subUrls)
        {
            if (subUrls == null || isshutdown)
            {
                return;
            }
            bool atNight = true;
            int max_background = atNight ? 1000 : 1;

            if (backgroundThreadStack.Count < max_background)
            {
                foreach (String vurl in subUrls)
                {
                    var url = vurl;
                    backgroundThreadStack.Push(() =>
                    {
                        lock (typeof(App))
                        {
                            Log("For:" + url + " ," + backgroundThreadStack.Count);
                            String r = addPage(url, false);
                            backgroundLog(url, r);
                        }
                    });
                }
            }

        }

        public static void backgroundLog(String url, String output)
        {
            if (output == null)
            {
                Log("Has indexed:" + url);
            }
            else if (url.equals(output))
            {
                Log("Indexed:" + url);
            }
            else
            {
                Log("Retry:" + url);
            }
            Log("");
        }


        private static ConcurrentStack<ThreadStart> backgroundThreadStack = new ConcurrentStack<ThreadStart>();
        private static bool isshutdown = false;
        private static Thread backgroundTasks = new Func<Thread>(() =>
        {
            int SLEEP_TIME = 2000;
            var bt = new Thread(() =>
            {
                while (!isshutdown)
                {
                    ThreadStart act;
                    if (backgroundThreadStack.TryPop(out act))
                    {
                        try
                        {
                            act();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                    if (!isshutdown)
                        Thread.Sleep(SLEEP_TIME);
                }
            });
            bt.Priority = ThreadPriority.Lowest;
            bt.IsBackground = true;
            bt.Start();
            return bt;
        })();

        public static void Shutdown()
        {
            if (backgroundTasks != null)
            {
                isshutdown = true;
                backgroundTasks.Priority = ThreadPriority.Highest;
                backgroundTasks.Join();
                backgroundTasks = null;
            }
            Log("Background Task Ended");
        }
    }
    public class IndexAPI
    {
        public readonly static Engine engine = new Engine();
        public static Engine ENGINE => engine;


        public static long[] Search(List<PageText> outputPages,
                String name, long[] startId, long pageCount)
        {
            name = name.Trim();
            if (name.Length > 100) { return new long[] { -1 }; }
            //And
            if (startId[0] > 0)
            {
                startId[0] = Search(outputPages, name, startId[0], pageCount);
                if (outputPages.Count >= pageCount && startId[0] > 0)
                {
                    return startId;
                }
            }

            //Or
            String orName = new String(ENGINE.sUtil.clear(name));
            orName = orName.Replace("\"", " ").Trim();

            ArrayList<StringBuilder> ors = new ArrayList<StringBuilder>();
            ors.add(new StringBuilder());
            for (int i = 0; i < orName.length(); i++)
            {
                char c = orName[i];
                StringBuilder last = ors.get(ors.size() - 1);

                if (c == ' ')
                {
                    if (last.Length > 0)
                    {
                        ors.add(new StringBuilder());
                    }
                }
                else if (last.Length == 0)
                {
                    last.Append(c);
                }
                else if (!ENGINE.sUtil.isWord(c))
                {
                    if (!ENGINE.sUtil.isWord(last[last.Length - 1]))
                    {
                        last.Append(c);
                        ors.add(new StringBuilder());
                    }
                    else
                    {
                        last = new StringBuilder();
                        last.Append(c);
                        ors.add(last);
                    }
                }
                else
                {
                    if (!ENGINE.sUtil.isWord(last[last.Length - 1]))
                    {
                        last = new StringBuilder();
                        last.Append(c);
                        ors.add(last);
                    }
                    else
                    {
                        last.Append(c);
                    }
                }
            }

            ors.add(0, null);
            ors.add(1, new StringBuilder(name));


            if (startId.Length < ors.size())
            {
                startId = new long[ors.size()];
                startId[0] = -1;
                for (int i = 1; i < startId.Length; i++)
                {
                    startId[i] = long.MaxValue;
                }
            }

            if (ors.Count > 16 || stringEqual(ors[1].ToString(), ors[2].ToString()))
            {
                for (int i = 1; i < startId.Length; i++)
                {
                    startId[i] = -1;
                }
                return startId;
            }


            using (IBox box = App.Cube())
            {

                IEnumerator<KeyWord>[] iters = new IEnumerator<KeyWord>[ors.size()];

                for (int i = 0; i < ors.size(); i++)
                {
                    StringBuilder sbkw = ors.get(i);
                    if (startId[i] <= 0 || sbkw == null || sbkw.Length < 2)
                    {
                        iters[i] = null;
                        startId[i] = -1;
                        continue;
                    }
                    //never set Long.MAX 
                    long subCount = pageCount * 10;
                    iters[i] = ENGINE.searchDistinct(box, sbkw.ToString(), startId[i], subCount).GetEnumerator();
                }

                KeyWord[] kws = new KeyWord[iters.Length];

                int mPos = maxPos(startId);
                while (mPos > 0)
                {

                    for (int i = 0; i < iters.Length; i++)
                    {
                        if (kws[i] == null)
                        {
                            if (iters[i] != null && iters[i].MoveNext())
                            {
                                kws[i] = iters[i].Current;
                                startId[i] = kws[i].I;
                            }
                            else
                            {
                                iters[i] = null;
                                kws[i] = null;
                                startId[i] = -1;
                            }
                        }
                    }

                    if (outputPages.Count >= pageCount)
                    {
                        break;
                    }

                    mPos = maxPos(startId);

                    if (mPos > 1)
                    {
                        KeyWord kw = kws[mPos];

                        long id = kw.I;
                        var p = box["PageText", id].Select<PageText>();
                        p.keyWord = kw;
                        p.isAndSearch = false;
                        outputPages.Add(p);
                    }

                    long maxId = startId[mPos];
                    for (int i = 0; i < startId.Length; i++)
                    {
                        if (startId[i] == maxId)
                        {
                            kws[i] = null;
                        }
                    }

                }

            }
            return startId;
        }


        private static int maxPos(long[] ids)
        {
            int pos = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] > ids[pos])
                {
                    pos = i;
                }
            }
            return pos;
        }

        private static bool stringEqual(String a, String b)
        {
            if (a.Equals(b)) { return true; }
            if (a.Equals("\"" + b + "\"")) { return true; }
            if (b.Equals("\"" + a + "\"")) { return true; }
            return false;
        }
        public static long Search(List<PageText> pages,
                String name, long startId, long pageCount)
        {
            name = name.Trim();
            using (var box = App.Cube())
            {
                foreach (KeyWord kw in engine.searchDistinct(box, name, startId, pageCount))
                {
                    startId = kw.I - 1;

                    long id = kw.I;
                    var p = box["PageText", id].Select<PageText>();
                    p.keyWord = kw;
                    pages.Add(p);
                    pageCount--;
                }
            }
            return pageCount == 0 ? startId : -1;
        }




        public static bool? addPage(Page page)
        {

            if (App.Auto.Get<Object>("Page", page.url) != null)
            {
                //call removePage first
                return null;
            }

            page.createTime = DateTime.Now;
            page.textOrder = App.Auto.NewId();

            //log last page
            App.Item.Insert("/PageBegin", page);
            return App.Auto.Insert("Page", page);
        }


        public static bool addPageIndex(String url)
        {

            Page page = App.Auto.Get<Page>("Page", url);
            if (page == null)
            {
                return false;
            }

            List<PageText> ptlist = Html.getDefaultTexts(page);

            foreach (PageText pt in ptlist)
            {
                addPageTextIndex(pt);
            }

            return true;
        }

        public static void addPageTextIndex(PageText pt)
        {
            using (IBox box = App.Auto.Cube())
            {
                if (box["PageText", pt.id].Select<Object>() != null)
                {
                    return;
                }
                box["PageText"].Insert(pt);
                ENGINE.indexText(box, pt.id, pt.indexedText(), false, DelayService.delay);
                box.Commit();
            }
        }

        public static void removePage(String url)
        {

            Page page = App.Auto.Get<Page>("Page", url);
            if (page == null)
            {
                return;
            }

            List<PageText> ptlist = App.Auto.Select<PageText>("from PageText where textOrder==?", page.textOrder);

            foreach (PageText pt in ptlist)
            {
                using (IBox box = App.Auto.Cube())
                {
                    ENGINE.indexText(box, pt.id, pt.indexedText(), true);
                    box["PageText", pt.id].Delete();
                    box.Commit();
                }
            }

            App.Auto.Delete("Page", url);
        }



    }




    public class Html
    {
        public static String getUrl(String name)
        {
            int p = name.IndexOf("http://");
            if (p < 0)
            {
                p = name.IndexOf("https://");
            }
            if (p >= 0)
            {
                name = name.substring(p).Trim();
                var t = name.IndexOf("#");
                if (t > 0)
                {
                    name = name.substring(0, t);
                }
                return name;
            }
            return "";
        }

        private static String getMetaContentByName(IDocument doc, String name)
        {
            String description = null;
            try
            {
                description = doc.QuerySelector("meta[name='" + name + "']").Attributes["content"].Value;
            }
            catch
            {

            }

            try
            {
                if (description == null)
                {
                    description = doc.QuerySelector("meta[property='og:" + name + "']").Attributes["content"].Value;
                }
            }
            catch
            {

            }

            name = name.substring(0, 1).ToUpper() + name.substring(1);
            try
            {
                if (description == null)
                {
                    description = doc.QuerySelector("meta[name='" + name + "']").Attributes["content"].Value;
                }
            }
            catch
            {

            }

            if (description == null)
            {
                description = "";
            }

            return replace(description);
        }
        static String splitWords = " ,.　，。";
        public static Page Get(String url, HashSet<String> subUrls)
        {
            try
            {
                if (url == null || url.Length > Page.MAX_URL_LENGTH || url.Length < 8)
                {
                    return null;
                }

                var config = Configuration.Default.WithDefaultLoader();
                var doc = BrowsingContext.New(config).OpenAsync(url).GetAwaiter().GetResult();
                if (doc == null)
                {
                    return null;
                }
                //Log(doc.ToHtml());
                fixSpan(doc);

                Page page = new Page();
                page.url = url;
                page.text = replace(doc.Body.TextContent);
                if (page.text.length() < 10)
                {
                    //some website can't get html
                    Log("No HTML " + url);
                    return null;
                }

                if (subUrls != null)
                {
                    var host = doc.BaseUrl.Host;
                    var links = doc.QuerySelectorAll<IHtmlAnchorElement>("a[href]");
                    foreach (var link in links)
                    {
                        if (link.Host.Equals(host))
                        {
                            String ss = link.Href;
                            if (ss != null && ss.length() > 8)
                            {
                                ss = getUrl(ss);
                                subUrls.add(ss);
                            }
                        }
                    }
                }

                String title = null;
                String keywords = null;
                String description = null;

                try
                {
                    title = doc.Title;
                }
                catch
                {

                }
                if (title == null)
                {
                    title = "";
                }
                if (title.length() < 1)
                {
                    title = url;
                    //ignore no title
                    Log("No Title " + url);
                    return null;
                }
                title = replace(title);
                if (title.length() > 100)
                {
                    title = title.substring(0, 100);
                }

                keywords = getMetaContentByName(doc, "keywords");
                foreach (char c in splitWords)
                {
                    keywords = keywords.Replace(c, ' ');
                }
                if (keywords.length() > 200)
                {
                    keywords = keywords.substring(0, 200);
                }

                description = getMetaContentByName(doc, "description");
                if (description.length() > 400)
                {
                    description = description.substring(0, 400);
                }

                page.title = title;
                page.keywords = keywords;
                page.description = description;

                return page;
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return null;
            }
        }
        public static List<PageText> getDefaultTexts(Page page)
        {
            if (page.textOrder < 1)
            {
                //no id;
                return null;
            }

            ArrayList<PageText> result = new ArrayList<PageText>();

            String title = page.title;
            String keywords = page.keywords;

            String url = page.url;
            long textOrder = page.textOrder;

            PageText description = new PageText();

            description.textOrder = textOrder;
            description.url = url;
            description.title = title;
            description.keywords = keywords;

            description.text = page.description;

            description.priority = PageText.descriptionPriority;
            if (page.isKeyPage)
            {
                description.priority = PageText.descriptionKeyPriority;
            }
            result.add(description);

            String content = page.text.trim() + "..";
            int maxLength = PageText.max_text_length;

            int wordCount = 0;
            for (int i = 0; i < content.length(); i++)
            {
                char c = content.charAt(i);
                if (c < 256)
                {
                    wordCount++;
                }
                else
                {
                    bool isword = IndexAPI.ENGINE.sUtil.isWord(c);
                    if (isword)
                    {
                        wordCount++;
                    }
                }
            }
            if (((double)wordCount / (double)content.length()) > 0.8)
            {
                maxLength *= 4;
            }

            long startPriority = PageText.descriptionPriority - 1;
            while (startPriority > 0 && content.length() > 0)
            {

                PageText text = new PageText();
                text.textOrder = textOrder;
                text.url = url;
                text.title = title;
                text.keywords = "";

                text.text = null;
                StringBuilder texttext = new StringBuilder(maxLength + 100);

                int last = Math.Min(maxLength, content.length() - 1);
                int p1 = 0;
                foreach (char c in splitWords.toCharArray())
                {
                    int t = content.lastIndexOf(c, last);
                    if (t >= 0)
                    {
                        p1 = Math.Max(p1, t);
                    }
                }
                if (p1 == 0)
                {
                    p1 = last;
                }

                texttext.append(content.substring(0, p1 + 1));

                content = content.substring(p1 + 1);

                if (content.length() > 0 && content.length() < 100)
                {
                    texttext.append(" ").append(content);
                    content = "";
                }

                text.text = texttext.toString();
                text.priority = startPriority;
                result.add(text);
                startPriority--;
            }

            return result;

        }


        private static string replace(String content)
        {
            content = content.Replace("　", " ").Replace(((char)8203).ToString(), "");
            content = Regex.Replace(content, "\t|\r|\n|�|<|>", " ");
            content = Regex.Replace(content, "\\$", " ");
            content = Regex.Replace(content, "\\s+", " ");
            content = content.Trim();
            return content;
        }
        private static void fixSpan(IDocument doc)
        {
            foreach (var s in new string[] { "script", "style", "textarea", "noscript" })
            {
                foreach (var c in doc.GetElementsByTagName(s).ToArray())
                {
                    c.Parent.RemoveElement(c);
                }
            }
            foreach (var c in doc.GetElementsByTagName("span"))
            {
                if (c.ChildNodes.Length == 1)
                {
                    c.TextContent += " " + c.TextContent + " ";
                }
            }
        }

    }
}