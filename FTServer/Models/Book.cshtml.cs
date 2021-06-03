


using System;

namespace FTServer.Models
{
    public class BookModel
    {
        public static readonly Random ran = new Random();
        public string Title { get; set; }
        public string Description { get; set; }

        public string Keywords { get; set; }

        public string Text { get; set; }

        public string Ex { get; set; }
    }
}