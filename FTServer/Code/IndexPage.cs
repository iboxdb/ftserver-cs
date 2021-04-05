using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static FTServer.App;
using System.Runtime.CompilerServices;

namespace FTServer
{

    public class IndexPage
    {

        public static void addSearchTerm(String keywords, bool isShutdown = false)
        {
            if (keywords.length() < PageSearchTerm.MAX_TERM_LENGTH)
            {
                PageSearchTerm pst = new PageSearchTerm();
                pst.time = DateTime.Now;
                pst.keywords = keywords;
                pst.uid = Guid.NewGuid();

                long huggersMem = 1024L * 1024L * 3L;
                if (isShutdown) { huggersMem = 0; }
                using (var box = App.Item.Cube())
                {
                    box["/PageSearchTerm"].Insert(pst);
                    box.Commit(huggersMem);
                }
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
            using (var box = App.Index.Cube())
            {
                List<String> result = new List<string>();
                result.AddRange(IndexAPI.ENGINE.discover(box, (char)0x0061, (char)0x007A, 2,
                    (char)0x4E00, (char)0x9FFF, 2));

                result.AddRange(IndexAPI.ENGINE.discover(box, (char)0x0621, (char)0x064A, 2,
                    (char)0x3040, (char)0x312F, 2));

                result.AddRange(IndexAPI.ENGINE.discover(box, (char)0x0410, (char)0x044F, 2,
                    (char)0xAC00, (char)0xD7AF, 2));
                return result;
            }
        }


        public static String addPage(String url, string userDescription, bool isKeyPage)
        {
            if (!isKeyPage)
            {
                if (App.Item.Count("from Page where url==? limit 0,1", url) > 0)
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
                return "Temporarily Unreachable";
            }
            else
            {
                p.userDescription = userDescription;
                p.show = true;
                p.isKeyPage = isKeyPage;
                long textOrder = IndexAPI.addPage(p);
                if (textOrder >= 0 && IndexAPI.addPageIndex(textOrder))
                {
                    IndexAPI.DisableOldPage(url);
                }
                long dbaddr = App.Indices.Count + IndexServer.IndexDBStart - 1;
                DateTime indexend = DateTime.Now;
                Log("TIME IO:" + (ioend - begin).TotalSeconds
                    + " INDEX:" + (indexend - ioend).TotalSeconds + "  TEXTORDER:" + textOrder + " (" + dbaddr + ") ");

                subUrls.remove(url);
                subUrls.remove(url + "/");
                subUrls.remove(url.substring(0, url.length() - 1));

                runBGTask(subUrls);

                return url;
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void runBGTask(String url, String customContent = null)
        {
            backgroundThreadStack.addFirst(() =>
             {
                 var bc = Console.BackgroundColor;
                 Console.BackgroundColor = ConsoleColor.DarkRed;
                 Log("(KeyPage) For:" + url + " ," + backgroundThreadStack.size());
                 Console.BackgroundColor = bc;
                 String r = addPage(url, customContent, true);
                 backgroundLog(url, r);
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

            if (backgroundThreadStack.size() < max_background)
            {
                foreach (String vurl in subUrls)
                {
                    var url = vurl;
                    backgroundThreadStack.addLast(() =>
                    {
                        Log("For:" + url + " ," + backgroundThreadStack.size());
                        String r = addPage(url, null, false);
                        backgroundLog(url, r);
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


        private static ConcurrentLinkedDeque<ThreadStart> backgroundThreadStack = new ConcurrentLinkedDeque<ThreadStart>();
        private static bool isshutdown = false;
        private static Thread backgroundTasks = new Func<Thread>(() =>
        {
            int SLEEP_TIME = 0;//2000;
            var bt = new Thread(() =>
            {
                while (!isshutdown)
                {
                    ThreadStart act = backgroundThreadStack.pollFirst();
                    if (act != null)
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
                    {
                        Thread.Sleep(SLEEP_TIME);
                    }
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
}