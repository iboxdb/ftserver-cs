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
                return SearchResource.engine.discover(box, 'a', 'z', 4,
                    '\u2E80', '\u9fa5', 1).ToList();
            }
        }

        public static async Task<String> IndexTextAsync(String name, bool onlyDelete)
        {
            return await SearchResource.indexTextAsync(name, onlyDelete);
        }


        public async Task OnGetAsync(string q)
        {
            var name = q;

            bool? isdelete = null;

            if (name.StartsWith("http://") || name.StartsWith("https://"))
            {
                isdelete = false;
            }
            else if (name.StartsWith("delete")
              && (name.Contains("http://") || name.Contains("https://")))
            {
                isdelete = true;
            }
            if (!isdelete.HasValue)
            {

                searchList.Enqueue(name.Replace("<", ""));
                while (searchList.Count > 15)
                {
                    String t;
                    searchList.TryDequeue(out t);
                }
            }
            else
            {
                await IndexTextAsync(name, isdelete.Value);
                urlList.Enqueue(name.Replace("<", ""));
                while (urlList.Count > 3)
                {
                    String t;
                    urlList.TryDequeue(out t);
                }
            }
        }
    }
}
