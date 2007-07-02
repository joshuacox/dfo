
using System;
using Gtk;
using Glade;

	public class AboutUI
	{
		[Glade.Widget]
		Window window4;
		
		[Glade.Widget]
		Image logoimage;
		
		[Glade.Widget]
		Label dfolabel;
		
		[Glade.Widget]
		Notebook notebook2;
		
		[Glade.Widget]
		TextView abouttextview;
		
		[Glade.Widget]
		TextView attribtextview;
		
		[Glade.Widget]
		Button closebutton;
		
		private AboutUI()
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window4", null);
		  gxml.Autoconnect (this);
		  
      Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(DeskFlickrUI.ICON_PATH);
		  logoimage.Pixbuf = pixbuf;
		  dfolabel.Markup = "<span font_desc='Sans Bold 16'>Desktop Flickr Organizer v0.4</span>";
		  
		  notebook2.SetTabLabelText(notebook2.CurrentPageWidget, "About");
		  notebook2.NextPage();
		  notebook2.SetTabLabelText(notebook2.CurrentPageWidget, "Attribution");
		  notebook2.NextPage();
		  notebook2.SetTabLabelText(notebook2.CurrentPageWidget, "License");
		  notebook2.Page = 0;
		  
		  closebutton.Label = "Close";
		  closebutton.Clicked += new EventHandler(OnCloseButtonClicked);
		  
		  SetAboutInfo();
		  SetAttributionInfo();
		  
		  window4.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  window4.ShowAll();
		}
		
		public static void FireUp() {
		  new AboutUI();
		}
		
		private void OnCloseButtonClicked(object o, EventArgs args) {
		  window4.Destroy();
		}
		
		private void SetAboutInfo() {
      System.Text.StringBuilder strb = new System.Text.StringBuilder();
      strb.AppendLine();
      strb.Append("Desktop based Flickr Organizer by Manish Rai Jain.");
      strb.AppendLine();
      strb.AppendLine();
      strb.Append("Organize, upload, and download photos all through a single interface.");
      strb.AppendLine();
      strb.AppendLine();
      strb.Append("Feedback and Discussion:");
      strb.AppendLine();
      strb.Append("\t dfo-users@groups.google.com");
      strb.AppendLine();
      strb.Append("\t http://groups.google.com/group/dfo-users");
      strb.AppendLine();
      strb.AppendLine();
      strb.Append("Homepage: ");
      strb.AppendLine();
      strb.Append("\t http://code.google.com/p/dfo/");
      strb.AppendLine();
      strb.AppendLine();
      strb.AppendLine();
      strb.Append("(c) 2007, Manish Rai Jain");
      abouttextview.Buffer.Text = strb.ToString();
		}
		
		private void SetAttributionInfo() {
		  System.Text.StringBuilder strb = new System.Text.StringBuilder();
		  strb.AppendLine();
		  strb.Append("FlickrNet library by Sam August");
		  strb.AppendLine();
		  strb.Append("\t http://www.codeplex.com/FlickrNet");
		  strb.AppendLine();
		  strb.AppendLine();
		  strb.Append("Font Book logo by AveTenebrae (Laurent Baumann)");
		  strb.AppendLine();
		  strb.Append("\t http://ave.ambitiouslemon.com");
		  attribtextview.Buffer.Text = strb.ToString();
		}
	}
