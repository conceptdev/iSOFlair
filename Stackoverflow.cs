
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using System.Text;

namespace iSOFlair
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}

	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
	{
		private int stackoverflowId = -1;
		
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// If you have defined a view, add it here:
			// window.AddSubview (navigationController.View);
			
			var prefs = NSUserDefaults.StandardUserDefaults;
			
			Console.WriteLine("Prefs: soid=" + prefs.StringForKey("soid"));
			string stackoverflowIdString = prefs.StringForKey("soid");
			
			if (!int.TryParse(stackoverflowIdString, out stackoverflowId))
			{
				stackoverflowId = 22656; // Jon Skeet
				// "25673"; // Craig Dunn
			}
			
			
			buttonUpdate.TouchDown += delegate {
				Update();	
			};
			
			if (System.IO.File.Exists("cache.txt"))
			{ 
				Load();	
			} else
			{	
				Update();
			}
			window.MakeKeyAndVisible ();

			return true;
		}
		
		private string profileUrl = "";
		
		protected void Load()
		{
			Console.WriteLine("Loading from cache.txt");
			string cache = System.IO.File.ReadAllText("cache.txt");
			string[] cacheItems = cache.Split('?');
			labelUsername.Text = cacheItems[0];
			labelPoints.Text = cacheItems[1];
			profileUrl = cacheItems[2];
			labelLastUpdated.Text=cacheItems[3];
		}
		protected void Update(){
			Console.WriteLine("Loading from web");
			WebClient wc = new WebClient();
			Uri uri = new Uri(
			                  String.Format("http://stackoverflow.com/users/flair/{0}.html"
			                                , stackoverflowId));
			byte[] bytes = null;
			try
			{
				bytes = wc.DownloadData(uri);
			} 
			catch (Exception ex)
			{
				Console.WriteLine("Internet connection failed: " + ex.Message);
				
				labelConnectionError.Text = "Update failed: cannot connect to the Internet.";
				return;
			} 
			string result = Encoding.UTF8.GetString(bytes);
			
			labelUsername.Text = result;
			
			int startpos = result.IndexOf("<a");
			startpos = result.IndexOf("href=",startpos) + 6;
			int endpos = result.IndexOf("\"",startpos);
			profileUrl = result.Substring(startpos, endpos-startpos);
			
			startpos = result.IndexOf("img src",endpos) + 9;
			endpos = result.IndexOf("\"",startpos);
			string gravatarUrl = result.Substring(startpos, endpos-startpos);
			uri = new Uri(gravatarUrl);
			wc.DownloadFile(uri, "gravatar.png");
			
			UIImage img = UIImage.FromFile("gravatar.png");
			imageviewGravatar.AnimationImages = new UIImage[] {img};
			imageviewGravatar.AnimationDuration = 10;
			imageviewGravatar.StartAnimating();
			
			startpos = result.IndexOf("username",endpos);
			startpos = result.IndexOf("_blank",endpos) + 8;
			endpos = result.IndexOf("<",startpos);
			
			string username = result.Substring(startpos, endpos-startpos);
			
			labelUsername.Text = username;
			
			startpos = result.IndexOf("reputation score",endpos) + 18;
			endpos = result.IndexOf("<",startpos);
			
			string points = result.Substring(startpos, endpos-startpos);
			string goldBadges="", silverBadges="", bronzeBadges="";
			startpos = result.IndexOf("gold badge",endpos);
			if (startpos > 0)
			{
				startpos --; // remove preceding space
				endpos = result.IndexOf("\"",startpos-10) + 1;
				goldBadges = result.Substring(endpos, startpos-endpos);
				Console.WriteLine(" " + goldBadges);
				labelGoldBadges.Text = goldBadges + " gold badges";
			}
			startpos = result.IndexOf("silver badge",endpos);
			if (startpos > 0)
			{
				startpos --; // remove preceding space
				endpos = result.IndexOf("\"",startpos-10) + 1;
				silverBadges = result.Substring(endpos, startpos-endpos);
				labelSilverBadges.Text = silverBadges + " silver badges";
			}			
			startpos = result.IndexOf("bronze badge",endpos);
			if (startpos > 0)
			{
				startpos --; // remove preceding space
				endpos = result.IndexOf("\"",startpos-10) + 1;
				bronzeBadges = result.Substring(endpos, startpos-endpos);
				labelBronzeBadges.Text = bronzeBadges + " bronze badges";
			}	
			
			Console.WriteLine("gold " + goldBadges);
			Console.WriteLine("silver " + silverBadges);
			Console.WriteLine("bronze " + bronzeBadges);
			
			
			
			
			labelPoints.Text = points;
			string dateUpdated = DateTime.Now.ToString("dd-MMM-yy hh:mm:ss");
			labelLastUpdated.Text = dateUpdated;
			
			string cache = String.Format("{0}?{1}?{2}?{3}"
			                             , username
			                             , points
			                             , profileUrl
			                             , dateUpdated);
			System.IO.File.WriteAllText("cache.txt", cache);
		}
		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}
