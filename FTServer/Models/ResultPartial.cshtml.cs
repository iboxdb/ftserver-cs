using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FTServer.Models
{
    public class ResultPartialModel
    {
        //?q=
        public string Query { get; set; }
        //&s=
        public long[] StartId { get; set; }



        public List<PageText> pages;
        public DateTime begin;

        public bool isFirstLoad;

        // reset in ResultPartial.cshtml
        public long pageCount = 8;


        public String IdToString()
        {
            long[] ids = StartId;
            char p = '_';
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ids.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(p);
                }
                sb.Append(ids[i]);
            }
            return sb.ToString();
        }

        public bool IsEnd()
        {
            return StartId[0] < 0 && StartId[1] < 0;
        }


        public String ToKeyWordString()
        {
            HashSet<string> hc = new HashSet<string>();
            foreach (var pg in pages)
            {
                if (pg.keyWord != null && pg.keyWord.previous != null) { continue; }
                if (pg.keyWord is KeyWordE e)
                {
                    hc.Add(e.K);
                }
                if (pg.keyWord is KeyWordN n)
                {
                    hc.Add(n.toKString());
                }
            }


            var ids = hc.ToArray();
            char p = ' ';
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ids.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(p);
                }
                sb.Append(ids[i]);
            }
            return sb.ToString();
        }

        public void Init()
        {
            if (StartId == null) { StartId = new long[] { long.MaxValue }; }

            isFirstLoad = StartId[0] == long.MaxValue;

            pages = new List<PageText>();

            begin = DateTime.Now;

            StartId = IndexAPI.Search(pages, Query, StartId, pageCount);

            if (isFirstLoad && pages.Count == 0)
            {

            }
        }
    }
}