/* iBoxDB FTServer Bruce Yang CL */
using System;
using System.Collections.Generic;
using System.Text;
using IBoxDB.LocalServer;
using static FTServer.App;

namespace FTServer
{

    public class IndexAPI
    {
        public readonly static Engine ENGINE = new Engine();

        private class StartIdParam
        {
            //andBox,orBox, ids...
            public long[] startId;

            public StartIdParam(long[] id)
            {
                if (id.Length == 1)
                {
                    startId = new long[] { App.Indices.Count - 1, -1, id[0] };
                }
                else
                {
                    startId = id;
                }
            }
            public bool isAnd()
            {
                if (startId[0] >= 0)
                {
                    if (startId[2] >= 0) { return true; }
                    startId[0]--;
                    startId[2] = long.MaxValue;
                    return isAnd();
                }
                return false;
            }

            internal virtual ArrayList<StringBuilder> ToOrCondition(String name)
            {
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

                ors.add(0, null); //and box
                ors.add(1, null); //or box
                ors.add(2, null); //and startId
                //full search
                ors.add(3, new StringBuilder(name));

                if (startId.Length != ors.size())
                {
                    startId = new long[ors.size()];
                    startId[0] = -1;
                    startId[1] = App.Indices.Count - 1;//or box
                    startId[2] = -1;
                    for (int i = 3; i < startId.Length; i++)
                    {
                        startId[i] = long.MaxValue;
                    }
                }

                if (ors.Count > 16 || stringEqual(ors[3].ToString(), ors[4].ToString()))
                {
                    for (int i = 0; i < startId.Length; i++)
                    {
                        startId[i] = -1;
                    }
                }

                return ors;
            }

            public bool isOr()
            {
                if (startId[1] >= 0)
                {
                    for (int i = 3; i < startId.Length; i++)
                    {
                        if (startId[i] >= 0) { return true; }
                    }
                    startId[1]--;
                    for (int i = 3; i < startId.Length; i++)
                    {
                        startId[i] = long.MaxValue;
                    }
                    return isOr();
                }
                return false;
            }
        }

        public static long[] Search(List<PageText> outputPages,
                String name, long[] t_startId, long pageCount)
        {
            name = name.Trim();
            if (name.Length > 150) { return new long[] { -1, -1, -1 }; }

            long maxTime = 1000 * 2;
            if (pageCount == 1)
            {
                maxTime = 500;
            }
            StartIdParam startId = new StartIdParam(t_startId);
            long beginTime = Environment.TickCount64;

            //And
            while (startId.isAnd())
            {
                DelayService.delayIndex();
                AutoBox auto = App.Indices[(int)startId.startId[0]];
                startId.startId[2] = SearchAnd(auto, outputPages, name, startId.startId[2], pageCount - outputPages.Count);
                foreach (var pt in outputPages)
                {
                    if (pt.dbOrder < 0)
                    {
                        pt.dbOrder = startId.startId[0] + IndexServer.IndexDBStart;
                    }
                }
                if (outputPages.Count >= pageCount)
                {
                    return startId.startId;
                }
                if ((Environment.TickCount64 - beginTime) > maxTime)
                {
                    return startId.startId;
                }
            }


            //OR            
            ArrayList<StringBuilder> ors = startId.ToOrCondition(name);
            while (startId.isOr())
            {
                DelayService.delayIndex();
                AutoBox auto = App.Indices[(int)startId.startId[1]];
                SearchOr(auto, outputPages, ors, startId.startId, pageCount);
                foreach (var pt in outputPages)
                {
                    if (pt.dbOrder < 0)
                    {
                        pt.dbOrder = startId.startId[1] + IndexServer.IndexDBStart;
                    }
                }
                if (outputPages.Count >= pageCount)
                {
                    break;
                }
                if ((Environment.TickCount64 - beginTime) > maxTime)
                {
                    break;
                }
            }
            return startId.startId;
        }

        private static long SearchAnd(AutoBox auto, List<PageText> pages,
                String name, long startId, long pageCount)
        {
            name = name.Trim();

            using (var box = auto.Cube())
            {
                foreach (KeyWord kw in ENGINE.searchDistinct(box, name, startId, pageCount))
                {
                    pageCount--;
                    startId = kw.I - 1;

                    long id = kw.I;
                    PageText pt = PageText.fromId(id);
                    Page p = getPage(pt.textOrder);
                    if (p.show)
                    {
                        pt = Html.getDefaultText(p, id);
                        pt.keyWord = kw;
                        pt.page = p;
                        pt.isAndSearch = true;
                        pages.Add(pt);
                    }
                }

                return pageCount == 0 ? startId : -1;
            }
        }
        private static void SearchOr(AutoBox auto, List<PageText> outputPages,
                       ArrayList<StringBuilder> ors, long[] startId, long pageCount)
        {

            using (IBox box = auto.Cube())
            {

                IEnumerator<KeyWord>[] iters = new IEnumerator<KeyWord>[ors.size()];

                for (int i = 0; i < ors.size(); i++)
                {
                    StringBuilder sbkw = ors.get(i);
                    if (sbkw == null || sbkw.Length < 1)
                    {
                        iters[i] = null;
                        continue;
                    }
                    //never set Long.MAX 
                    long subCount = pageCount * 10;
                    iters[i] = ENGINE.searchDistinct(box, sbkw.ToString(), startId[i], subCount).GetEnumerator();
                }

                int orStartPos = 3;
                KeyWord[] kws = new KeyWord[iters.Length];

                int mPos = maxPos(startId);
                while (mPos >= orStartPos)
                {

                    for (int i = orStartPos; i < iters.Length; i++)
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
                                startId[i] = -1;
                            }
                        }
                    }

                    if (outputPages.Count >= pageCount)
                    {
                        break;
                    }

                    mPos = maxPos(startId);

                    if (mPos > orStartPos)
                    {
                        KeyWord kw = kws[mPos];

                        long id = kw.I;

                        PageText pt = PageText.fromId(id);
                        Page p = getPage(pt.textOrder);
                        if (p.show)
                        {
                            pt = Html.getDefaultText(p, id);
                            pt.keyWord = kw;
                            pt.page = p;
                            pt.isAndSearch = false;
                            outputPages.Add(pt);
                        }
                    }

                    long maxId = startId[mPos];
                    for (int i = orStartPos; i < startId.Length; i++)
                    {
                        if (startId[i] == maxId)
                        {
                            kws[i] = null;
                        }
                    }

                }

            }

        }

        private static int maxPos(long[] ids)
        {
            int orStartPos = 3;
            orStartPos--;
            for (int i = orStartPos; i < ids.Length; i++)
            {
                if (ids[i] > ids[orStartPos])
                {
                    orStartPos = i;
                }
            }
            return orStartPos;
        }

        private static bool stringEqual(String a, String b)
        {
            if (a.Equals(b)) { return true; }
            if (a.Equals("\"" + b + "\"")) { return true; }
            if (b.Equals("\"" + a + "\"")) { return true; }
            return false;
        }
        public static Page getPage(long textOrder)
        {
            return App.Item.Get<Page>("Page", textOrder);
        }

        public static long addPage(Page page)
        {
            /*
            Page oldPage = GetOldPage(page.url);
            if (oldPage != null && oldPage.show)
            {
                if (oldPage.text.equals(page.text))
                {
                    Log("Page is not changed. " + page.url);
                    return -1L;
                }
                else
                {
                    Log("Page is changed. " + page.url);
                }
            }
            */
            using (var box = App.Item.Cube())
            {
                page.createTime = DateTime.Now;
                page.textOrder = box.NewId();
                box["Page"].Insert(page, 1);
                if (box.Commit() == CommitResult.OK)
                {
                    return page.textOrder;
                }
                return -1L;
            }
        }


        public static bool addPageIndex(long textOrder)
        {
            Page page = getPage(textOrder);
            if (page == null) { return false; }
            long HuggersMemory = int.MaxValue / 2;
            List<PageText> ptlist = Html.getDefaultTexts(page);

            int count = 0;
            foreach (PageText pt in ptlist)
            {
                count++;
                addPageTextIndex(pt, count == ptlist.Count ? 0 : HuggersMemory);
            }

            return true;
        }

        private static void addPageTextIndex(PageText pt, long huggers = 0)
        {
            using (IBox box = App.Index.Cube())
            {
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

                foreach (var p in box.Select<Page>("from Page where url==? limit 1,10", url))
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
        public static Page GetOldPage(String url)
        {
            List<Page> pages = App.Item.Select<Page>("from Page where url==? limit 0,1", url);
            if (pages.Count > 0) { return pages[0]; }
            return null;
        }
    }

}