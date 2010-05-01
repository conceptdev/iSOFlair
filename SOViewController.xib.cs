
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using System.Text;

namespace iSOFlair
{
	/// <summary>
	/// Flair page 'template', re-used four times
	/// </summary>
	public partial class SOViewController : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public SOViewController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public SOViewController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public SOViewController ()
		{
			Initialize ();
		}

		void Initialize ()
		{
			
		}
		
		#endregion
		
		private TrilogySite _site = null;
		
		public TrilogySite Site {
			get{ return _site;}
			set
			{
				_site = value;
				
				imageLogo.Image = UIImage.FromFile(Site.Logo);
			}
		}
		/// <summary>
		/// Path to save image and cache
		/// </summary>
		public string DocumentsDirectory{get;set;}
		
		public string Username {
			get {return labelUsername.Text;}
			set {labelUsername.Text = value;}
		}
		public string Points
		{
			get {return labelPoints.Text;}
			set {labelPoints.Text = value;}
		}
		private string ImagePath{
			get {
				return Path.Combine (DocumentsDirectory, Site.PreferencesPrefix+"gravatar.png");
			}
		}
		private string CachePath{
			get {
				return Path.Combine (DocumentsDirectory, Site.PreferencesPrefix+"cache.txt");
			}
		}
		/// <summary>
		/// Setup the button delegates
		/// </summary>
		public void WireUp ()
		{
			buttonUpdate.TouchDown += delegate {
				buttonUpdate.Hidden = true;
				buttonSafari.Hidden = true;
				ScreenCapture();
				buttonUpdate.Hidden = false;
				buttonSafari.Hidden = false;
			};
			buttonSafari.TouchDown += delegate{
				UIApplication.SharedApplication.OpenUrl(new NSUrl(String.Format(Site.ProfileUrl 
			                                , Site.SiteId)));
			};
		}
		
		#region respond to shaking (OS3+)
		// also requires you to put
		// UIApplication.SharedApplication.ApplicationSupportsShakeToEdit = true;
		// in Main.cs : FinishedLaunching()
		public override bool CanBecomeFirstResponder {
			get {
				return true;
			}
		}
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			this.BecomeFirstResponder();
		}
		public override void ViewWillDisappear (bool animated)
		{
			this.ResignFirstResponder();
			base.ViewWillDisappear (animated);
		}
		public override void MotionEnded (UIEventSubtype motion, UIEvent evt)
		{
			Console.WriteLine("Motion detected");
			if (motion ==  UIEventSubtype.MotionShake)
			{
				Console.WriteLine("and was a shake");
				labelLastUpdated.Text = "All shook up! Updating..."; // never appears
				// Do your application-specific shake response here...
				Update();			
			}
		}
		#endregion
		/// <summary>
		/// Load data from txt file (cached from last webclient request)
		/// </summary>
		public bool LoadFromCache()
		{
			bool loaded = false;
			Console.WriteLine("Loading from cache.txt");
			try {
				string cache = "";
	        		if (File.Exists(CachePath))
				{
					cache = System.IO.File.ReadAllText(CachePath);
				}
				if (cache == "")
				{
					labelConnectionError.Text ="Cache is empty. Shake to refresh data.";
					labelUsername.Text = "";
					labelPoints.Text="";
					labelGoldBadges.Text = "";
					labelSilverBadges.Text="";
					labelBronzeBadges.Text="";
					textDebug.Text ="";
				}
				else
				{
					string[] cacheItems = cache.Split('?');
					labelUsername.Text = cacheItems[0];
					labelPoints.Text = cacheItems[1];
					labelGoldBadges.Text = cacheItems[2] + " gold badges";
					labelSilverBadges.Text = cacheItems[3] + " silver badges";
					labelBronzeBadges.Text = cacheItems[4] + " bronze badges";
					//profileUrl = cacheItems[5];
					labelLastUpdated.Text=cacheItems[6] + " from cache";
					textDebug.Text =""; // blank out, nothing loaded from the web
					
					if (File.Exists(ImagePath))
					{
						UIImage img = UIImage.FromFileUncached(ImagePath); // TODO: WHY didn't FromFile work, when FromFileUncached does?
						if (img != null)
							this.imageAvatar.Image = img;
					}
					loaded = true;
				}
			} 
			catch (Exception ex)
			{
				labelConnectionError.Text ="Cache is invalid. Shake to refresh data.";
				labelUsername.Text = "";
				labelPoints.Text="";
				labelGoldBadges.Text = "";
				labelSilverBadges.Text="";
				labelBronzeBadges.Text="";
				textDebug.Text ="";
			}
			return loaded;
		}
		
		/// <summary>
		/// Update this site data from the web, but downloading the flair, parsing 
		/// and saving in a cache text file
		/// </summary>
		public void Update()
		{
			Console.WriteLine("Loading from web " + Site.SiteId);
			labelLastUpdated.Text = "Loading...";
			
			// Download flair page
			WebClient wc = new WebClient();
			Uri uri = new Uri(String.Format(Site.FlairUrl, Site.SiteId));
			byte[] bytes = null;
			try
			{
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
				bytes = wc.DownloadData(uri);
			} 
			catch (Exception ex)
			{
				Console.WriteLine("Internet connection failed: " + ex.Message);
				textDebug.Text = Environment.NewLine +"DOWNLOADDATA:"
					+Environment.NewLine+ ex.Message;
				labelLastUpdated.Text = "Internet connection failed: " + ex.Message;
				labelConnectionError.Text = "Update failed: cannot connect to the Internet.";
				return;
			} 
			finally
			{
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
			}
			// Parse flair HTML
			try 
			{
				string result = Encoding.UTF8.GetString(bytes);
					
				int startpos = result.IndexOf("<a");
				startpos = result.IndexOf("href=",startpos) + 6;
				int endpos = result.IndexOf("\"",startpos);
				string profileUrl = result.Substring(startpos, endpos-startpos);
				
				startpos = result.IndexOf("img src",endpos) + 9;
				endpos = result.IndexOf("\"",startpos);
				
				// download image
				string gravatarUrl = result.Substring(startpos, endpos-startpos);
				uri = new Uri(gravatarUrl);
				try
				{	
					Console.WriteLine("wc.Download("+uri+","+ImagePath+")");
					wc.DownloadFile (uri, ImagePath);
				} 
				catch (Exception ex1)
				{
					textDebug.Text += Environment.NewLine + "DOWNLOAD IMAGE:"
						+ Environment.NewLine + gravatarUrl
						+ Environment.NewLine + ImagePath
						+ Environment.NewLine + ex1.Message;
				}
				
				// Parse the HTML
				//HACK: yes this is a hack... HtmlAgilityPack would've been smarter.
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
				// load image from file into UIImage
				try
				{
					if (File.Exists(ImagePath))
					{
						//UIImage img = UIImage.FromFile (ImagePath); // WHY does this break???
						UIImage img = UIImage.FromFileUncached(ImagePath); // BUT this works???
						if (img != null)
							this.imageAvatar.Image = img;
					}
					else
					{	
					textDebug.Text += Environment.NewLine + "UIIMAGE NOT EXIST:"
							+ Environment.NewLine + ImagePath;
					}
				} 
				catch (Exception ex2)
				{
						textDebug.Text += Environment.NewLine + "FROM FILE: " 
							+ Environment.NewLine + ImagePath 
							+ Environment.NewLine + ex2.Message;
				}
				Console.WriteLine("gold " + goldBadges);
				Console.WriteLine("silver " + silverBadges);
				Console.WriteLine("bronze " + bronzeBadges);
				
				
				labelPoints.Text = points;
				string dateUpdated = "Last updated " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss");
				labelLastUpdated.Text = dateUpdated;
				
				string cache = String.Format("{0}?{1}?{2}?{3}?{4}?{5}?{6}"
				                             , username
				                             , points
					                         , goldBadges
					                         , silverBadges
					                         , bronzeBadges
				                             , profileUrl
				                             , dateUpdated);
				System.IO.File.WriteAllText (CachePath, cache);
				labelConnectionError.Text = ""; // blank out if we get to here without Exception
			} 
			catch (Exception ex)
			{
				labelLastUpdated.Text = ex.Message;
				textDebug.Text += Environment.NewLine
					+ ex.Message;
			}
		}
				
		/// <summary>
		/// Capture a copy of the current View and:
		/// * re-display in a UIImage control
		/// * save to the Photos collection
		/// * save to disk in the application's Documents folder
		/// </summary>
		public void ScreenCapture()
		{
			Console.WriteLine("start image cap");
			Console.WriteLine("frame" + this.View.Frame.Size);
			UIGraphics.BeginImageContext(View.Frame.Size);
			var documentsDirectory = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			
			var ctx = UIGraphics.GetCurrentContext();
			if (ctx != null)
			{
				Console.WriteLine("ctx not null");
				View.Layer.RenderInContext(ctx);
				Console.WriteLine("render in context");
				UIImage img = UIGraphics.GetImageFromCurrentImageContext();
				Console.WriteLine("get from current content");
				UIGraphics.EndImageContext();
				
				// Set to display in a UIImage control _on_ the view
				//imageLogo.Image = img;
				
				// Save to Photos
				img.SaveToPhotosAlbum((sender, args)=>{
					Console.WriteLine("image saved to Photos");
				 	var av = new UIAlertView("Screenshot saved"
	                                    , "Image saved to "+Environment.NewLine+"Photos : Camera Roll"
	                                    , null
	                                    , "Ok thanks"
	                                    , null);
	            		av.Show();
				});
				
				// thought this "might" overwrite the splashscreen
				//string png = Path.Combine (documentsDirectory, "../iSOFlair.app/Default.png");
				
				// Save to application's Documents folder, kinda pointless except as an example
				// since there is no way to "read" it
				string png = Path.Combine (documentsDirectory, "Screenshot.png");
				NSData imgData = img.AsPNG();
				NSError err = null;
				if (imgData.Save(png, false, out err))
				{
					Console.WriteLine("saved " + png);
				} else {
					Console.WriteLine("not saved" + png + " because" + err.LocalizedDescription);
				}
			}
			else
			{
				Console.WriteLine("ctx null - doesn't happen but wasn't sure at first");
			}
		}
	}
}
