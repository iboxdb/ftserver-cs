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
        public IActionResult Index(int book = 0, float start = 0, int length = 2000, string ex = "")
        {
            if (Books == null)
            {
                String[] tmp = new String[2];
                if (System.IO.File.Exists(book1_path) && System.IO.File.Exists(book2_path))
                {
                    StreamReader sr = new StreamReader(book1_path);
                    tmp[0] = sr.ReadToEnd();
                    sr.Close();

                    sr = new StreamReader(book2_path);
                    tmp[1] = sr.ReadToEnd();
                    sr.Close();
                }
                else
                {
                    tmp[0] = RamdomBook(0x4E00, 0x9FFF, (int)'　', 150, 600000);
                    tmp[1] = RamdomBook(0x0061, 0x007A, (int)' ', 16, 1000000);
                }
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
            String description = content.Length > 400 ? content.Substring(100, 300) : content;
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
            m.Ex = ex;
            return View(m);
        }

        private static String RamdomBook(int startChar, int endChar, int emptyChar, int emptylen, int maxlen)
        {
            Random ran = new Random();
            char[] cs = new char[maxlen];
            for (int i = 0; i < cs.Length; i++)
            {
                if (ran.nextInt(emptylen) == 0)
                {
                    cs[i] = (char)emptyChar;
                }
                else
                {
                    char c = (char)(ran.nextInt(endChar - startChar) + startChar);
                    cs[i] = c;
                }
            }
            return new String(cs);
        }
    }


}