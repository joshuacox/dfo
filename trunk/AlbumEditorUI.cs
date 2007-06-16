using System;
using Gtk;
using Glade;

	public class AlbumEditorUI
	{
		
		[Glade.Widget]
		Window window3;
		
		[Glade.Widget]
		Label label8;
		
		[Glade.Widget]
		Label label9;
		
		[Glade.Widget]
		Label label10;
		
		[Glade.Widget]
		Entry entry3;
		
		[Glade.Widget]
		Entry entry4;
		
		[Glade.Widget]
		Button button8;
		
		[Glade.Widget]
		Button button7;
		
		[Glade.Widget]
		Image image4;
		
		private Album _album;
		
		public AlbumEditorUI(Album album)
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window3", null);
		  gxml.Autoconnect (this);
		  
		  this._album = album;
		  window3.Title = String.Format("Editing information for {0}", album.Title);
		  window3.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  
		  label8.Text = "Edit";
		  label9.Text = "Title: ";
		  label10.Text = "Description: ";
		  
		  entry3.Text = album.Title;
		  entry4.Text = album.Desc;
		  
		  entry3.Changed += new EventHandler(OnTitleChanged);
		  entry4.Changed += new EventHandler(OnDescriptionChanged);
		  
		  button7.Clicked += new EventHandler(OnCancelButtonClicked);
		  button8.Clicked += new EventHandler(OnSaveButtonClicked);
		  
		  image4.Pixbuf = PersistentInformation.GetInstance()
		                    .GetSmallImage(album.PrimaryPhotoid);
		  window3.ShowAll();
		}
		
		public void OnTitleChanged(object o, EventArgs args) {
		  _album.Title = entry3.Text;
		}
		
		public void OnDescriptionChanged(object o, EventArgs args) {
		  _album.Desc = entry4.Text;
		}
		
		public void OnCancelButtonClicked(object o, EventArgs args) {
		  window3.Destroy();
		}
		
		public void OnSaveButtonClicked(object o, EventArgs args) {
		  PersistentInformation.GetInstance().SaveAlbum(_album);
		  window3.Destroy();
		  DeskFlickrUI.GetInstance().PopulateAlbums();
		}
	}