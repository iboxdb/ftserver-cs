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

using IBoxDB.LocalServer;
using static FTServer.App;
using System.Runtime.CompilerServices;

namespace FTServer
{

    public class IndexAPI
    {
        //set >0, more memory, Writer faster.
        public static long HuggersMemory = 0;

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
        public static long Search(IBox box, List<PageText> pages,
                String name, long startId, long pageCount)
        {
            name = name.Trim();

            foreach (KeyWord kw in engine.searchDistinct(box, name, startId, pageCount))
            {
                pageCount--;
                startId = kw.I - 1;

                long id = kw.I;
                PageText pt = PageText.fromId(id);
                Page p = getPage(pt.textOrder);
                if (!p.show) { continue; }
                pt = Html.getDefaultText(p, id);

                if (pt.text.Length < 100)
                {
                    pt.text += " " + p.getRandomContent(100);
                }

                pt.keyWord = kw;
                pages.Add(pt);
            }

            return pageCount == 0 ? startId : -1;
        }

        public static Page getPage(long textOrder)
        {
            return App.Item.Get<Page>("Page", textOrder);
        }

        public static bool addPage(Page page)
        {
            page.createTime = DateTime.Now;
            page.textOrder = App.Item.NewId();
            return App.Item.Insert("Page", page);
        }


        public static bool addPageIndex(Page page)
        {

            List<PageText> ptlist = Html.getDefaultTexts(page);

            int count = 0;
            foreach (PageText pt in ptlist)
            {
                count++;
                addPageTextIndex(pt, count == ptlist.Count ? 0 : HuggersMemory);
            }

            return true;
        }

        public static void addPageTextIndex(PageText pt, long huggers = 0)
        {
            using (IBox box = App.Index.Cube())
            {
                if (box["PageText", pt.id].Select<Object>() != null)
                {
                    return;
                }
                box["PageText"].Insert(pt);
                ENGINE.indexText(box, pt.id, pt.indexedText(), false, DelayService.delay);
                CommitResult cr = box.Commit(huggers);
                Log("MEM:  " + cr.GetMemoryLength(box).ToString("#,#"));
            }
        }

        public static void DisableOldPage(String url)
        {
            using (var box = App.Item.Cube())
            {
                List<Page> page = new List<Page>();

                foreach (var p in box.Select<Page>("from Page where url== limit 1,10", url))
                {
                    if (!p.show) { break; }
                    page.Add(p);
                }
                foreach (var p in page)
                {
                    p.show = false;
                    box["Page"].Update(p);
                }
                box.Commit().Assert();
            }
        }
    }

}