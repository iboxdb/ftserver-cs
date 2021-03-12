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
            var m = new AboutModel();
            q = q.Replace("<", "").Replace(">", "").Trim();

            bool ishttp = false;

            if (q.StartsWith("http://") || q.StartsWith("https://"))
            {
                ishttp = true;
            }

            if (!ishttp)
            {
                IndexPage.addSearchTerm(q);
            }
            else
            {
                String furl = Html.getUrl(q);
                IndexPage.runBGTask(furl);
                q = "Background Index Running, See Console Output";
            }

            m.Result = new ResultPartialModel
            {
                Query = q,
                StartId = null
            };

            DelayService.delayIndex();
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
            DelayService.delayIndex();
            return View("ResultPartial", Result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

