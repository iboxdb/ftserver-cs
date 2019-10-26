using System;
using System.Collections.Generic;
using System.Text;

namespace FTServer.Pages
{
    public class ResultPartialModel
    {
        //?q=
        public string Query { get; set; }
        //&s=
        public long[] StartId { get; set; }



        public List<FTServer.Page> pages;
        public DateTime begin;

        public bool isFirstLoad;

        // reset in ResultPartial.cshtml
        public long pageCount = 12;


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
            long[] ids = StartId;
            foreach (long l in ids)
            {
                if (l > 0)
                {
                    return false;
                }
            }
            return true;
        }
        public void Init()
        {
            if (StartId == null) { StartId = new long[] { long.MaxValue }; }

            isFirstLoad = StartId[0] == long.MaxValue;

            pages = new List<FTServer.Page>();

            begin = DateTime.Now;

            StartId = IndexAPI.Search(pages, Query, StartId, pageCount);

            if (isFirstLoad && pages.Count == 0)
            {
                {
                    Page p = new Page();
                    p.title = "NotFound";
                    p.description = "";
                    p.content = "input URL(http or https) to index";
                    p.url = "./";
                    pages.Add(p);
                }
            }
        }
    }
}