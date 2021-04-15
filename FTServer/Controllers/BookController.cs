using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using FTServer.Models;
using System.Text.RegularExpressions;

namespace FTServer.Controllers
{
    public class BookController : Controller
    {
        public static String[] Books = null;
        public static int Base = 100;

        //UTF-8 Text
        private static String book1_path = "/home/user/github/hero.txt";
        private static String book2_path = "/home/user/github/phoenix.txt";

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index(int book = 0, float start = 0, int length = 2000)
        {
            if (Books == null)
            {
                String[] tmp = new String[2];

                StreamReader sr = new StreamReader(book1_path);
                tmp[0] = sr.ReadToEnd();
                sr.Close();

                sr = new StreamReader(book2_path);
                tmp[1] = sr.ReadToEnd();
                sr.Close();

                Books = tmp;
            }

            start = start / (float)Base;

            int startIndex = (int)(start * Books[book].Length);
            int endIndex = startIndex + length;
            if (endIndex > Books[book].Length)
            {
                endIndex = Books[book].Length;
            }

            String content = Books[book].Substring(startIndex, endIndex - startIndex);

            String title = content.Length > 200 ? content.Substring(0, 200) : content;
            String description = content.Length > 300 ? content.Substring(100, 300) : content;
            String text = content.Length > 500 ? content.Substring(300) : content;
            String keywords = "keyword1 keywords2,keyword3 hello";

            title = Regex.Replace(title, "\t|\r|\n|�|<|>|\\s+", " ");
            description = Regex.Replace(description, "\t|\r|\n|�|<|>|\\s+", " ");


            var m = new BookModel();
            m.Title = title;
            m.Description = description;
            m.Keywords = keywords;
            if (book == 0)
            {
                text += "  " + DateTime.Now;
            }
            m.Text = text;

            return View(m);
        }
    }


}