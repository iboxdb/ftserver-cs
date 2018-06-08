using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTServer.Pages
{
    public class AboutModel : PageModel
    {
        public string Message { get; set; }

        public async Task OnGetAsync(string q)
        {
            SearchResource.searchList.Enqueue(q);
            Message = "Your application description page: " + q;
        }
    }
}
