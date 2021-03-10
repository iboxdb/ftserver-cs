using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using IBoxDB.LocalServer;
using System.Threading.Tasks;

//the db classes
namespace FTServer
{
    public class PageSearchTerm
    {

        public readonly static int MAX_TERM_LENGTH = 24;

        public DateTime time;
        public String keywords;
        public Guid uid;
    }
    public class PageText
    {

        public static readonly long userPriority = 12;

        public static readonly long descriptionKeyPriority = 11;

        //this is the center of Priorities, under is Body.Text, upper is user's input
        public static readonly long descriptionPriority = 10;

        public static readonly long contextPriority = 9;


        private static readonly int priorityOffset = 50;

        public static PageText fromId(long id)
        {
            PageText pt = new PageText();
            pt.priority = id >> priorityOffset;
            pt.textOrder = id - (pt.priority << priorityOffset);

            pt.text = String.Empty;
            pt.keywords = String.Empty;
            pt.url = String.Empty;
            pt.title = String.Empty;
            return pt;
        }
        public static long toId(long textOrder, long priority)
        {
            return textOrder | (priority << priorityOffset);
        }

        public long id
        {
            get
            {
                return toId(textOrder, priority);
            }
            set
            {
                //ignore set    
            }
        }


        public long textOrder;
        public long priority;

        public String url;

        public String title;

        public String text;

        //keywords
        public String keywords;

        public DateTime createTime = DateTime.Now;

        [NotColumn]
        public String indexedText()
        {
            if (priority >= descriptionPriority)
            {
                return text + " " + title;
            }

            if (priority == (descriptionPriority - 1))
            {
                return text + " " + url;
            }

            return text;
        }

        [NotColumn]
        public bool isAndSearch = true;

        [NotColumn]
        public KeyWord keyWord;


        [NotColumn]
        public Page page;

        [NotColumn]
        public long dbOrder = -1;
    }

    public partial class Page
    {
        public const int MAX_URL_LENGTH = 150;

        public String url;

        public long textOrder;

        // too too big this html
        //public String html;
        public String text;

        public DateTime createTime;
        public bool isKeyPage = false;

        public String title;
        public String keywords;
        public String description;

        public string userDescription;

        public bool show = true;

    }
    public partial class Page
    {

        private static Random RAN = new Random();


        public String getRandomContent(int length)
        {
            int len = text.length() - 100;
            if (len <= 20)
            {
                return text;
            }
            int s = RAN.nextInt(len);
            if (s < 0)
            {
                s = 0;
            }
            if (s > len)
            {
                s = len;
            }

            int end = s + length;
            if (end > text.length())
            {
                end = text.length();
            }

            return text.substring(s, end);
        }
    }


}


