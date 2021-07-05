using System.Collections.Generic;

namespace FTServer
{
    public sealed class Config
    {


        public static readonly long lowReadonlyCache = Config.mb(2);

        public static readonly long DSize = 1L;

        public static long mb(long len)
        {
            return 1024L * 1024L * len;
        }

        public static long SwitchToReadonlyIndexLength = mb(500L * 1L) / DSize;

        public static long Readonly_CacheLength = mb(32);
        public static long Readonly_MaxDBCount = mb(2000) / mb(32) / DSize;

        public static long ItemConfig_CacheLength = mb(256);
        public static int ItemConfig_SwapFileBuffer = (int)mb(20);

    }

}