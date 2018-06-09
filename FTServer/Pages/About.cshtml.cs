using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTServer.Pages
{
    public class AboutModel : PageModel
    {

        public static ConcurrentQueue<String> searchList
            = new ConcurrentQueue<String>();
        public static ConcurrentQueue<String> urlList
            = new ConcurrentQueue<String>();

        public static List<String> GetDiscoveries()
        {
            using (var box = App.Auto.Cube())
            {
                return SearchResource.engine.discover(box, 'a', 'z', 2,
                    '\u2E80', '\u9fa5', 1).ToList();
            }
        }

        public static async Task<String> IndexTextAsync(String name, bool onlyDelete)
        {
            return await SearchResource.indexTextAsync(name, onlyDelete);
        }

        public ResultPartialModel Result { get; set; }
        public async Task OnGetAsync(string q)
        {
            q = q.Replace("<", "").Replace(">", "").Trim();

            Result = new ResultPartialModel
            {
                Query = q,
                StartId = null
            };

            bool? isdelete = null;

            if (q.StartsWith("http://") || q.StartsWith("https://"))
            {
                isdelete = false;
            }
            else if (q.StartsWith("delete")
              && (q.Contains("http://") || q.Contains("https://")))
            {
                isdelete = true;
            }
            if (!isdelete.HasValue)
            {
                searchList.Enqueue(q);
                while (searchList.Count > 15)
                {
                    String t;
                    searchList.TryDequeue(out t);
                }
            }
            else
            {
                await IndexTextAsync(q, isdelete.Value);
                urlList.Enqueue(q);
                while (urlList.Count > 3)
                {
                    String t;
                    urlList.TryDequeue(out t);
                }
            }
        }
    }
}
