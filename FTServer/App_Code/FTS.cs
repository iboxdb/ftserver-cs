using System;
using System.Collections.Generic;
using iBoxDB.LocalServer;
using CsQuery;
using System.Collections.Concurrent;

namespace FTServer
{
	public class SearchResource
	{

		public static ConcurrentQueue <String> searchList
			= new ConcurrentQueue<String> ();
		public static ConcurrentQueue<String> urlList
			= new ConcurrentQueue<String> ();
		public readonly static Engine engine = new Engine ();

		public static String indexText (String name, bool isDelete)
		{

			String url = getUrl (name);

			using (var box = SDB.search_db.Cube()) {
				foreach (Page p in box.Select<Page>( "from Page where url==?", url)) {
					engine.indexText (box, p.id, p.content.ToString (), true);
					box ["Page", p.id].Delete ();
					break;
				}
				box.Commit ().Assert ();
			}
			if (isDelete) {
				return "deleted";
			}
			{ 
				Page p = Page.Get (url);
				if (p == null) {
					return "temporarily unreachable";
				} else {
					using (var box = SDB.search_db.Cube()) {
						p.id = box.NewId ();
						box ["Page"].Insert (p);
						engine.indexText (box, p.id, p.content.ToString (), false);
						CommitResult cr = box.Commit ();
						cr.Assert (cr.GetErrorMsg (box));
					}
					urlList.Enqueue (p.url);
					while (urlList.Count > 3) {
						String t;
						urlList.TryDequeue (out t);
					}
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
				return name.Substring (p).Trim ();
			}
			return "";
		}
	}

	public class SDB
	{

		public static DB.AutoBox search_db;

		public static void init (String path)
		{

			Console.WriteLine ("DBPath=" + path);

			DB.Root (path);

			DB server = new DB (1);
			/*
        server.GetConfig().DBConfig.CacheLength
                = server.GetConfig().DBConfig.MB(8);
         */
			server.GetConfig ().DBConfig.SwapFileBuffer
				= (int)server.GetConfig ().DBConfig.MB (2);
			new Engine ().Config (server.GetConfig ().DBConfig);
			server.GetConfig ().EnsureTable<Page> ("Page", "id");
			server.GetConfig ().EnsureIndex<Page> ("Page", true, "url(100)");

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

		public long id;
		public String url;
		public String title;
		public String description;
		public UString content;

		public static Page Get (String url)
		{
			try {
				if (url == null || url.Length > 100 || url.Length < 8) {
					return null;
				}
				Page page = new Page ();
				page.url = url;

			 
				CQ doc = CQ.CreateFromUrl (url);
				doc ["script"].Remove ();
				doc ["style"].Remove ();
				doc ["Script"].Remove ();
				doc ["Style"].Remove ();
						 
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
					.Replace (">", " ").Replace ("$", " ");

				doc = CQ.Create (doc.Text ().Replace ("&lt;", "<")
				            .Replace ("&gt;", ">"));
				doc ["script"].Remove ();
				doc ["style"].Remove ();
				doc ["Script"].Remove ();
				doc ["Style"].Remove ();

				String content = doc.Text ();
				if (content.Length < 10) {
					return null;
				}
				if (content.Length > 5000) {
					content = content.Substring (0, 5000);
				}

				content = content.Replace ("\r", " ")
					.Replace ("\n", " ")
						.Replace ("   ", " ")
						.Replace ("  ", " ")
						.Replace ("  ", " ").Trim ();
			
				page.content = ((content
					+ " " + page.url
					+ " " + page.description).Replace ("<", " ")
				                         .Replace (">", " ").Replace ("$", " "));

				return page;
			} catch(Exception ex) {
				Console.WriteLine (ex.ToString ());
				return null;
			}
		}


		[NotColumn]
		public KeyWord keyWord;
	}
}

