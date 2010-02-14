
using System;
using System.Collections.Generic;

namespace Swipe01
{
	/// <summary>
	/// Data about the four Stack Overflow sites
	/// </summary>
	public class TrilogySite
	{
		public string FlairUrl {get;set;}
		public string ProfileUrl {get;set;}
		public string PreferencesPrefix {get;set;}
		public string Logo {get;set;}
		public string SiteId {get;set;}
		/// <summary>
		/// TODO: allow only three, two or one views; not just hardcoded to four (future)
		/// </summary>
		public bool HasValue {get;set;}
		
		public SOViewController ViewController{get;set;}
		
		public TrilogySite ()
		{}
		
		public static List<TrilogySite> GetAll()
		{
			List<TrilogySite> list = new List<TrilogySite>();
			list.Add(new TrilogySite{
					FlairUrl="http://stackoverflow.com/users/flair/{0}.html"
			        , ProfileUrl="http://stackoverflow.com/users/{0}"
					, PreferencesPrefix="so"
					, Logo = "SOlogo.png"
					, HasValue = false
			});
			list.Add(new TrilogySite{
					FlairUrl="http://serverfault.com/users/flair/{0}.html"
			        , ProfileUrl="http://serverfault.com/users/{0}"
			        , PreferencesPrefix="sf"
					, Logo = "SFlogo.png"
					, HasValue = false
			});
			list.Add(new TrilogySite{
					FlairUrl="http://superuser.com/users/flair/{0}.html"
			        , ProfileUrl="http://superuser.com/users/{0}"
			        , PreferencesPrefix="su"
					, Logo = "SUlogo.png"
					, HasValue = false
			});
			list.Add(new TrilogySite{
					FlairUrl="http://meta.stackoverflow.com/users/flair/{0}.html"
			        , ProfileUrl="http://meta.stackoverflow.com/users/{0}"
			        , PreferencesPrefix="me"
					, Logo = "MElogo.png"
					, HasValue = false
			});
			return list;
		}
	}
}
