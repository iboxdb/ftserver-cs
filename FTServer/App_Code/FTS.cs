using System;
using System.Collections.Generic;
using iBoxDB.LocalServer;
using CsQuery;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FTServer
{
	public class SearchResource
	{

		public static ConcurrentQueue <String> searchList
			= new ConcurrentQueue<String> ();
		public static ConcurrentQueue<String> urlList
			= new ConcurrentQueue<String> ();
		public readonly static Engine engine = new Engine ();
		public static int commitCount = 200;

		public static String indexText (String name, bool isDelete)
		{
			String url = getUrl (name);
					 
			foreach (Page p in SDB.search_db.Select<Page>( "from Page where url==?", url)) {
				engine.indexTextNoTran (SDB.search_db, commitCount, p.id, p.content.ToString (), true);
				engine.indexTextNoTran (SDB.search_db, commitCount, p.rankUpId (), p.rankUpDescription (), true);
				SDB.search_db.Delete ("Page", p.id);				 
			}
		 
			if (isDelete) {
				return "deleted";
			}
			{ 
				Page p = Page.Get (url);
				if (p == null) {
					return "temporarily unreachable";
				} else {				 
					p.id = SDB.search_db.NewId ();
					SDB.search_db.Insert ("Page", p);
					engine.indexTextNoTran (SDB.search_db, commitCount, p.id, p.content.ToString (), false);
					engine.indexTextNoTran (SDB.search_db, commitCount, p.rankUpId (), p.rankUpDescription (), false);			 
				
					return p.url;
				}
			}
		}

		private static String getUrl (String name)
		{
			int p = name.IndexOf ("http://");
			if (p < 0) {
				p = name.IndexOf ("https://");
			}
			if (p >= 0) {
				name = name.Substring (p).Trim ();
				var t = name.IndexOf ("#");
				if (t > 0) {
					name = name.Substring (0, t);
				}
				return name;
			}
			return "";
		}
	}

	public class SDB
	{

		public static DB.AutoBox search_db;

		public static void init (String path, bool isVM)
		{

			Console.WriteLine ("DBPath=" + path);

			DB.Root (path);
		 
			DB server = new DB (1);
			if (isVM) {
				server.GetConfig ().DBConfig.CacheLength
                = server.GetConfig ().DBConfig.MB (16);
			} else {
				server.GetConfig ().DBConfig.CacheLength
					= server.GetConfig ().DBConfig.MB (512);
			}
			server.GetConfig ().DBConfig.SwapFileBuffer
				= (int)server.GetConfig ().DBConfig.MB (4);
			server.GetConfig ().DBConfig.FileIncSize
				= (int)server.GetConfig ().DBConfig.MB (16);
			new Engine ().Config (server.GetConfig ().DBConfig);
			server.GetConfig ().EnsureTable<Page> ("Page", "id");
			server.GetConfig ().EnsureIndex<Page> ("Page", true, "url(" + Page.MAX_URL_LENGTH + ")");

			search_db = server.Open ();

		}

		public static void close ()
		{
			if (search_db != null) {
				search_db.GetDatabase ().Close ();
			}
			search_db = null;
			Console.WriteLine ("DBClosed");
		}
	}

	public class Page
	{
		public const int MAX_URL_LENGTH = 100;
		public long id;
		public String url;
		public String title;
		public String description;
		public UString content;

		[NotColumn]
		public long rankUpId ()
		{
			return id | (1L << 60);
		}

		[NotColumn]
		public static long rankDownId (long id)
		{
			return id & (~(1L << 60));
		}

		[NotColumn]
		public String rankUpDescription ()
		{
			return description + " " + title;
		}

		private static readonly Random cran = new Random ();

		[NotColumn]
		public String getRandomContent ()
		{
			int len = content.ToString ().Length - 100;
			if (len <= 20) {
				return content.ToString ();
			}
			int s = cran.Next (len);
			if (s < 0) {
				s = 0;
			}
			if (s > len) {
				s = len;
			}

			int count = content.ToString ().Length - s;
			if (count > 200) {
				count = 200;
			}
			return content.ToString ().Substring (s, count);
		}

		[NotColumn]
		public static Page Get (String url)
		{
			try {
				if (url == null || url.Length > MAX_URL_LENGTH || url.Length < 8) {
					return null;
				}
				Page page = new Page ();
				page.url = url;

			  
				CQ doc = CQ.CreateFromUrl (url); 

				//Console.WriteLine(doc.Html());
				doc ["script"].Remove ();
				doc ["Script"].Remove ();

				doc ["style"].Remove ();
				doc ["Style"].Remove ();

				doc ["textarea"].Remove ();
				doc ["Textarea"].Remove ();

				doc ["noscript"].Remove ();
				doc ["Noscript"].Remove ();
						 
				page.title = doc ["title"].Text ();
				if (page.title == null) {
					page.title = doc ["Title"].Text ();
				}
				if (page.title == null) {
					page.title = url;
				}
				page.title = page.title.Trim ();
				if (page.title.Length < 2) {
					page.title = url;
				}
				if (page.title.Length > 80) {
					page.title = page.title.Substring (0, 80);
				}
				page.title = page.title.Replace ("<", " ")
					.Replace (">", " ").Replace ("$", " ");
				doc ["title"].Remove ();
				doc ["Title"].Remove ();
				if (page.title.Contains ("�")) {
					//encode ??
					return null;
				}

				page.description = doc ["meta[name='description']"].Attr ("content");
				if (page.description == null) {
					page.description = doc ["meta[name='Description']"].Attr ("content");
				}
				if (page.description == null) {
					page.description = "";
				}
				if (page.description.Length > 200) {
					page.description = page.description.Substring (0, 200);
				}
				page.description = page.description.Replace ("<", " ")
					.Replace (">", " ").Replace ("$", " ").Replace (((char)8203).ToString (), "");

			
				fixSpan(doc);
				String content = doc.Text ().Replace ("　", " ").Replace (((char)8203).ToString (), "");
				content = Regex.Replace (content, "\t|\r|\n|�|<|>", " ");
				content = Regex.Replace (content, "\\$", " ");
				content = Regex.Replace (content, "\\s+", " ");
				content = content.Trim (); 

				if (content.Length < 50) {
					return null;
				}
				if (content.Length > 5000) {
					content = content.Substring (0, 5000);
				}		
			
				page.content = content + " " + page.url;

				return page;
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
				return null;
			}
		}

		private static void fixSpan( CQ doc ){
			foreach (var c in doc ["span"]) {
				if (c.ChildNodes.Length == 1) {
					c.InnerText = c.InnerText + " ";
				}
			}
		}

		[NotColumn]
		public KeyWord keyWord;
	}
}

