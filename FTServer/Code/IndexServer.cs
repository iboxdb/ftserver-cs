
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using IBoxDB.LocalServer;
using IBoxDB.LocalServer.IO;

using static FTServer.App;

namespace FTServer
{
    public class DelayService
    {

        private static DateTime pageIndexDelay = DateTime.MinValue;

        public static void delayIndex(int seconds = 5)
        {
            pageIndexDelay = DateTime.Now.AddSeconds(seconds);
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

    public class ReadonlyIndexServer : LocalDatabaseServer
    {

        public bool OutOfCache = false;
        protected override DatabaseConfig BuildDatabaseConfig(long address)
        {
            return new ReadonlyConfig(address, OutOfCache);
        }
        public class ReadonlyConfig : ReadonlyStreamConfig
        {
            private long address;
            public bool OutOfCache;
            public ReadonlyConfig(long address, bool outOfCache) : base(GetStreamsImpl(address, outOfCache))
            {
                this.address = address;
                this.OutOfCache = outOfCache;
                this.CacheLength = Config.Readonly_CacheLength;
                if (this.CacheLength < Config.lowReadonlyCache)
                {
                    this.CacheLength = Config.lowReadonlyCache;
                }
            }

            private static Stream[] GetStreamsImpl(long address, bool outOfCache)
            {
                string pa = DatabaseConfig.GetFileName(address);
                Stream[] os = new Stream[outOfCache ? 1 : 2];
                for (int i = 0; i < os.Length; i++)
                {
                    os[i] = new FileStream(pa, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                return os;
            }

        }
    }
    public class IndexServer : LocalDatabaseServer
    {
        public static long ItemDB = 2L;

        public static long IndexDBStart = 10L;
        public IndexServer()
        {
        }

        protected override DatabaseConfig BuildDatabaseConfig(long address)
        {
            if (address >= IndexDBStart)
            {
                return new IndexConfig();
            }
            if (address == ItemDB)
            {
                return new ItemConfig();
            }
            return null;
        }

        private class ItemConfig : BoxFileStreamConfig
        {
            public ItemConfig()
            {
                CacheLength = Config.ItemConfig_CacheLength;
                SwapFileBuffer = Config.ItemConfig_SwapFileBuffer;
                FileIncSize = Config.ItemConfig_SwapFileBuffer;
                EnsureTable<PageSearchTerm>("/PageSearchTerm", "time", "keywords(" + PageSearchTerm.MAX_TERM_LENGTH + ")", "uid");

                EnsureTable<Page>("Page", "textOrder");
                //the 'textOrder' is used to control url's order
                EnsureIndex<Page>("Page", "url(" + Page.MAX_URL_LENGTH + ")", "textOrder");
            }
        }


        //this IndexConfig will use IndexStream() to delay index-write, 
        //other application Tables, place into ItemConfig 
        private class IndexConfig : BoxFileStreamConfig
        {
            public IndexConfig()
            {
                CacheLength = Config.SwitchToReadonlyIndexLength;
                SwapFileBuffer = Config.ItemConfig_SwapFileBuffer;
                //this size trigger "SWITCH" in Flush()
                FileIncSize = Config.ItemConfig_SwapFileBuffer;

                Log("Index DB Cache = " + (CacheLength / 1024 / 1024) + " MB");
                Engine.Instance.Config(this);

            }

            public override IBStream CreateStream(string path, StreamAccess access)
            {
                IBStream s = base.CreateStream(path, access);
                if (access == StreamAccess.ReadWrite)
                {
                    return new IndexStream(s);
                }
                return s;
            }

        }
        private class IndexStream : IBStreamWrapper
        {

            public IndexStream(IBStream iBStream) : base(iBStream)
            {

            }
            public override void BeginWrite(long appID, int maxLen)
            {
                DelayService.delay();
                base.BeginWrite(appID, maxLen);
            }

            long length = 0;
            public override void SetLength(long value)
            {
                base.SetLength(value);
                length = value;
            }

            public override void Flush()
            {
                base.Flush();
                if (length > Config.SwitchToReadonlyIndexLength)
                {

                    App.Indices.switchIndexToReadonly();
                    long addr = App.Index.GetDatabase().LocalAddress;
                    addr++;
                    Log("\r\nSwitch To DB (" + addr + ")");
                    App.Indices.add(addr, false);

                    App.Index = App.Indices.get(App.Indices.length() - 1);

                    System.GC.Collect();
                }
            }

        }
    }
}
