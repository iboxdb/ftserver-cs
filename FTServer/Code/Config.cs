using System.Collections.Generic;

namespace FTServer
{
    public sealed class Config
    {


        public static readonly long lowReadonlyCache = Config.mb(8);

        public static readonly long DSize = 1L;

        public static long mb(long len)
        {
            return 1024L * 1024L * len;
        }

        // it should set bigger than 500MB
        public static long SwitchToReadonlyIndexLength = mb(500L * 1L) / DSize;
        public static long Index_CacheLength = mb(500L * 1L) / DSize;


        public static long Readonly_CacheLength = mb(32);

        //Set 1400 MB Readonly Index Cache
        public static long Readonly_MaxDBCount = mb(1400) / mb(32) / DSize;


        //HTML Page Cache, this should set bigger, if have more memory. 
        public static long ItemConfig_CacheLength = mb(256);
        public static int ItemConfig_SwapFileBuffer = (int)mb(20);

        //this should less than 2/3 MaxMemory
        public static long minCache()
        {
            return Index_CacheLength + Readonly_CacheLength * Readonly_MaxDBCount + ItemConfig_CacheLength
                    + ItemConfig_SwapFileBuffer * 2;
        }
    }

}