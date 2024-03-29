using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

using static FTServer.App;

namespace FTServer
{

    public class IndexPage
    {
        public static readonly String SystemShutdown = "SystemShutdown";

        public static void addSearchTerm(String keywords)
        {
            if (keywords == null)
            {
                return;
            }
            if (keywords.length() > PageSearchTerm.MAX_TERM_LENGTH)
            {
                keywords = keywords.substring(0, PageSearchTerm.MAX_TERM_LENGTH - 1);
            }

            PageSearchTerm pst = new PageSearchTerm();
            pst.time = DateTime.Now;
            pst.keywords = keywords;
            pst.uid = Guid.NewGuid();

            long huggersMem = 1024L * 1024L * 3L;

            bool isShutdown = SystemShutdown.Equals(keywords);
            if (isShutdown) { huggersMem = 0; }
            using (var box = App.Item.Cube())
            {
                box["/PageSearchTerm"].Insert(pst);
                box.Commit(huggersMem);
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
            return Engine.Instance.getDesc(str, kw, length);
        }
        public static List<String> discover()
        {
            using (var box = App.Index.Cube())
            {
                ArrayList<String> result = new ArrayList<string>();

                //English           
                result.addAll(Engine.Instance.discoverEN(box, (char)0x0061, (char)0x007A, 2));

                //Russian
                result.addAll(Engine.Instance.discoverEN(box, (char)0x0410, (char)0x044F, 2));

                //arabic
                result.addAll(Engine.Instance.discoverEN(box, (char)0x0621, (char)0x064A, 2));

                //India
                result.addAll(Engine.Instance.discoverEN(box, (char)0x0900, (char)0x097F, 2));

                //Japanese  Hiragana(0x3040-309F), Katakana(0x30A0-30FF)           
                result.addAll(Engine.Instance.discoverCN(box, (char)0x3040, (char)0x30FF, 2));

                //Chinese
                result.addAll(Engine.Instance.discoverCN(box, (char)0x4E00, (char)0x9FFF, 2));

                //Korean
                result.addAll(Engine.Instance.discoverEN(box, (char)0xAC00, (char)0xD7AF, 2));


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
            if (subUrls.size() > 0)
            {
                subUrls.remove(url);
                subUrls.remove(url + "/");
                subUrls.remove(url.substring(0, url.length() - 1));
                runBGTask(subUrls, isKeyPage);
            }
            if (p == null)
            {
                return "Temporarily Unreachable";
            }
            else
            {
                if (userDescription != null)
                {
                    userDescription = Html.replace(userDescription);
                }
                p.userDescription = userDescription;
                p.show = true;
                p.isKeyPage = isKeyPage;
                long textOrder = IndexAPI.addPage(p);
                if (textOrder >= 0 && IndexAPI.addPageIndex(textOrder))
                {
                    IndexAPI.DisableOldPage(url);
                }
                long dbaddr = App.Indices.length() + IndexServer.IndexDBStart - 1;
                DateTime indexend = DateTime.Now;
                Log("TIME IO:" + (ioend - begin).TotalSeconds
                    + " INDEX:" + (indexend - ioend).TotalSeconds + "  TEXTORDER:" + textOrder + " (" + dbaddr + ") ");

                return url;
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void runBGTask(String url, String customContent = null)
        {
            backgroundThreadQueue.addFirst(() =>
             {
                 var bc = Console.BackgroundColor;
                 Console.BackgroundColor = ConsoleColor.DarkRed;
                 Log("(KeyPage) For:" + url + " ," + backgroundThreadQueue.size());
                 Console.BackgroundColor = bc;
                 String r = addPage(url, customContent, true);
                 backgroundLog(url, r);
             });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void runBGTask(HashSet<String> subUrls, bool isKeyPage)
        {
            if (subUrls == null || isshutdown)
            {
                return;
            }
            bool atNight = true;

            int max_background = atNight ? 500 : 0;
            if (App.IsAndroid && max_background > 50)
            {
                max_background = 50;
            }

            if (isKeyPage)
            {
                max_background *= 2;
            }


            foreach (String vurl in subUrls)
            {
                if (backgroundThreadQueue.size() > max_background)
                {
                    break;
                }
                var url = Html.getUrl(vurl);
                backgroundThreadQueue.addLast(() =>
                {
                    Log("For:" + url + " ," + backgroundThreadQueue.size());
                    String r = addPage(url, null, false);
                    backgroundLog(url, r);
                });
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


        private static ConcurrentLinkedDeque<ThreadStart> backgroundThreadQueue = new ConcurrentLinkedDeque<ThreadStart>();
        private static bool isshutdown = false;

        public static int HttpGet_SleepTime = 1000;
        private static Thread backgroundTasks = new Func<Thread>(() =>
        {
            var bt = new Thread(() =>
            {
                while (!isshutdown)
                {
                    ThreadStart act = backgroundThreadQueue.pollFirst();
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
                    else
                    {
                        Thread.Sleep(2000);
                    }

                    if (!isshutdown)
                    {
                        Thread.Sleep(HttpGet_SleepTime);
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