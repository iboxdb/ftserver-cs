using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using iBoxDB.LocalServer;
using System.Threading.Tasks;

namespace FTServer
{
    public class SearchResource
    {
        public readonly static Engine engine = new Engine();
        public static int commitCount = 200;

        public static async Task<String> indexTextAsync(String name, bool onlyDelete)
        {
            String url = getUrl(name);
            {
                foreach (Page p in App.Auto.Select<Page>("from Page where url==?", url))
                {
                    engine.indexTextNoTran(App.Auto, commitCount, p.id, p.content.ToString(), true);
                    engine.indexTextNoTran(App.Auto, commitCount, p.rankUpId(), p.rankUpDescription(), true);
                    App.Auto.Delete("Page", p.id);
                }
            }
            if (onlyDelete)
            {
                return "deleted";
            }
            {
                Page p = await Page.GetAsync(url);
                if (p == null)
                {
                    return "temporarily unreachable";
                }
                else
                {
                    p.id = App.Auto.NewId();
                    App.Auto.Insert("Page", p);
                    engine.indexTextNoTran(App.Auto, commitCount, p.id, p.content.ToString(), false);
                    engine.indexTextNoTran(App.Auto, commitCount, p.rankUpId(), p.rankUpDescription(), false);

                    return p.url;
                }
            }
        }

        private static String getUrl(String name)
        {
            int p = name.IndexOf("http://");
            if (p < 0)
            {
                p = name.IndexOf("https://");
            }
            if (p >= 0)
            {
                name = name.Substring(p).Trim();
                var t = name.IndexOf("#");
                if (t > 0)
                {
                    name = name.Substring(0, t);
                }
                return name;
            }
            return "";
        }
    }

    public partial class Page
    {
        public const int MAX_URL_LENGTH = 150;
        public long id;
        public String url;
        public String title;
        public String description;
        public UString content;

        [NotColumn]
        public KeyWord keyWord;
    }
    public partial class Page
    {
        [NotColumn]
        public long rankUpId()
        {
            return id | (1L << 60);
        }

        [NotColumn]
        public static long rankDownId(long id)
        {
            return id & (~(1L << 60));
        }

        [NotColumn]
        public String rankUpDescription()
        {
            return description + " " + title;
        }

        private static readonly Random cran = new Random();

        [NotColumn]
        public String getRandomContent()
        {
            int len = content.ToString().Length - 100;
            if (len <= 20)
            {
                return content.ToString();
            }
            int s = cran.Next(len);
            if (s < 0)
            {
                s = 0;
            }
            if (s > len)
            {
                s = len;
            }

            int count = content.ToString().Length - s;
            if (count > 200)
            {
                count = 200;
            }
            return content.ToString().Substring(s, count);
        }

        [NotColumn]
        public static async Task<Page> GetAsync(String url)
        {
            try
            {
                if (url == null || url.Length > MAX_URL_LENGTH || url.Length < 8)
                {
                    return null;
                }
                Page page = new Page();
                page.url = url;

                var config = Configuration.Default.WithDefaultLoader();
                var doc = await BrowsingContext.New(config).OpenAsync(url);


                fixSpan(doc);

                page.title = doc.QuerySelector("title").Text();
                if (page.title == null)
                {
                    page.title = url;
                }
                page.title = page.title.Trim();
                if (page.title.Length < 2)
                {
                    page.title = url;
                }
                if (page.title.Length > 80)
                {
                    page.title = page.title.Substring(0, 80);
                }
                page.title = page.title.Replace("<", " ")
                    .Replace(">", " ").Replace("$", " ");

                removeTag(doc, "title");

                if (page.title.Contains("�"))
                {
                    //encode ??
                    return null;
                }

                try
                {
                    page.description = doc.QuerySelector("meta[name=\"description\"]").Attributes["content"].Value;
                }
                catch
                {
                }
                try
                {
                    if (page.description == null)
                    {
                        page.description = doc.QuerySelector("meta[name=\"Description\"]").Attributes["content"].Value;
                    }
                }
                catch
                {

                }
                if (page.description == null)
                {
                    page.description = "";
                }
                if (page.description.Length > 200)
                {
                    page.description = page.description.Substring(0, 200);
                }
                page.description = page.description.Replace("<", " ")
                    .Replace(">", " ").Replace("$", " ").Replace(((char)8203).ToString(), "");



                String content = doc.Body.Text().Replace("　", " ").Replace(((char)8203).ToString(), "");
                content = Regex.Replace(content, "\t|\r|\n|�|<|>", " ");
                content = Regex.Replace(content, "\\$", " ");
                content = Regex.Replace(content, "\\s+", " ");
                content = content.Trim();

                if (content.Length < 50)
                {
                    return null;
                }
                if (content.Length > 5000)
                {
                    content = content.Substring(0, 5000);
                }

                page.content = content + " " + page.url;

                return page;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }


        private static void removeTag(IDocument doc, string tag)
        {
            foreach (var c in doc.QuerySelectorAll(tag).ToArray())
            {
                c.Parent.RemoveElement(c);
            }
        }
        private static void fixSpan(IDocument doc)
        {

            foreach (var s in new string[] { "script", "style", "textarea", "noscript" })
            {
                removeTag(doc, s);
            }

            foreach (var c in doc.QuerySelectorAll("span"))
            {
                if (c.ChildNodes.Length == 1)
                {
                    c.TextContent += " ";
                }
            }
        }

    }
}

