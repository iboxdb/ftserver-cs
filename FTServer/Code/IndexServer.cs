
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
        protected override DatabaseConfig BuildDatabaseConfig(long address)
        {
            return new ReadonlyConfig(address);
        }
        private class ReadonlyConfig : ReadonlyStreamConfig
        {
            private long address;
            public ReadonlyConfig(long address) : base(GetStreamsImpl(address))
            {
                this.address = address;
                this.CacheLength = MB(32);
            }

            private static Stream[] GetStreamsImpl(long address)
            {
                string pa = BoxFileStreamConfig.RootPath + ReadonlyStreamConfig.GetNameByAddrDefault(address);
                return new Stream[] { new FileStream(pa, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                 new FileStream(pa, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) };
            }

        }

    }
    public class IndexServer : LocalDatabaseServer
    {
        public static long SwitchToReadonlyIndexLength = 1024L * 1024L * 500L;
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
                CacheLength = MB(256);
                EnsureTable<PageSearchTerm>("/PageSearchTerm", "time", "keywords(" + PageSearchTerm.MAX_TERM_LENGTH + ")", "uid");
                EnsureTable<Page>("Page", "textOrder");
                EnsureIndex<Page>("Page", "url(" + Page.MAX_URL_LENGTH + ")", "textOrder");
            }
        }


        //this IndexConfig will use IndexStream() to delay index-write, 
        //other application Tables, place into ItemConfig 
        private class IndexConfig : BoxFileStreamConfig
        {
            public IndexConfig()
            {
                CacheLength = MB(512);
                Log("DB Cache = " + (CacheLength / 1024 / 1024) + " MB");
                new Engine().Config(this);

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
                if (length > IndexServer.SwitchToReadonlyIndexLength)
                {

                    var newIndices = new List<AutoBox>(App.Indices);
                    newIndices.RemoveAt(newIndices.Count - 1);

                    long addr = App.Index.GetDatabase().LocalAddress;
                    newIndices.Add(new ReadonlyIndexServer().GetInstance(addr).Get());
                    addr++;
                    Log("\r\nSwitch To DB (" + addr + ")");
                    newIndices.Add(new IndexServer().GetInstance(addr).Get());

                    App.Indices = newIndices;

                    App.Index = newIndices[newIndices.Count - 1];

                    System.GC.Collect();
                }
            }

        }
    }
}