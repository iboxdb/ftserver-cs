using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using iBoxDB.LocalServer;
using System.Threading.Tasks;

namespace FTServer
{
    public class PageLock
    {
        public String url;
        public DateTime time;
    }
    public partial class Page
    {
        public const int MAX_URL_LENGTH = 150;
        public long id;
        public String url;
        public String title;
        public String description;
        public String content;

        [NotColumn]
        public KeyWord keyWord;
    }
    public partial class Page
    {
        [NotColumn]
        public long rankUpId()
        {
            return id | (1L << 60);
        }

        [NotColumn]
        public static long rankDownId(long id)
        {
            return id & (~(1L << 60));
        }

        [NotColumn]
        public String rankUpDescription()
        {
            return description + " " + title;
        }

        private static readonly Random cran = new Random();

        [NotColumn]
        public String getRandomContent()
        {
            int len = content.Length - 100;
            if (len <= 20)
            {
                return content;
            }
            int s = cran.Next(len);
            if (s < 0)
            {
                s = 0;
            }
            if (s > len)
            {
                s = len;
            }

            int count = content.Length - s;
            if (count > 200)
            {
                count = 200;
            }
            return content.Substring(s, count);
        }


    }
}

