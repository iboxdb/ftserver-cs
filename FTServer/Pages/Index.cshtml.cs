using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTServer.Pages
{
    public class IndexModel : PageModel
    {
        public List<String> Discoveries { get; set; } = new List<String>();

        public void OnGet()
        {
            using (var box = App.Auto.Cube())
            {
                foreach (String skw in SearchResource.engine.discover(box, 'a', 'z', 4,
                                                              '\u2E80', '\u9fa5', 1))
                {
                    Discoveries.Add(skw);
                }
            }
        }
    }
}
