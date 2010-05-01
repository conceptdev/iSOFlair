
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO; // for Path
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace iSOFlair
{
	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
	{
		List<TrilogySite> list = TrilogySite.GetAll();
		int siteCount = 0; // TODO: remove 'hardcoded' fixed number of four sites
		int maxPageVisited = 0; // used to set the 'badge' number: number of un-viewed panels
		
		/// <summary>
		/// http://wiki.monotouch.net/HowTo/Files/HowTo%3a_Store_Files
		/// </summary>
		string documentsDirectory="";
		
		/// <summary>
		/// When shutting down, set the badge number to 'unviewed' panel count
		/// </summary>
		public override void WillTerminate (UIApplication application)
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 3 - maxPageVisited;
		}

		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			documentsDirectory = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			documentsDirectory = documentsDirectory.Replace("Documents", "tmp"); // use tmp dir, don't need iTunes to back-up
			
			// load preferences in a  loop
			var prefs = NSUserDefaults.StandardUserDefaults;
			foreach (var site in list)
			{
				site.SiteId = prefs.StringForKey(site.PreferencesPrefix + "id");
				siteCount++;
				Console.WriteLine(site.PreferencesPrefix + "id" + ": " + site.SiteId);	
			}
			UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = true;
			
			CreatePanels();
			
			// unviewed panel count starts at 4, and is decremented when view is changed
			// by a swipe or pagercontrol touch
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 4;
			
			// handler for when the user navigates via the pager, rather than swiping
			pageControl.ValueChanged += delegate(object sender, EventArgs e) {
				var pc = (UIPageControl)sender;
				double fromPage = Math.Floor((scrollView.ContentOffset.X - scrollView.Frame.Width / 2) / scrollView.Frame.Width) + 1;
				var toPage = pc.CurrentPage;
				var pageOffset = scrollView.ContentOffset.X + scrollView.Frame.Width;
				Console.WriteLine("fromPage " + fromPage + " toPage " + toPage);	
				if (fromPage > toPage)
					pageOffset = scrollView.ContentOffset.X - scrollView.Frame.Width;
				PointF p = new PointF(pageOffset, 0);
				scrollView.SetContentOffset(p,true);
				list[toPage].ViewController.BecomeFirstResponder(); // so it can "accept" shakes
				// so we can change the 'badge' on the application icon
				maxPageVisited = maxPageVisited<toPage?toPage:maxPageVisited;
			};
			
			window.MakeKeyAndVisible ();

			return true;
		}


		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
		
		/// <summary>
		/// Create an SOViewController for each 'site'
		/// </summary>
		/// <remarks>
		/// Pager sample from
		/// http://simon.nureality.ca/?p=135
		/// 
		/// Attempts to load data from cache txt files, doesn't do WebClient requests.
		/// 
		/// We store each SOViewController in the list so that we can
		/// call BecomeFirstResponder when we change views... this is because
		/// the 'shake gesture' goes to the FirstResponder
		/// </remarks>
		private void CreatePanels()
		{
		    scrollView.Scrolled += ScrollViewScrolled;
			
		    int count = siteCount;
		    RectangleF scrollFrame = scrollView.Frame;
		    scrollFrame.Width = scrollFrame.Width * count;
		    scrollView.ContentSize = scrollFrame.Size;
		
			SOViewController defaultFirstResponderView = null;
		    for (int i=0; i<count; i++)
		    {
		        RectangleF frame = scrollView.Frame;
		        PointF location = new PointF();
		        location.X = frame.Width * i;
		
		        frame.Location = location;
		        
				SOViewController cellController = new SOViewController();
				NSBundle.MainBundle.LoadNib("SOViewController", cellController, null);
				cellController.View.Frame = frame;
				cellController.Site = list[i];
				cellController.DocumentsDirectory = documentsDirectory;
				cellController.WireUp();
				cellController.LoadFromCache();
				
				list[i].ViewController = cellController;
				scrollView.AddSubview(cellController.View);
				if (i==0) defaultFirstResponderView = cellController; // set the first one to FirstResponder
		    }
		    pageControl.Pages = count;
			defaultFirstResponderView.BecomeFirstResponder();
		}

		/// <summary>
		/// After a swipe has occurred, calculate the page based on width
		/// and coordinates, then update the pager (correct 'dot' highlighted)
		/// </summary>
		private void ScrollViewScrolled (object sender, EventArgs e)
		{
			Console.Write("Swiped from " + pageControl.CurrentPage);
		    double page = Math.Floor((scrollView.ContentOffset.X - scrollView.Frame.Width / 2) / scrollView.Frame.Width) + 1;
		    int pageNumber = (int)page;
			if (pageNumber >= 0 && pageNumber <list.Count)
			{
				pageControl.CurrentPage = pageNumber;
				Console.WriteLine(" to " + pageControl.CurrentPage);
				list[(int)page].ViewController.BecomeFirstResponder(); // so it can "accept" shakes
				// so we can change the 'badge' on the application icon
				maxPageVisited = maxPageVisited<pageControl.CurrentPage?pageControl.CurrentPage:maxPageVisited;
			}
			else
			{
				Console.WriteLine("Scrolled a little too far, to page " + page);
			}
		}
	}
}