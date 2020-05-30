using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTServer.Models
{
    public class AboutModel
    {

        public static ConcurrentQueue<String> searchList
            = new ConcurrentQueue<String>();
        public static ConcurrentQueue<String> urlList
            = new ConcurrentQueue<String>();

        public static List<String> GetDiscoveries()
        {
            using (var box = App.Auto.Cube())
            {
                return IndexAPI.engine.discover(box, 'a', 'z', 2,
                    '\u2E80', '\u9fa5', 2).ToList();
            }
        }

        public ResultPartialModel Result { get; set; }

    }
}
