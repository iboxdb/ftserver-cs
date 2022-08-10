using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Dom.Events;

using static FTServer.App;

namespace FTServer
{
    public class Html
    {
        public static String getUrl(String name)
        {
            int p = name.IndexOf("http://");
            if (p < 0)
            {
                p = name.IndexOf("https://");
            }
            if (p >= 0)
            {
                name = name.substring(p).Trim();
                var t = name.IndexOf("#");
                if (t > 0)
                {
                    name = name.substring(0, t);
                }
                return name;
            }
            return "";
        }

        private static String getMetaContentByName(IDocument doc, String name)
        {
            String description = null;
            try
            {
                description = doc.QuerySelector("meta[name='" + name + "']").Attributes["content"].Value;
            }
            catch
            {

            }

            try
            {
                if (description == null)
                {
                    description = doc.QuerySelector("meta[property='og:" + name + "']").Attributes["content"].Value;
                }
            }
            catch
            {

            }

            name = name.substring(0, 1).ToUpper() + name.substring(1);
            try
            {
                if (description == null)
                {
                    description = doc.QuerySelector("meta[name='" + name + "']").Attributes["content"].Value;
                }
            }
            catch
            {

            }

            if (description == null)
            {
                description = "";
            }

            return replace(description);
        }
        //static String splitWords = " ,.　，。";
        public static Page Get(String url, HashSet<String> subUrls)
        {
            try
            {
                if (url == null || url.Length > Page.MAX_URL_LENGTH || url.Length < 8)
                {
                    Log("URL Length: " + url + " :" + (url != null ? url.length() : ""));
                    return null;
                }

                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);

                context.AddEventListener(AngleSharp.Dom.EventNames.Requested, (s, e) =>
                {
                    var r = ((RequestEvent)e).Response;
                    //Content-Type = text/html; charset=utf-8
                    bool isHTML = false;
                    foreach (var h in r.Headers)
                    {
                        if (h.Value == null) { continue; }
                        isHTML |= h.Value.Contains("text/html", StringComparison.InvariantCultureIgnoreCase);
                    }
                    if (!isHTML)
                    {
                        String Location = "Location";
                        if (r.Headers.ContainsKey(Location))
                        {
                            // https://blog.nuget.org/ 
                            Log(r.StatusCode + " Location: " + r.Headers[Location] + " from " + r.Address.Href);
                        }
                        else
                        {
                            // https://www.nuget.org/api/v2/package/iBoxDB/3.5.0
                            Log(r.StatusCode + " ContextType Not HTML, " + r.Address.Href);
                            r.Dispose();
                        }
                    }
                });
                var doc = context.OpenAsync(url).GetAwaiter().GetResult();
                if (doc == null)
                {
                    return null;
                }
                if (doc.StatusCode >= System.Net.HttpStatusCode.BadRequest)
                {
                    Log("Not OK " + url);
                    return null;
                }
                if (doc.ContentType != null && doc.ContentType.toLowerCase().equals("text/xml"))
                {
                    Log("XML " + url);
                    return null;
                }
                if (doc.ContentType != null && (doc.ContentType.toLowerCase().IndexOf("text/html") < 0))
                {
                    Log("Not HTML " + doc.ContentType);
                    return null;
                }
                if (subUrls != null)
                {
                    var host = doc.BaseUrl.Host;
                    var links = doc.QuerySelectorAll<IHtmlAnchorElement>("a[href]");
                    foreach (var link in links)
                    {
                        //if (link.Host.Equals(host))
                        {
                            String ss = link.Href;
                            if (ss != null && ss.length() > 8)
                            {
                                ss = getUrl(ss);
                                subUrls.add(ss);
                            }
                        }
                    }
                }
                //Log(doc.ToHtml());
                fixSpan(doc);

                Page page = new Page();
                page.url = url;
                String text = replace(doc.Body.Text());
                if (text.length() < 10)
                {
                    //some website can't get html
                    Log("No HTML " + url);
                    return null;
                }
                if (text.length() > 100_000)
                {
                    Log("BIG HTML " + url);
                    return null;
                }
                if (text.length() > 50_000)
                {
                    Log("[BigURL] " + url);
                }
                page.text = text;



                String title = null;
                String keywords = null;
                String description = null;

                try
                {
                    title = doc.Title;
                }
                catch
                {

                }
                if (title == null)
                {
                    title = "";
                }
                if (title.length() < 1)
                {
                    //title = url;
                    //ignore no title
                    Log("No Title " + url);
                    return null;
                }
                title = replace(title);
                if (title.length() > 200)
                {
                    title = title.substring(0, 200);
                }

                keywords = getMetaContentByName(doc, "keywords");
                keywords = keywords.Replace("，", ",");

                if (keywords.length() > 200)
                {
                    keywords = keywords.substring(0, 200);
                }

                description = getMetaContentByName(doc, "description");
                if (description.length() == 0)
                {
                    Log("Can't find description " + url);
                    page.text += " " + title;
                }
                if (description.length() > 500)
                {
                    description = description.substring(0, 500);
                }

                page.title = title;
                page.keywords = keywords;
                page.description = description;

                return page;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }

        public static PageText getDefaultText(Page page, long id)
        {
            PageText pt = PageText.fromId(id);
            pt.url = page.url;
            pt.title = page.title;
            pt.createTime = page.createTime;
            if (pt.priority >= PageText.descriptionPriority)
            {
                pt.keywords = page.keywords;
            }
            if (pt.priority == PageText.userPriority)
            {
                pt.text = page.userDescription;
            }
            if (pt.priority == PageText.descriptionPriority || pt.priority == PageText.descriptionKeyPriority)
            {
                pt.text = page.description;
            }

            if (pt.priority == PageText.contextPriority)
            {
                pt.text = page.text;
            }
            return pt;
        }
        public static List<PageText> getDefaultTexts(Page page)
        {
            if (page.textOrder < 1)
            {
                //no id;
                return null;
            }

            ArrayList<PageText> result = new ArrayList<PageText>();

            if (page.userDescription != null && page.userDescription.Length > 0)
            {
                result.add(getDefaultText(page, PageText.toId(page.textOrder, PageText.userPriority)));
            }
            if (page.description != null && page.description.Length > 0)
            {
                long p = page.isKeyPage ? PageText.descriptionKeyPriority : PageText.descriptionPriority;
                result.add(getDefaultText(page, PageText.toId(page.textOrder, p)));
            }
            if (page.text != null && page.text.Length > 0)
            {
                result.add(getDefaultText(page, PageText.toId(page.textOrder, PageText.contextPriority)));
            }

            return result;
        }


        public static string replace(String content)
        {
            content = content.Replace("　", " ").Replace(((char)8203).ToString(), " ");
            content = Regex.Replace(content, "\t|\r|\n|<|>", " ");
            content = Regex.Replace(content, "\\$", " ");
            content = Regex.Replace(content, "\\s+", " ");
            content = content.Trim();
            return content;
        }
        private static void fixSpan(IDocument doc)
        {
            foreach (var s in new string[] { "script", "style", "textarea", "noscript", "code" })
            {
                foreach (var c in new List<IElement>(doc.GetElementsByTagName(s)))
                {
                    c.Parent.RemoveElement(c);
                }
            }
            foreach (var s in new string[] {
                 "span", "td", "th", "li", "a", "option", "p",
                 "div", "h1","h2","h3","h4","h5", "pre" })
            {
                foreach (var c in doc.GetElementsByTagName(s))
                {
                    if (c.ChildNodes.Length == 1 && c.ChildNodes[0].NodeType == NodeType.Text)
                    {
                        try
                        {
                            c.TextContent = " " + c.TextContent + " ";
                        }
                        catch (Exception e)
                        {
                            Log(e.ToString());
                        }
                    }
                }
            }
        }

    }
}