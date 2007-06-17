
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
    
    [Glade.Widget]
    EventBox eventbox1;

    [Glade.Widget]
    EventBox eventbox2;
    
    [Glade.Widget]
    EventBox eventbox3;
    
    [Glade.Widget]
    Label label11;
    
    [Glade.Widget]
    Label label12;
    
    [Glade.Widget]
    Entry entry5;
    
    public static string ICON_PATH = "icons/Font-Book.ico";
    public static string THUMBNAIL_PATH = "icons/FontBookThumbnail.png";
    public static string FLICKR_ICON = "icons/flickr_logo.gif";
    
    private static DeskFlickrUI deskflickr = null;
    private static Gdk.Color tabselectedcolor = new Gdk.Color(0x6A, 0x79, 0x7A);
    private static Gdk.Color tabcolor = new Gdk.Color(0xCC, 0xCC, 0xB8);
    
    // Needed to store the order of albums and photos shown in
    // left and right panes respectively. These two variables used to
    // store just the ids. However, an afterthought suggests that if they 
    // store the real photo and set objects, it would be more efficient. So,
    // changing to that.
    private ArrayList _albums;
    private ArrayList _photos;
    private ArrayList _tags;

    private int leftcurselectedindex;
    private int selectedtab;
    private TargetEntry[] targets;

    private TreeModelFilter filter;
    
		private DeskFlickrUI() {
		  _albums = new ArrayList();
		  _photos = new ArrayList();
		  _tags = new ArrayList();
		  
		  leftcurselectedindex = 0;
		  selectedtab = 0;
      targets = new TargetEntry[] {
        new TargetEntry("text/plain", 0, 0)
      };
		}
		
		public void CreateGUI() {
		  Application.Init();
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window1", null);
		  gxml.Autoconnect (this);
		  
		  // Set Text for the label
		  label1.Text = "Desktop Flickr Organizer";
      label12.Text = "Search: ";
      entry5.Changed += OnFilterEntryChanged;
		  // Set the menu bar
		  this.SetMenuBar();
		  // hbox2.Remove(progressbar1);
		  
		  SetLeftTextView();
		  SetLeftTreeView();
		  SetRightTreeView();
		  SetHorizontalToolBar();
		  SetVerticalBar();
		  SetFlamesWindow();
		  
		  // Set window properties
		  window1.SetIconFromFile(ICON_PATH);
		  window1.DeleteEvent += OnWindowDeleteEvent;
		  window1.ShowAll();
		  Application.Run();
		}
	
    public void PopulateAlbums() {
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
        this._albums.Add(a);
      }
      treeview1.Model = albumStore;
      TreeIter curIter = (TreeIter) treeiters[leftcurselectedindex];
      treeview1.Selection.SelectIter(curIter);
      treeview1.ShowAll();
    }
    
    public void PopulateTags() {
      Gtk.ListStore tagStore = (Gtk.ListStore) treeview1.Model;
      if (tagStore == null) {
        tagStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(string));
      } else {
        tagStore.Clear();
      }
      this._tags.Clear();
      
      ArrayList treeiters = new ArrayList();
      foreach (Tag tag in PersistentInformation.GetInstance().GetAllTags()) {
        Photo p = 
            PersistentInformation.GetInstance().GetSinglePhotoForTag(tag.Name);
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        info.AppendFormat(
            "<span font_desc='Times Bold 10'>{0}</span>", tag.Name);
        info.AppendLine();
        info.AppendFormat(
            "<span font_desc='Times Bold 10'>{0} pics</span>", tag.NumberOfPics);
        TreeIter curiter = tagStore.AppendValues(p.Thumbnail, info.ToString());
        treeiters.Add(curiter);
        // Now add the tag name to _tags.
        this._tags.Add(tag.Name);
      }
      treeview1.Model = tagStore;
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
      
      // Drag and drop mechanism
      treeview1.EnableModelDragDest(targets, Gdk.DragAction.Copy);
      treeview1.DragDataReceived += OnPhotoDraggedForAddition;
                       
      // Can use CursorChanged if need to get an event on every click.
      treeview1.Selection.Changed += OnSelectionLeftTree;
      treeview1.RowActivated += new RowActivatedHandler(OnDoubleClickLeftView);
      AlbumTabSelected(null, null);
		}
		
		// Don't really care about the selection data sent. Because, we can
		// rather easily just look at the selected photos.
		private void OnPhotoDraggedForAddition(object o, DragDataReceivedArgs args) {
		  TreePath path;
		  TreeViewDropPosition pos;
		  // This line determines the destination row.
		  treeview1.GetDestRowAtPos(args.X, args.Y, out path, out pos);
		  if (path == null) return;
		  int destindex = path.Indices[0];
		  
		  if (selectedtab == 0) { // albums
		    Console.WriteLine(treeview2.Selection.GetSelectedRows().Length);
		    foreach (TreePath tp in treeview2.Selection.GetSelectedRows()) {
		      string photoid = ((Photo) _photos[tp.Indices[0]]).Id;
		      string setid = ((Album) _albums[destindex]).SetId;
		      PersistentInformation.GetInstance()
		          .AddPhotoToAlbum(photoid, setid);
		      PersistentInformation.GetInstance()
		          .SetAlbumToPhotoDirty(photoid, setid, true);
		    }
		    PopulateAlbums();
		  } else if (selectedtab == 1) { // tags
		    foreach (TreePath tp in treeview2.Selection.GetSelectedRows()) {
		      string photoid = ((Photo) _photos[tp.Indices[0]]).Id;
		      string tag = (string) _tags[destindex];
		      PersistentInformation.GetInstance().InsertTag(photoid, tag);
		      PersistentInformation.GetInstance().SetPhotoDirty(photoid, true);
		    }
		    PopulateTags();
		  }
		}
		
		// This method is a general purpose method, meant to take of changes
		// done to albums, or tags, shown in the left pane.
		private void OnSelectionLeftTree(object o, EventArgs args) {
      TreePath[] treepaths = ((TreeSelection)o).GetSelectedRows();
      if (treepaths.Length > 0) {
        leftcurselectedindex = (treepaths[0]).Indices[0];
      } else return;

      ArrayList photos = null;
      if (selectedtab == 0) {
        TextBuffer buf = textview2.Buffer;
        buf.Clear();
        // It is obvious that there would be at least one album, because
        // otherwise left treeview model wouldn't be formed, and the user
        // would have nothing to click upon.
        
        Album album = (Album) _albums[leftcurselectedindex];
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
        photos = PersistentInformation.GetInstance().GetPhotosForAlbum(album.SetId);
      } 
      else if (selectedtab == 1) {
        string tag = (string) _tags[leftcurselectedindex];
        photos = PersistentInformation.GetInstance().GetPhotosForTag(tag);
        textview2.Buffer.Text = "";
      }
      PopulatePhotosTreeView(photos);
		}
		
		public void OnDoubleClickLeftView(object o, RowActivatedArgs args) {
		  if (selectedtab != 0) return; // if not albums, then don't care.
		  int index = args.Path.Indices[0];
		  Album album = (Album) _albums[index];
		  AlbumEditorUI editor = new AlbumEditorUI(album);
		}
		
		public void PopulatePhotosTreeView(ArrayList photos) {
		
		  ListStore photoStore = new Gtk.ListStore(
		                                   typeof(Gdk.Pixbuf), typeof(string),
		                                   typeof(string), typeof(string), 
		                                   typeof(string));
		  _photos.Clear();
		  foreach (Photo p in photos) {
		    
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
		    _photos.Add(p);
		  }
		  filter = new TreeModelFilter(photoStore, null);
		  filter.VisibleFunc = new TreeModelFilterVisibleFunc(FilterPhotos);
		  treeview2.Model = filter;
		  treeview2.ShowAll();
    }
    
    private bool FilterPhotos(TreeModel model, TreeIter iter) {
      int index = model.GetPath(iter).Indices[0];
      Photo p = (Photo) _photos[index];
      return FilterPhoto(p);
    }
    
    private bool FilterPhoto(Photo p) {
      string query = entry5.Text;
      if (query == "") return true;
      bool flag = false;
      System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase;
      if (p.Title.IndexOf(query, comp) > -1) flag = true;
      else if (p.Description.IndexOf(query, comp) > -1) flag = true;
      else if (p.TagString.IndexOf(query, comp) > -1) flag = true;
      else if (p.PrivacyInfo.IndexOf(query, comp) > -1) flag = true;
      else if (p.LicenseInfo.IndexOf(query, comp) > -1) flag = true;
      return flag;
    }
    
    private Photo GetPhoto(TreePath path) {
      int filteredindex = path.Indices[0];
      ArrayList filteredPhotos = new ArrayList();
      foreach (Photo p in _photos) {
        if (FilterPhoto(p)) filteredPhotos.Add(p);
      }
      return (Photo) filteredPhotos[filteredindex];
    }
    
    private void OnFilterEntryChanged(object o, EventArgs args) {
      filter.Refilter();
    }
    
    public void RefreshPhotosTreeView() {
      if (selectedtab == 0) {
        string setid = ((Album) _albums[leftcurselectedindex]).SetId;
        ArrayList photos = 
            PersistentInformation.GetInstance().GetPhotosForAlbum(setid);
        PopulatePhotosTreeView(photos);
      } 
      else if (selectedtab == 1) {
        string tag = (string) _tags[leftcurselectedindex];
        ArrayList photos =
            PersistentInformation.GetInstance().GetPhotosForTag(tag);
        PopulatePhotosTreeView(photos);
      }
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
		  
		  // Allow dragging
		  treeview2.EnableModelDragSource(Gdk.ModifierType.Button1Mask, 
		                                  targets, Gdk.DragAction.Copy);
		  treeview2.DragDataGet += GetDataOfPhotoDragged;
		  treeview2.DragBegin += BeginDragSetIcon;
		}
		
		private void GetDataOfPhotoDragged(object o, DragDataGetArgs args) {
      Gdk.Atom[] types = args.Context.Targets;
      byte[] bytes = System.Text.Encoding.UTF8.GetBytes("");
		  args.SelectionData.Set(types[0], 8, bytes, bytes.Length);
		}
		
		private void BeginDragSetIcon(object o, DragBeginArgs args) {
		  TreePath path = treeview2.Selection.GetSelectedRows()[0];
		  Photo p = GetPhoto(path);
		  Drag.SetIconPixbuf(args.Context, p.Thumbnail, 0, 0);
		}
		
		private void OnDoubleClickPhoto(object o, RowActivatedArgs args) {
		  ArrayList selectedphotos = new ArrayList();
      Photo p = GetPhoto(args.Path);
      selectedphotos.Add(p);
		  PhotoEditorUI photoeditor = new PhotoEditorUI(selectedphotos);
		}
				
		private void EditButtonClicked(object o, EventArgs args) {
		  
		  // Check right tree first.
		  if (treeview2.Selection.GetSelectedRows().Length > 0) {
		    ArrayList selectedphotos = new ArrayList();
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          Photo p = GetPhoto(path);
          selectedphotos.Add(p);
        }
        PhotoEditorUI photoeditor = new PhotoEditorUI(selectedphotos);
  		} // check left tree now.
  		else if (treeview1.Selection.GetSelectedRows().Length > 0) {
  		  TreePath path = treeview1.Selection.GetSelectedRows()[0];
  		  Album a = (Album) _albums[path.Indices[0]];
  		  AlbumEditorUI editor = new AlbumEditorUI(a);
  		}
		}
		
		private void SetHorizontalToolBar() {
		  ToolButton editbutton = new ToolButton(Stock.Edit);
		  editbutton.IsImportant = true;
		  editbutton.Sensitive = true;
		  editbutton.Clicked += new EventHandler(EditButtonClicked);
		  toolbar1.Insert(editbutton, -1);
		}
		
		private void SetVerticalBar() {
      Label albumLabel = new Label();
      albumLabel.Markup = "<span foreground='white'>Sets</span>";
      albumLabel.Angle = 90;
      eventbox1.Add(albumLabel);
      eventbox1.ButtonPressEvent += AlbumTabSelected;
      
      Label tagLabel = new Label();
      tagLabel.Markup = "<span foreground='white'>Tags</span>";
      tagLabel.Angle = 90;
      eventbox2.Add(tagLabel);
      eventbox2.ButtonPressEvent += TagTabSelected;
      
      // Set the default tab.
      AlbumTabSelected(null, null);
		}
		
		private void AlbumTabSelected(object o, EventArgs args) {
		  eventbox1.ModifyBg(StateType.Normal, tabselectedcolor);
		  eventbox2.ModifyBg(StateType.Normal, tabcolor);
		  selectedtab = 0;
		  leftcurselectedindex = 0;
		  label11.Markup = 
		      "<span style='italic'>Drop photos here to remove from set.</span>";
		  PopulateAlbums();
		}
		
		private void TagTabSelected(object o, EventArgs args) {
		  eventbox1.ModifyBg(StateType.Normal, tabcolor);
		  eventbox2.ModifyBg(StateType.Normal, tabselectedcolor);
		  selectedtab = 1;
		  leftcurselectedindex = 0;
		  textview2.Buffer.Text = "";
		  label11.Markup = 
		      "<span style='italic'>Drop photos here to remove tag.</span>";
		  PopulateTags();
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
    
    private void SetFlamesWindow() {
      eventbox3.ModifyBg(StateType.Normal, tabcolor);
      Gtk.Drag.DestSet(eventbox3, Gtk.DestDefaults.All, 
                       targets, Gdk.DragAction.Copy);
      eventbox3.DragDataReceived += OnPhotoDraggedForDeletion;
    }
    
    private void OnPhotoDraggedForDeletion(object o, DragDataReceivedArgs args) {
      if (selectedtab == 0) { // albums
        string setid = ((Album) _albums[leftcurselectedindex]).SetId;
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
		      string photoid = GetPhoto(path).Id;
		      PersistentInformation.GetInstance()
		          .MarkPhotoForDeletionFromAlbum(photoid, setid);
        }
        PopulateAlbums();
      } else if (selectedtab == 1) { // tags
        string tag = (string) _tags[leftcurselectedindex];
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
		      string photoid = GetPhoto(path).Id;
		      PersistentInformation.GetInstance().DeleteTag(photoid, tag);
		    }
		    PopulateTags();
      } // if else ends here.
    }
    
	}
