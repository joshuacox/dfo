
using System;
using System.Collections;
using Gtk;
using Glade;

	public class DeskFlickrUI
	{
	  [Glade.Widget]
    Window window1;
    
    [Glade.Widget]
    Label label1;
    
    [Glade.Widget]
    ProgressBar progressbar1;
    
    [Glade.Widget]
    ImageMenuItem imagemenuitem2;
    
    [Glade.Widget]
    ImageMenuItem imagemenuitem5;
    
    [Glade.Widget]
    TreeView treeview1;
    
    [Glade.Widget]
    TreeView treeview2;
    
    [Glade.Widget]
    HBox hbox2;
    
    [Glade.Widget]
    TextView textview2;
    
    [Glade.Widget]
    Toolbar toolbar1;
    
    private static DeskFlickrUI deskflickr = null;
    public static string ICON_PATH = "icons/Font-Book.ico";
    public static string THUMBNAIL_PATH = "icons/FontBookThumbnail.png";
    public static string FLICKR_ICON = "icons/flickr_logo.gif";
    
    // Needed to store the order of albums and photos shown in
    // left and right panes respectively. These two variables just store
    // the ids of sets and photos respectively.
    private ArrayList _albums;
    private ArrayList _photos;
    
    private int leftcurselectedindex;
    
		private DeskFlickrUI() {
		  _albums = new ArrayList();
		  _photos = new ArrayList();
		  leftcurselectedindex = 0;
		}
		
		public void CreateGUI() {
		  Application.Init();
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window1", null);
		  gxml.Autoconnect (this);
		  
		  // Set Text for the label
		  label1.Text = "Desktop Flickr Organizer";
      
		  // Set the menu bar
		  this.SetMenuBar();
		  // hbox2.Remove(progressbar1);
		  
		  SetLeftTextView();
		  SetLeftTreeView();
		  SetRightTreeView();
		  SetToolBar();
		  // Set window properties
		  window1.SetIconFromFile(ICON_PATH);
		  window1.DeleteEvent += OnWindowDeleteEvent;
		  window1.ShowAll();
		  Application.Run();
		}
	
    public void PopulateAlbumTreeView() {
      Gtk.ListStore albumStore = (Gtk.ListStore) treeview1.Model;
      if (albumStore == null) {
        albumStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(string));
      } else {
        albumStore.Clear();
      }
      this._albums.Clear();
      
      // Temporarily store treeiters
      ArrayList treeiters = new ArrayList();
      foreach (Album a in PersistentInformation.GetInstance().GetAlbums()) {
        Photo primaryPhoto = PersistentInformation.GetInstance().
            GetPhoto(a.PrimaryPhotoid);
        Gdk.Pixbuf thumbnail = null;
        if (primaryPhoto != null) {
          thumbnail = primaryPhoto.Thumbnail;
        }
        
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        info.AppendFormat(
            "<span font_desc='Times Bold 10'>{0}</span>", a.Title);
        info.AppendLine();
        info.AppendFormat(
            "<span font_desc='Times Bold 10'>{0} pics</span>", a.NumPics);
        TreeIter curiter = albumStore.AppendValues(thumbnail, info.ToString());
        treeiters.Add(curiter);
        // Now add the setid to albums.
        this._albums.Add(a.SetId);
      }
      treeview1.Model = albumStore;
      TreeIter curIter = (TreeIter) treeiters[leftcurselectedindex];
      treeview1.Selection.SelectIter(curIter);
      treeview1.ShowAll();
    }
    
    private void SetLeftTextView() {
      TextTag tag = new TextTag("headline");
      tag.Font = "Times Bold 12";
      tag.WrapMode = WrapMode.Word;
      // tag.BackgroundGdk = new Gdk.Color(0x99, 0x66, 0x00);
      textview2.Buffer.TagTable.Add(tag);
      
      tag = new TextTag("paragraph");
      tag.Font = "Times Italic 10";
      tag.WrapMode = WrapMode.Word;
      tag.ForegroundGdk = new Gdk.Color(0, 0, 0x99);
      textview2.Buffer.TagTable.Add(tag);
    }
    
		private void SetLeftTreeView() {
		  // Set tree view 1
		  Gtk.CellRendererText titleRenderer = new Gtk.CellRendererText();
		  titleRenderer.WrapMode = Pango.WrapMode.Word;
		  titleRenderer.WrapWidth = 200;
		  
      treeview1.AppendColumn ("Icon", new Gtk.CellRendererPixbuf(), "pixbuf", 0);
      treeview1.AppendColumn ("Title", titleRenderer, "markup", 1);
      treeview1.HeadersVisible = false;
      treeview1.Model = null;
      treeview1.Selection.Changed += OnSelectionLeftTreeChanged;
      treeview1.RowActivated += new RowActivatedHandler(OnDoubleClickLeftView);
      PopulateAlbumTreeView();
		}
		
		// This method is a general purpose method, meant to take of changes
		// done to albums, or tags, shown in the left pane.
		private void OnSelectionLeftTreeChanged(object o, EventArgs args) {
		
      TreePath[] treepaths = ((TreeSelection)o).GetSelectedRows();
      if (treepaths.Length > 0) {
        leftcurselectedindex = (treepaths[0]).Indices[0];
      } else {
        return;
      }
      
      TextBuffer buf = textview2.Buffer;
      buf.Clear();
      // It is obvious that there would be at least one album, because
      // otherwise left treeview model wouldn't be formed, and the user
      // would have nothing to click upon.
      string setid = (string) _albums[leftcurselectedindex];
      Album album = PersistentInformation.GetInstance().GetAlbum(setid);
      // Set the buffer here.
      buf.Text = "\n" + album.Title + "\n\n" + album.Desc;
      TextIter start;
      TextIter end;
      start = buf.GetIterAtLine(1);
      end = buf.GetIterAtLine(2);
      buf.ApplyTag("headline", start, end);
      buf.ApplyTag("paragraph", end, buf.EndIter);
      textview2.Buffer = buf;
      textview2.ShowAll();
        
      // Set photos here
      PopulatePhotosTreeView(setid);
		}
		
		public void OnDoubleClickLeftView(object o, RowActivatedArgs args) {
		  int index = args.Path.Indices[0];
		  string setid = (string)_albums[index];
		  Album album = PersistentInformation.GetInstance().GetAlbum(setid);
		  AlbumEditorUI editor = new AlbumEditorUI(album);
		}
		
		public void PopulatePhotosTreeView(string setid) {
		  Gtk.ListStore photoStore = (Gtk.ListStore) treeview2.Model;
		  if (photoStore == null) {
		    photoStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(string),
		                                   typeof(string), typeof(string), 
		                                   typeof(string));
		  } else {
		    photoStore.Clear();
		  }
		  _photos.Clear();
		  foreach (Photo p in PersistentInformation.
		      GetInstance().GetPhotosForAlbum(setid)) {
		      
        System.Text.StringBuilder pangoTitle = new System.Text.StringBuilder();
        pangoTitle.AppendFormat(
            "<span font_desc='Times Bold 10'>{0}</span>", p.Title);
        pangoTitle.AppendLine();
        pangoTitle.AppendFormat(
            "<span font_desc='Times Italic 10'>{0}</span>", 
            p.Description);
        
        string pangoTags = String.Format(
            "<span font_desc='Times 10'>{0}</span>", 
            p.TagString);
        string pangoPrivacy = String.Format(
            "<span font_desc='Times 10'>{0}</span>", p.PrivacyInfo);
        
        string pangoLicense = String.Format(
            "<span font_desc='Times 10'>{0}</span>", p.LicenseInfo);
            
		    photoStore.AppendValues(p.Thumbnail, pangoTitle.ToString(), 
		                            pangoTags, pangoPrivacy, pangoLicense);
		    _photos.Add(p.Id);
		  }
		  treeview2.Model = photoStore;
		  treeview2.ShowAll();
    }
    
    public void RefreshPhotosTreeView() {
      string setid = (string)_albums[leftcurselectedindex];
      PopulatePhotosTreeView(setid);
    }
    
		public void SetRightTreeView() {
		  Gtk.CellRendererText titleRenderer = new Gtk.CellRendererText();
		  titleRenderer.WrapWidth = 400;
		  titleRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  Gtk.CellRendererText tagRenderer = new Gtk.CellRendererText();
		  tagRenderer.WrapWidth = 150;
		  tagRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  Gtk.CellRendererText licenseRenderer = new Gtk.CellRendererText();
		  licenseRenderer.WrapWidth = 200;
		  licenseRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  treeview2.AppendColumn("Thumbnail", new Gtk.CellRendererPixbuf(), "pixbuf", 0);
		  treeview2.AppendColumn("Title/Description", titleRenderer, "markup", 1);
		  treeview2.AppendColumn("Tags", tagRenderer, "markup", 2);
		  treeview2.AppendColumn("Viewable", tagRenderer, "markup", 3);
		  treeview2.AppendColumn("License", licenseRenderer, "markup", 4);
		  treeview2.HeadersVisible = true;
		  treeview2.Model = null;
		  // Alternate rows have same color.
		  treeview2.RulesHint = true;
		  // Select multiple photos together.
		  treeview2.Selection.Mode = Gtk.SelectionMode.Multiple;
		  // Handle double clicks on photos.
		  treeview2.RowActivated += new RowActivatedHandler(OnDoubleClickPhoto);
		}
		
		private void OnDoubleClickPhoto(object o, RowActivatedArgs args) {
		  ArrayList selectedphotos = new ArrayList();
		  int index = args.Path.Indices[0];
      Photo p = PersistentInformation.GetInstance()
                                     .GetPhoto((string)_photos[index]);
      selectedphotos.Add(p);
		  PhotoEditorUI photoeditor = new PhotoEditorUI(selectedphotos);
		}
				
		private void EditButtonClicked(object o, EventArgs args) {
		  
		  // Check right tree first.
		  if (treeview2.Selection.GetSelectedRows().Length > 0) {
		    ArrayList selectedphotos = new ArrayList();
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          string photoid = (string) _photos[path.Indices[0]];
          Photo p = PersistentInformation.GetInstance()
                                         .GetPhoto(photoid);
          selectedphotos.Add(p);
        }
        PhotoEditorUI photoeditor = new PhotoEditorUI(selectedphotos);
  		} // check left tree now.
  		else if (treeview1.Selection.GetSelectedRows().Length > 0) {
  		  TreePath path = treeview1.Selection.GetSelectedRows()[0];
  		  string setid = (string) _albums[path.Indices[0]];
  		  Album a = PersistentInformation.GetInstance().GetAlbum(setid);
  		  AlbumEditorUI editor = new AlbumEditorUI(a);
  		}
		}
		
		private void SetToolBar() {
		  ToolButton editbutton = new ToolButton(Stock.Edit);
		  editbutton.IsImportant = true;
		  editbutton.Sensitive = true;
		  editbutton.Clicked += new EventHandler(EditButtonClicked);
		  toolbar1.Insert(editbutton, -1);
		  
//		  ToolButton quitbutton = new ToolButton(Stock.Quit);
//		  quitbutton.IsImportant = true;
//		  quitbutton.Sensitive = true;
//		  quitbutton.Clicked += new EventHandler(OnWindowDeleteEvent);
//		  toolbar1.Insert(quitbutton, -1);
		}
		
		public static DeskFlickrUI GetInstance() {
		  if (deskflickr == null) {
		    deskflickr = new DeskFlickrUI();
		  }
		  return deskflickr;
		}
		
		public void ShowAllInWindow() {
		  window1.ShowAll();
		}
		
		public void SetLimitsProgressBar(int max) {
		  progressbar1.Adjustment.Lower = 0;
		  progressbar1.Adjustment.Upper = max;
		  progressbar1.Adjustment.Value = 0;
		}
		
		public void SetValueProgressBar(int val) {
		  progressbar1.Adjustment.Value = val;
		}
		
		public void IncrementProgressBar(int delta) {
		  if (progressbar1.Adjustment.Upper > progressbar1.Adjustment.Value) {
		    progressbar1.Adjustment.Value += delta;
		  }
		  progressbar1.Text = String.Format("{0}/{1}",
		      progressbar1.Adjustment.Value, progressbar1.Adjustment.Upper);
		}
		
		public void SetProgressBarText(string status) {
		  progressbar1.Text = status;
		}
		
		// Connect the Signals defined in Glade
  	private void OnWindowDeleteEvent (object sender, EventArgs a) {
  		Application.Quit ();
  		// a.RetVal = true;
  	}
  	
  	private void ConnectionHandler(object sender, EventArgs e) {
  	  SetStatusLabel("Connecting to Flickr...");
  	  FlickrCommunicator.GetInstance();
  	}
  	
  	public void SetStatusLabel(string status) {
  	  label1.Text = status;
  	}
  	
  	private void SetMenuBar() {
      // Connect menu item.
      imagemenuitem2.Activated += new EventHandler(ConnectionHandler);
      
      // Quit menu item.
      imagemenuitem5.Activated += new EventHandler(OnWindowDeleteEvent);
    }
	}
