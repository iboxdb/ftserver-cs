using System.Collections.Generic;

namespace FTServer
{
	using System;

	public partial class s : System.Web.UI.Page
	{
		protected string name;
		protected List<Page> pages;
		protected DateTime begin;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			name = Request ["q"];

			if (name.Length > 500) {
				name = "";
				return;
			}
			name = name.Replace ("<", " ").Replace (">", " ")
				.Replace ("\"", " ").Replace (",", " ")
					.Replace ("\\$", " ").Trim ();

			bool? isdelete = null;

			if (name.StartsWith ("http://") || name.StartsWith ("https://")) {
				isdelete = false;
			} else if (name.StartsWith ("delete")
				&& (name.Contains ("http://") || name.Contains ("https://"))) {
				isdelete = true;
			}
			if (!isdelete.HasValue) {
				SearchResource.searchList.Enqueue (name);
				while (SearchResource.searchList.Count > 15) {
					String t;
					SearchResource.searchList.TryDequeue (out t);
				}
			} else {
				name = SearchResource.indexText (name, isdelete.Value);
			}

			pages = new List<Page> ();
			begin = DateTime.Now;

		
			using (var box = SDB.search_db.Cube()) {
				foreach (KeyWord kw in SearchResource.engine.searchDistinct(box, name)) {
					long id = kw.ID;
					id = FTServer.Page.rankDownId (id);
					Page p = box ["Page", id].Select<Page> ();
					p.keyWord = kw;
					pages.Add (p);
					if (pages.Count > 100) {
						break;
					}
				}
			}  

			if (pages.Count == 0) {
				Page p = new Page ();
				p.title = "NotFound";
				p.description = "";
				p.content = "input URL to index";
				p.url = "https://github.com/iboxdb/ftserver-cs";
				pages.Add (p);
			}

		}
	}
}

