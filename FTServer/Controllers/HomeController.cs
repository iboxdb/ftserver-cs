using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FTServer.Models;

using static FTServer.App;

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
        public async Task<IActionResult> About(string q)
        {
            await Task.Yield();

            var m = new AboutModel();
            q = q.Replace("<", "").Replace(">", "").Trim();

            bool? isdelete = null;

            if (q.StartsWith("http://") || q.StartsWith("https://"))
            {
                isdelete = false;
            }
            else if (q.StartsWith("delete")
              && (q.Contains("http://") || q.Contains("https://")))
            {
                q = q.substring(6).trim();
                isdelete = true;
            }
            if (!isdelete.HasValue)
            {
                try
                {
                    IndexPage.addSearchTerm(q);
                }
                catch
                {

                }
            }
            else if (isdelete.Value)
            {
                lock (typeof(App))
                {
                    IndexPage.removePage(q);
                }
                q = "deleted";
            }
            else
            {

                String[] fresult = new String[] { "background running" };
                String furl = Html.getUrl(q);

                Task.Run(() =>
                {
                    lock (typeof(App))
                    {
                        Log("For:" + furl);
                        String rurl = IndexPage.addPage(furl, true);
                        IndexPage.backgroundLog(furl, rurl);

                        //IndexPage.addPageCustomText(furl, ttitle, tmsg);

                        fresult[0] = rurl;
                    }
                }).Wait(3000);
                q = fresult[0];
            }

            m.Result = new ResultPartialModel
            {
                Query = q,
                StartId = null
            };
            IndexAPI.delayIndex();
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
            IndexAPI.delayIndex();
            return View("ResultPartial", Result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

