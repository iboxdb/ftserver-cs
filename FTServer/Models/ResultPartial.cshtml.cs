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

            ArrayList<KeyWord> kws = new ArrayList<KeyWord>();
            foreach (var pg in pages)
            {
                KeyWord kw = pg.keyWord;
                while( kw is KeyWordN)
                {  
                   kws.add(kw);
                   kw = kw.previous;
                }
            }
            foreach(KeyWord kw in kws){
                if (kw is KeyWordE e)
                {
                    hc.Add(e.K);
                }
                if (kw is KeyWordN n)
                {
                    String ks = n.toKString();
                    if (ks.length() > 1)
                      hc.Add(ks);
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
            String r = sb.ToString();
            //App.Log(r);
            return r;
        }

        public void Init()
        {
            if (StartId == null)
            {
                StartId = new long[] { long.MaxValue };
            }

            isFirstLoad = StartId[0] == long.MaxValue;
            if (isFirstLoad) { pageCount = 1; }

            pages = new List<PageText>();

            begin = DateTime.Now;

            StartId = IndexAPI.Search(pages, Query, StartId, pageCount);

        }
    }
}