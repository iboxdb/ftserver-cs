using System;
using System.Collections.Generic;

namespace FTServer.Pages
{
    public class ResultPartialModel
    {
        //?q=
        public string Query { get; set; }
        //&s=
        public long? StartId { get; set; }



        public List<FTServer.Page> pages;
        public DateTime begin;

        public bool isFirstLoad;

        // reset in ResultPartial.cshtml
        public long pageCount = 12;


        public void Init()
        {
            if (StartId == null) { StartId = long.MaxValue; }

            isFirstLoad = StartId == long.MaxValue;

            pages = new List<FTServer.Page>();

            begin = DateTime.Now;

            StartId = IndexAPI.search(pages, Query, StartId.Value, pageCount);

            if (StartId == long.MaxValue)
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