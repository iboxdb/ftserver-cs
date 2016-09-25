

namespace FTServer
{
	using System;
	using System.Web;
	using System.Web.UI;
	using System.Collections.Generic;

	public partial class spart : System.Web.UI.Page
	{
		protected string name;
		protected List<FTServer.Page> pages;
		protected DateTime begin;
		protected readonly long pageCount = 12;
		protected long startId = long.MaxValue;
		protected bool isFirstLoad ;
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e); 
			name = Request ["q"];
			name = name.Trim ();

			string temps = Request ["s"];
			if (!string.IsNullOrEmpty(temps)) {
				startId = long.Parse (temps);
			}
			isFirstLoad = startId == long.MaxValue;

			pages = new List<FTServer.Page> ();			

			begin = DateTime.Now;

			using (var box = SDB.search_db.Cube()) {
				foreach (KeyWord kw in SearchResource.engine.searchDistinct(box, name,startId,pageCount)) {
					startId = kw.ID - 1;

					long id = kw.ID;
					id = FTServer.Page.rankDownId (id);
					var p = box ["Page", id].Select<FTServer.Page> ();
					p.keyWord = kw;
					pages.Add (p);
					 
				}
			}  

			if (startId == long.MaxValue) {
				Page p = new Page ();
				p.title = "NotFound";
				p.description = "";
				p.content = "input URL to index";
				p.url = "./";
				pages.Add (p);
			}
		}
	}
}

