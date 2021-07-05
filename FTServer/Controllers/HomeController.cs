using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FTServer.Models;

namespace FTServer.Controllers
{
    public class HomeController : Controller
    {

        public HomeController()
        {

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult About(string q)
        {
            if (q == null) { return NotFound(); }

            var m = new AboutModel();
            q = q.Replace("<", "").Replace(">", "").Trim();


            IndexPage.addSearchTerm(q);

            m.Result = new ResultPartialModel
            {
                Query = q,
                StartId = null
            };


            return View(m);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Result(String q, String s)
        {
            q = q.Replace("<", "").Replace(">", "").Trim();

            long[] ids = new long[] { long.MaxValue };
            if (s != null)
            {
                String[] ss = s.Trim().Split("_");
                ids = new long[ss.Length];
                for (int i = 0; i < ss.Length; i++)
                {
                    ids[i] = long.Parse(ss[i]);
                }
            }

            var Result = new ResultPartialModel
            {
                Query = q,
                StartId = ids
            };
            return View("ResultPartial", Result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Admin(String url = null, String msg = null)
        {
            if (url != null)
            {
                url = url.trim();
                bool ishttp = false;

                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    ishttp = true;
                }

                if (ishttp)
                {
                    String furl = Html.getUrl(url);
                    IndexPage.runBGTask(furl, msg);
                    url = IndexAPI.IndexingMessage;
                }
            }
            var m = new AdminModel();
            m.url = url;
            m.msg = msg;

            return View(m);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

