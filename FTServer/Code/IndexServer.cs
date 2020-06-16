
using System;
using System.Threading;
using iBoxDB.LocalServer;
using iBoxDB.LocalServer.IO;

using static FTServer.App;

namespace FTServer
{
    public class DelayService
    {

        private static DateTime pageIndexDelay = DateTime.MinValue;

        public static void delayIndex(int seconds = 5)
        {
            lock (typeof(DelayService))
            {
                pageIndexDelay = DateTime.Now.AddSeconds(seconds);
            }
        }

        public static void delay()
        {
            if (pageIndexDelay == DateTime.MinValue) { return; }

            while (DateTime.Now < pageIndexDelay)
            {
                var d = (pageIndexDelay - DateTime.Now).TotalSeconds;
                if (d < 0) { d = 0; }
                if (d > 120) { d = 120; }

                Thread.Sleep((int)(d * 1000));
            }
        }
    }

    public class IndexServer : LocalDatabaseServer
    {

        public IndexServer()
        {
        }

        protected override DatabaseConfig BuildDatabaseConfig(long address)
        {
            if (address == 1)
            {
                return new IndexConfig();
            }
            if (address == 2)
            {
                return new ItemConfig();
            }
            return null;
        }

        private class ItemConfig : BoxFileStreamConfig
        {
            public ItemConfig()
            {
                CacheLength = MB(64);
                EnsureTable<PageSearchTerm>("/PageSearchTerm", "time", "keywords(" + PageSearchTerm.MAX_TERM_LENGTH + ")", "uid");
                EnsureTable<Page>("/PageBegin", "textOrder", "url(" + Page.MAX_URL_LENGTH + ")");
            }
        }


        //this IndexConfig will use IndexStream() to delay index-write, 
        //other application Tables, place into ItemConfig 
        private class IndexConfig : BoxFileStreamConfig
        {
            public IndexConfig()
            {
                //Recommend setup 4G-8G Cache
                CacheLength = MB(1024);

                FileIncSize = (int)MB(4);
                SwapFileBuffer = (int)MB(4);

                Log("DB Cache = " + (CacheLength / 1024 / 1024) + " MB");
                new Engine().Config(this);


                EnsureTable<Page>("Page", "url(" + Page.MAX_URL_LENGTH + ")");
                EnsureIndex<Page>("Page", true, "textOrder");

                EnsureTable<PageText>("PageText", "id");
                EnsureIndex<PageText>("PageText", false, "textOrder");
            }

            public override IBStream CreateStream(string path, StreamAccess access)
            {
                IBStream s = base.CreateStream(path, access);
                if (access == StreamAccess.ReadWrite)
                {
                    return new IndexStream(s);
                }
                return new IndexStream(s);
            }

        }
        private class IndexStream : IBStream
        {
            private IBStream s;

            public IndexStream(IBStream iBStream)
            {
                this.s = iBStream;
            }
            public void BeginWrite(long appID, int maxLen)
            {
                DelayService.delay();
                s.BeginWrite(appID, maxLen);
            }

            public void Write(long position, byte[] buffer, int offset, int count)
            {
                s.Write(position, buffer, offset, count);
            }


            int getmorecache = 1024 * 10;
            byte[] buf = new byte[1024 * 1024 * 1];
            public int Read(long position, byte[] buffer, int offset, int count)
            {
                if (buf != null && buf.Length > count)
                {
                    getmorecache--;
                    if (getmorecache > 0)
                    {
                        s.Read(position, buf, 0, buf.Length);
                    }
                    else
                    {
                        buf = null;
                    }
                }
                return s.Read(position, buffer, offset, count);
            }

            public long Length => s.Length;

            public void Dispose()
            {
                if (s != null)
                {
                    s.Dispose();
                    s = null;
                }
            }

            public void EndWrite()
            {
                s.EndWrite();
            }

            public void Flush()
            {
                s.Flush();
            }

            public void SetLength(long value)
            {
                s.SetLength(value);
            }
        }
    }
}