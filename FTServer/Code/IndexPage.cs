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
            using (var box = App.Index.Cube())
            {
                return IndexAPI.engine.discover(box, 'a', 'z', 2,
                    '\u2E80', '\u9fa5', 2).ToList();
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
            p.userDescription = userDescription;
            p.show = true;
            DateTime ioend = DateTime.Now;

            if (p == null)
            {
                return "temporarily unreachable";
            }
            else
            {

                p.isKeyPage = isKeyPage;
                IndexAPI.addPage(p);
                IndexAPI.addPageIndex(p);
                IndexAPI.DisableOldPage(url);

                long textOrder = App.Item.NewId(0, 0);
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


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void runBGTask(String url, String customContent = null)
        {
            backgroundThreadStack.addFirst(() =>
             {
                 lock (typeof(App))
                 {
                     var bc = Console.BackgroundColor;
                     Console.BackgroundColor = ConsoleColor.DarkRed;
                     Log("(KeyPage) For:" + url + " ," + backgroundThreadStack.size());
                     Console.BackgroundColor = bc;
                     String r = addPage(url, customContent, true);
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

            if (backgroundThreadStack.size() < max_background)
            {
                foreach (String vurl in subUrls)
                {
                    var url = vurl;
                    backgroundThreadStack.addLast(() =>
                    {
                        lock (typeof(App))
                        {
                            Log("For:" + url + " ," + backgroundThreadStack.size());
                            String r = addPage(url, null, false);
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