using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IBoxDB.LocalServer;

namespace FTServer
{

    public class ReadonlyList : IDisposable
    {

        public class AutoBoxHolder
        {

            public readonly long address;
            public readonly AutoBox Auto;

            public AutoBoxHolder(AutoBox auto, long a)
            {
                Auto = auto;
                address = a;
            }
        }

        AutoBoxHolder[] list = new AutoBoxHolder[0];

        public int length()
        {
            return list.Length;
        }

        public void switchIndexToReadonly()
        {
            App.Log("Switch Readonly DB " + list.Length);
            long addr = list[list.Length - 1].address;
            AutoBox auto = CreateAutoBox(addr, true);
            list[list.Length - 1] = new AutoBoxHolder(auto, addr);
        }

        public AutoBox get(int pos)
        {
            if (Config.Readonly_MaxDBCount < 2)
            {
                Config.Readonly_MaxDBCount = 2;
            }
            if (pos < (list.Length - Config.Readonly_MaxDBCount))
            {
                AutoBoxHolder o = list[pos];
                if (o.Auto != null)
                {
                    App.Log("Out of Cache " + (pos) + " / " + list.Length + " , set Config.Readonly_MaxDBCount bigger");
                    list[pos] = new AutoBoxHolder(null, o.address);
                }
            }
            AutoBoxHolder a = list[pos];
            if (a.Auto != null)
            {
                return a.Auto;
            }
            ReadonlyIndexServer server = new ReadonlyIndexServer();
            server.OutOfCache = true;
            if (Config.DSize > 1)
            {
                App.Log("Use No Cache DB " + a.address);
            }
            return server.GetInstance(a.address).Get();
        }

        public void tryCloseOutOfCache(AutoBox auto)
        {
            DatabaseConfig cfg = auto.GetDatabase().GetConfig();
            if (cfg is ReadonlyIndexServer.ReadonlyConfig)
            {
                if (((ReadonlyIndexServer.ReadonlyConfig)cfg).OutOfCache)
                {
                    if (Config.DSize > 1)
                    {
                        App.Log("close No Cache DB " + auto.GetDatabase().LocalAddress);
                    }
                    auto.GetDatabase().Dispose();
                }
            }
        }

        public void add(long addr, bool isReadonly)
        {
            AutoBox auto = CreateAutoBox(addr, isReadonly);
            AutoBoxHolder[] t = Arrays.copyOf(list, list.Length + 1);
            t[t.Length - 1] = new AutoBoxHolder(auto, addr);
            list = t;
        }

        private AutoBox CreateAutoBox(long addr, bool isReadonly)
        {
            AutoBox auto;
            if (isReadonly)
            {
                if (Config.Readonly_CacheLength < Config.lowReadonlyCache)
                {
                    auto = null;
                }
                else
                {
                    ReadonlyIndexServer server = new ReadonlyIndexServer();
                    auto = server.GetInstance(addr).Get();

                }
            }
            else
            {
                auto = new IndexServer().GetInstance(addr).Get();
            }
            return auto;
        }

        public void Dispose()
        {
            if (list != null)
            {
                foreach (AutoBoxHolder a in list)
                {
                    if (a.Auto != null)
                    {
                        a.Auto.GetDatabase().Dispose();
                    }
                }
            }
            list = null;
        }
    }

}