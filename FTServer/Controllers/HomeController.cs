using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FTServer.Models;

using static FTServer.App;
using System.Threading;

namespace FTServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

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
            if (!ishttp)
            {
                DelayService.delayIndex();
            }
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

