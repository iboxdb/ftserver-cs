using System.Collections.Generic;

namespace FTServer
{
	using System;

	public partial class s : System.Web.UI.Page
	{
		protected string name;
		protected string queryString; 

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			Response.Expires = 0;
			Response.CacheControl = "no-cache";
			Response.AppendHeader("Pragma", "No-Cache");

			queryString = Request.RawUrl.Substring (Request.RawUrl.IndexOf ("?") + 1);
			name = Request ["q"];
			name = name.Trim ();

			bool? isdelete = null;

			if (name.StartsWith ("http://") || name.StartsWith ("https://")) {
				isdelete = false;
			} else if (name.StartsWith ("delete")
				&& (name.Contains ("http://") || name.Contains ("https://"))) {
				isdelete = true;
			}
			if (!isdelete.HasValue) {
				SearchResource.searchList.Enqueue (name.Replace("<",""));
				while (SearchResource.searchList.Count > 15) {
					String t;
					SearchResource.searchList.TryDequeue (out t);
				}
			} else {
				name = SearchResource.indexText (name, isdelete.Value);
				SearchResource.urlList.Enqueue (name.Replace("<",""));
				while (SearchResource.urlList.Count > 3) {
					String t;
					SearchResource.urlList.TryDequeue (out t);
				}
			}

		}
	}
}

