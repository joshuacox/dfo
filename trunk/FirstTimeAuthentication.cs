
using System;
using System.Threading;
using Gtk;
using Glade;
using FlickrNet;

	public class FirstTimeAuthentication
	{
	  [Glade.Widget]
	  Dialog dialog1;
	  
	  // Message to User window
		[Glade.Widget]
		TextView textview1;
		
		// Done authentication button
		[Glade.Widget]
		Button button1;
		
		// More Info button
		[Glade.Widget]
		Button button2;
		
		[Glade.Widget]
		Gtk.Image image1;
		
	  [Glade.Widget]
		Gtk.Image image2;
		
		private Flickr flickrObj;
		private string frob;
		
		private string _apikey = "413051629a00e140b4f448fd22d715d2";
    private string _secret = "9aa8b5eef280f665";
    
		private FirstTimeAuthentication()
		{
      Glade.XML gxml = new Glade.XML (null, "organizer.glade", "dialog1", null);
		  gxml.Autoconnect (this);
		  
		  textview1.Buffer.Text = "The application needs to be authorized"
		      + " before it can read or modify your photos and data on Flickr."
		      + " Authorization is a simple process which takes place in web"
		      + " browser. When you're finished, return to this window to"
		      + " complete authorization by clicking on Done.";
		  
		  button1.Clicked += new EventHandler(OnButtonPressDone);
		  button2.Clicked += new EventHandler(OnButtonPressMoreInfo);
		  
		  // Set images.
		  Gdk.Pixbuf pixbuf1 = new Gdk.Pixbuf(DeskFlickrUI.THUMBNAIL_PATH);
		  image1.Pixbuf = pixbuf1;
		  Gdk.Pixbuf pixbuf2 = new Gdk.Pixbuf(DeskFlickrUI.FLICKR_ICON);
		  image2.Pixbuf = pixbuf2;

		  dialog1.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  dialog1.ShowAll();
		  ConnectToFlickr();
		  Console.WriteLine("Should show up the new gui now.");
		}
		
		public static void FireUp() {
		  new FirstTimeAuthentication();
		}
		
		private void OnButtonPressDone(object sender, EventArgs e) {
		  Auth auth = this.flickrObj.AuthGetToken(frob);
		  string token = auth.Token;
		  PersistentInformation.GetInstance().Token = token;
		  Console.WriteLine("Set the token in gconf.");
		  dialog1.Destroy();
		}
		
		private void OnButtonPressMoreInfo(object sender, EventArgs e) {
		  string moreInfoUrl = 
		      "http://www.flickr.com/services/api/auth.howto.desktop.html";
		  System.Diagnostics.Process.Start(moreInfoUrl);
		}
		
		private void ConnectToFlickr() {
		  Console.WriteLine("Inside connect to flickr.");

		  this.flickrObj = new Flickr(this._apikey, this._secret);
		  frob = this.flickrObj.AuthGetFrob();
		  string url = this.flickrObj.AuthCalcUrl(frob, 
		                                          AuthLevel.Read | AuthLevel.Write);
		  System.Diagnostics.Process.Start(url);
		}
	}
