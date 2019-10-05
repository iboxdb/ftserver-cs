using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using iBoxDB.LocalServer;
using System.Threading.Tasks;

namespace FTServer
{
    public class IndexAPI
    {
        public readonly static Engine engine = new Engine();

        public static long search(List<Page> pages,
                String name, long startId, long pageCount)
        {
            name = name.Trim();
            using (var box = App.Cube())
            {
                foreach (KeyWord kw in engine.searchDistinct(box, name, startId, pageCount))
                {
                    startId = kw.ID - 1;

                    long id = kw.ID;
                    id = FTServer.Page.rankDownId(id);
                    var p = box["Page", id].Select<FTServer.Page>();
                    p.keyWord = kw;
                    pages.Add(p);

                }
            }


            //Recommend
            if (pages.Count == 0 && name.length() > 1)
            {
                if (!engine.isWord(name[0]) && name[0] != '"')
                {
                    //only search one char, if full search is empty
                    search(pages, name.substring(0, 1), long.MaxValue, pageCount);
                }
                else
                {
                    int pos = name.IndexOf(' ');
                    if (pos > 0)
                    {
                        name = name.substring(0, pos);
                        search(pages, name, long.MaxValue, pageCount);
                    }
                }
                if (pages.Count == 0)
                {
                    return startId;
                }
                return -1;
            }
            return startId;
        }
        public static async Task<String> indexTextAsync(String url, bool deleteOnly)
        {
            bool tran = true;
            if (tran)
            {
                return await indexTextWithTranAsync(Html.getUrl(url), deleteOnly);
            }
            return await indexTextNoTranAsync(Html.getUrl(url), deleteOnly);
        }


        //with transaction, faster
        private static async Task<String> indexTextWithTranAsync(String url, bool onlyDelete)
        {
            return await pageLockAsync(url, async () =>
            {
                using (var box = App.Auto.Cube())
                {
                    Page defaultPage = null;
                    foreach (Page p in box.Select<Page>("from Page where url==?", url).ToArray())
                    {
                        engine.indexText(box, p.id, p.content, true);
                        engine.indexText(box, p.rankUpId(), p.rankUpDescription(), true);
                        box["Page", p.id].Delete();
                        defaultPage = p;
                    }

                    if (onlyDelete)
                    {
                        return box.Commit() == CommitResult.OK ? "deleted" : "not deleted";
                    }
                    {
                        Page p = await Html.GetAsync(url);
                        if (p == null)
                        {
                            //p = defaultPage;
                        }
                        if (p == null)
                        {
                            return "temporarily unreachable";
                        }
                        else
                        {
                            if (p.id == 0)
                            {
                                p.id = box.NewId();
                            }
                            box["Page"].Insert(p);
                            engine.indexText(box, p.id, p.content, false);
                            engine.indexText(box, p.rankUpId(), p.rankUpDescription(), false);

                            CommitResult cr = box.Commit();
                            if (cr != CommitResult.OK)
                            {
                                return cr.GetErrorMsg(box);
                            }
                            return p.url;
                        }
                    }

                }
            });
        }

        //no transaction, less memory
        private static async Task<String> indexTextNoTranAsync(String url, bool onlyDelete)
        {

            const int commitCount = 200;
            return await pageLockAsync(url, async () =>
            {
                Page defaultPage = null;
                foreach (Page p in App.Auto.Select<Page>("from Page where url==?", url))
                {
                    engine.indexTextNoTran(App.Auto, commitCount, p.id, p.content, true);
                    engine.indexTextNoTran(App.Auto, commitCount, p.rankUpId(), p.rankUpDescription(), true);
                    App.Auto.Delete("Page", p.id);
                    defaultPage = p;
                }

                if (onlyDelete)
                {
                    return "deleted";
                }
                {
                    Page p = await Html.GetAsync(url);
                    if (p == null)
                    {
                        p = defaultPage;
                    }
                    if (p == null)
                    {
                        return "temporarily unreachable";
                    }
                    else
                    {
                        if (p.id == 0)
                        {
                            p.id = App.Auto.NewId();
                        }
                        App.Auto.Insert("Page", p);
                        engine.indexTextNoTran(App.Auto, commitCount, p.id, p.content, false);
                        engine.indexTextNoTran(App.Auto, commitCount, p.rankUpId(), p.rankUpDescription(), false);

                        return p.url;
                    }
                }
            });
        }

        private static async Task<String> pageLockAsync(String url, Func<Task<string>> run)
        {
            using (var box = App.Auto.Cube())
            {
                PageLock pl = box["PageLock", url].Select<PageLock>();
                if (pl == null)
                {
                    pl = new PageLock
                    {
                        url = url,
                        time = DateTime.Now
                    };
                }
                else if ((DateTime.Now - pl.time).TotalSeconds > (60 * 5))
                {
                    pl.time = DateTime.Now;
                }
                else
                {
                    return "Running";
                }
                box["PageLock"].Replace(pl);
                if (box.Commit() != CommitResult.OK)
                {
                    return "Running";
                }
            }
            try
            {
                return await run();
            }
            finally
            {
                App.Auto.Delete("PageLock", url);
            }
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
                name = name.Substring(p).Trim();
                var t = name.IndexOf("#");
                if (t > 0)
                {
                    name = name.Substring(0, t);
                }
                return name;
            }
            return "";
        }
        public static async Task<Page> GetAsync(String url)
        {
            try
            {
                if (url == null || url.Length > Page.MAX_URL_LENGTH || url.Length < 8)
                {
                    return null;
                }
                Page page = new Page();
                page.url = url;

                var config = Configuration.Default.WithDefaultLoader();
                var doc = await BrowsingContext.New(config).OpenAsync(url);


                fixSpan(doc);

                page.title = doc.QuerySelector("title").Text();
                if (page.title == null)
                {
                    page.title = url;
                }
                page.title = page.title.Trim();
                if (page.title.Length < 2)
                {
                    page.title = url;
                }
                if (page.title.Length > 80)
                {
                    page.title = page.title.Substring(0, 80);
                }
                page.title = page.title.Replace("<", " ")
                    .Replace(">", " ").Replace("$", " ");

                removeTag(doc, "title");

                if (page.title.Contains("�"))
                {
                    //encode ??
                    return null;
                }

                try
                {
                    page.description = doc.QuerySelector("meta[name=\"description\"]").Attributes["content"].Value;
                }
                catch
                {
                }
                try
                {
                    if (page.description == null)
                    {
                        page.description = doc.QuerySelector("meta[name=\"Description\"]").Attributes["content"].Value;
                    }
                }
                catch
                {

                }
                if (page.description == null)
                {
                    page.description = "";
                }
                if (page.description.Length > 200)
                {
                    page.description = page.description.Substring(0, 200);
                }
                page.description = page.description.Replace("<", " ")
                    .Replace(">", " ").Replace("$", " ").Replace(((char)8203).ToString(), "");



                String content = doc.Body.Text().Replace("　", " ").Replace(((char)8203).ToString(), "");
                content = Regex.Replace(content, "\t|\r|\n|�|<|>", " ");
                content = Regex.Replace(content, "\\$", " ");
                content = Regex.Replace(content, "\\s+", " ");
                content = content.Trim();

                if (content.Length < 50)
                {
                    return null;
                }
                if (content.Length > 5000)
                {
                    content = content.Substring(0, 5000);
                }

                page.content = content + " " + page.url;

                return page;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }


        private static void removeTag(IDocument doc, string tag)
        {
            foreach (var c in doc.QuerySelectorAll(tag).ToArray())
            {
                c.Parent.RemoveElement(c);
            }
        }
        private static void fixSpan(IDocument doc)
        {

            foreach (var s in new string[] { "script", "style", "textarea", "noscript" })
            {
                removeTag(doc, s);
            }

            foreach (var c in doc.QuerySelectorAll("span"))
            {
                if (c.ChildNodes.Length == 1)
                {
                    c.TextContent += " ";
                }
            }
        }

    }
}