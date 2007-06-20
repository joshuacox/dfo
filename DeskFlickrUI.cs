
using System;
using System.Collections;
using System.Threading;
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
    CheckMenuItem checkmenuitem3;
    
    [Glade.Widget]
    ImageMenuItem imagemenuitem5;
    
    [Glade.Widget]
    TreeView treeview1;
    
    [Glade.Widget]
    TreeView treeview2;
    
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
    Label label13;
    
    [Glade.Widget]
    Image image5;
    
    [Glade.Widget]
    Entry entry5;
    
    ToggleToolButton streambutton;
    ToggleToolButton conflictbutton;
    
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
    // Keep track of photos who are modified both here, and in the server.
    private ArrayList _conflictedphotos;
    
    private int leftcurselectedindex;
    private int selectedtab;
    private TargetEntry[] targets;
    private ListStore photoStore;
    private TreeModelFilter filter;
    
    private Thread _connthread;
    
		private DeskFlickrUI() {
		  _albums = new ArrayList();
		  _photos = new ArrayList();
		  _tags = new ArrayList();
		  _conflictedphotos = new ArrayList();
		  
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
		  
		  // The value of stream button in this toolbar is being used by
		  // other initializations. So, this should be positioned _before_ them.
		  SetHorizontalToolBar();
		  
		  // Set Text for the label
		  label1.Text = "Desktop Flickr Organizer";
      label12.Text = "Search: ";
      entry5.Changed += OnFilterEntryChanged;
		  
		  SetLeftTextView();
		  SetLeftTreeView();
		  SetRightTreeView();
		  
		  // Set the menu bar
		  SetMenuBar();
		  SetVerticalBar();
		  SetFlamesWindow();
		  
		  SetIsConnected(0);
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
      if (treeiters.Count > 0) {
        TreeIter curIter = (TreeIter) treeiters[leftcurselectedindex];
        treeview1.Selection.SelectIter(curIter);
      }
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
      if (treeiters.Count > 0) {
        TreeIter curIter = (TreeIter) treeiters[leftcurselectedindex];
        treeview1.Selection.SelectIter(curIter);
      }
      treeview1.ShowAll();
    }
    
    public void RefreshLeftTreeView() {
      if (selectedtab == 0) PopulateAlbums();
      else if (selectedtab == 1) PopulateTags();
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
      // No need to specifically select the album tab, because treeview1's
      // selection automatically triggers repopulation of photos.
      // AlbumTabSelected(null, null);
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
		    foreach (TreePath tp in treeview2.Selection.GetSelectedRows()) {
		      string photoid = ((Photo) _photos[tp.Indices[0]]).Id;
		      string setid = ((Album) _albums[destindex]).SetId;
		      
		      // Scenario 1: New photo is being added to set. Insert a new 
		      // entry to the table, and mark it dirty.
		      // Scenario 2: A photo is already present in the set. The photo
		      // is not dirty, and not deleted. User
		      // tries to insert the same photo in the set again. Nothing
		      // should happen.
		      // Scenario 3: A photo has been deleted from the set. User
		      // tries to add the photo back to the set. The isdirty bit should
		      // be cleared, same for isdeleted bit.
		      bool exists = PersistentInformation.GetInstance()
		          .HasAlbumPhoto(photoid, setid);
		      if (exists) {
		        bool isdirty = PersistentInformation.GetInstance()
		            .IsAlbumToPhotoDirty(photoid, setid);
		        bool isdeleted = PersistentInformation.GetInstance()
		            .IsAlbumToPhotoMarkedDeleted(photoid, setid);
		            
		        if (!isdirty && !isdeleted) continue;
		        if (isdeleted) {
		          PersistentInformation.GetInstance()
		            .MarkPhotoForDeletionFromAlbum(photoid, setid, false);
		        }
		      } else {
		        PersistentInformation.GetInstance()
		            .AddPhotoToAlbum(photoid, setid);
		        PersistentInformation.GetInstance()
		            .SetAlbumToPhotoDirty(photoid, setid, true);
		      }
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
        if (!streambutton.Active && !conflictbutton.Active) {
          photos = PersistentInformation.GetInstance().GetPhotosForAlbum(album.SetId);
        }
      }
      else if (selectedtab == 1) {
        string tag = (string) _tags[leftcurselectedindex];
        textview2.Buffer.Text = "";
        if (!streambutton.Active && !conflictbutton.Active) {
          photos = PersistentInformation.GetInstance().GetPhotosForTag(tag);
        }
      }
      if (!streambutton.Active && !conflictbutton.Active) {
        PopulatePhotosTreeView(photos);
      }
		}
		
		public void OnDoubleClickLeftView(object o, RowActivatedArgs args) {
		  if (selectedtab != 0) return; // if not albums, then don't care.
		  int index = args.Path.Indices[0];
		  Album album = (Album) _albums[index];
		  AlbumEditorUI.FireUp(album);
		}
		
		private string GetCol1Data(Photo p) {
      System.Text.StringBuilder pangoTitle = new System.Text.StringBuilder();
      pangoTitle.AppendFormat(
          "<span font_desc='Times Bold 10'>{0}</span>", p.Title);
      pangoTitle.AppendLine();
      pangoTitle.AppendFormat(
          "<span font_desc='Times Italic 10'>{0}</span>", 
          p.Description);
      return pangoTitle.ToString();
		}
		
		private string GetCol2Data(Photo p) {
      return String.Format(
            "<span font_desc='Times 10'>{0}</span>", 
            p.TagString);
		}
		
		private string GetCol3Data(Photo p) {
		  return String.Format(
            "<span font_desc='Times 10'>{0}</span>", p.PrivacyInfo);
		}
		
		private string GetCol4Data(Photo p) {
		  return String.Format(
            "<span font_desc='Times 10'>{0}</span>", p.LicenseInfo);
		}
		
		public void PopulatePhotosTreeView(ArrayList photos) {
		  _photos.Clear();
		  // Note that whatever change we do to photoStore, i.e. addition or
		  // editing of entries, filter is triggered. So, we'll create a store
		  // first, and then just assign this new store to the global photoStore.
		  ListStore store = new Gtk.ListStore(
		                                 typeof(Gdk.Pixbuf), typeof(string),
		                                 typeof(string), typeof(string), 
		                                 typeof(string));
		  foreach (Photo p in photos) {
		    store.AppendValues(p.Thumbnail, GetCol1Data(p), GetCol2Data(p),
		                            GetCol3Data(p), GetCol4Data(p));
		    _photos.Add(p);
		  }
		  photoStore = store;
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
    
    // If the user has used search tab, the path
    // provided here wouldn't exactly show the absolute position in the
    // model, because entries would have been filtered out.
    private Photo GetPhoto(TreePath path) {
      int filteredindex = path.Indices[0];
      ArrayList filteredPhotos = new ArrayList();
      foreach (Photo p in _photos) {
        if (FilterPhoto(p)) filteredPhotos.Add(p);
      }
      return (Photo) filteredPhotos[filteredindex];
    }
    
    private int GetPhotoIndex(TreePath path) {
      int filteredindex = path.Indices[0];
      int findex = -1;
      int realindex = -1;
      foreach (Photo p in _photos) {
        realindex++;
        if (FilterPhoto(p)) findex++;
        if (findex == filteredindex) {
          return realindex;
        }
      }
      return -1;
    }
    
    private void OnFilterEntryChanged(object o, EventArgs args) {
      filter.Refilter();
    }
    
    // This method is used only to refresh the photos in the right pane.
    // Its not used to populate photos when a tab selection is made,
    // or stream button is pressed. It doesn't populate new different
    // set of photos, just is intended to refresh the already existant ones.
    public void RefreshPhotosTreeView() {
      ArrayList photos = null;
      if (streambutton.Active) {
        photos = PersistentInformation.GetInstance().GetAllPhotos();
      }
      else if (conflictbutton.Active) {
        photos = new ArrayList();
        foreach (string photoid in _conflictedphotos) {
          Photo p = PersistentInformation.GetInstance().GetPhoto(photoid);
          photos.Add(p);
        }
      }
      else if (selectedtab == 0) {
        string setid = ((Album) _albums[leftcurselectedindex]).SetId;
        photos = 
            PersistentInformation.GetInstance().GetPhotosForAlbum(setid);
        
      }
      else if (selectedtab == 1) {
        string tag = (string) _tags[leftcurselectedindex];
        photos =
            PersistentInformation.GetInstance().GetPhotosForTag(tag);
      }
      PopulatePhotosTreeView(photos);
    }
    
    public void UpdatePhotos() {
      foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
        Photo p = GetPhoto(path);
        TreeIter iter;
        photoStore.GetIter(out iter, path);
        photoStore.SetValue(iter, 1, GetCol1Data(p));
        photoStore.SetValue(iter, 2, GetCol2Data(p));
        photoStore.SetValue(iter, 3, GetCol3Data(p));
        photoStore.SetValue(iter, 4, GetCol4Data(p));
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
		  PhotoEditorUI.FireUp(selectedphotos, conflictbutton.Active);
		}
				
		private void OnEditButtonClicked(object o, EventArgs args) {
		  
		  // Check right tree first.
		  if (treeview2.Selection.GetSelectedRows().Length > 0) {
		    ArrayList selectedphotos = new ArrayList();
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          Photo p = GetPhoto(path);
          selectedphotos.Add(p);
        }
        PhotoEditorUI.FireUp(selectedphotos, conflictbutton.Active);
  		} // check left tree now.
  		else if (treeview1.Selection.GetSelectedRows().Length > 0) {
  		  TreePath path = treeview1.Selection.GetSelectedRows()[0];
  		  Album album = (Album) _albums[path.Indices[0]];
  		  AlbumEditorUI.FireUp(album);
  		}
		}
		
		private void OnStreamButtonClicked(object o, EventArgs args) {
		  if (streambutton.Active) {
		    conflictbutton.Active = false;
		    PopulatePhotosTreeView(PersistentInformation.GetInstance().GetAllPhotos());
		  } else {
		    RefreshLeftTreeView();
		  }
		}
		
		private void OnConflictButtonClicked(object o, EventArgs args) {
		  if (conflictbutton.Active) {
		    streambutton.Active = false;
	      ArrayList photos = new ArrayList();
	      // Get the photoid from these source (server) photos, and 
	      // populate the tree with the ones that we have.
	      foreach (Photo src in _conflictedphotos) {
	        Photo p = PersistentInformation.GetInstance().GetPhoto(src.Id);
	        photos.Add(p);
	      }
	      PopulatePhotosTreeView(photos);
		  } else {
		    RefreshLeftTreeView();
		  }
		}
		
		private void SetHorizontalToolBar() {
		  ToolButton editbutton = new ToolButton(Stock.Edit);
		  editbutton.IsImportant = true;
		  editbutton.Sensitive = true;
		  editbutton.Clicked += new EventHandler(OnEditButtonClicked);
		  toolbar1.Insert(editbutton, -1);
		  
		  streambutton = new ToggleToolButton(Stock.SelectAll);
		  streambutton.IsImportant = true;
		  streambutton.Sensitive = true;
		  streambutton.Label = "Show Stream";
		  streambutton.Clicked += new EventHandler(OnStreamButtonClicked);
		  toolbar1.Insert(streambutton, -1);
		  
		  conflictbutton = new ToggleToolButton(Stock.DialogWarning);
		  conflictbutton.IsImportant = true;
		  conflictbutton.Sensitive = true;
		  conflictbutton.Label = "Conflicts";
		  conflictbutton.Clicked += new EventHandler(OnConflictButtonClicked);
		  toolbar1.Insert(conflictbutton, -1);
		  UpdateToolBarButtons();
		}
		  	
  	public void UpdateToolBarButtons() {
  	  int countphotos = PersistentInformation.GetInstance().GetCountPhotos();
  	  streambutton.Label = "Show Stream (" + countphotos + ")";
  	  conflictbutton.Label = "Conflicts (" + _conflictedphotos.Count + ")";
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
		
		public void SetIsConnected(int connected) {
		  if (connected == 2) {
		    label13.Markup = "<span weight='bold'>Busy</span>";
		    image5.Stock = Stock.Connect;
		  } else if (connected == 1) {
		    label13.Markup = "<span weight='bold'>Online</span>";
		    image5.Stock = Stock.Connect;
		  } else if (connected == 0) {
		    label13.Markup = "<span weight='bold'>Offline</span>";
		    image5.Stock = Stock.Disconnect;
		  }
		}
  	
  	private void SetMenuBar() {
      // Connect menu item.
      imagemenuitem2.Activated += new EventHandler(ConnectionHandler);
      
      // Quit menu item.
      imagemenuitem5.Activated += new EventHandler(OnQuitEvent);
      checkmenuitem3.Activated += new EventHandler(OnWorkOfflineEvent);
    }
    
		private void OnQuitEvent (object sender, EventArgs args) {

  	  ResponseType result = ResponseType.Yes;
  	  // If the connection is busy, notify the user that he's aborting
  	  // the connection.
  	  if (FlickrCommunicator.GetInstance().IsBusy) {
  	    MessageDialog md = new MessageDialog(
  	        null, DialogFlags.DestroyWithParent, MessageType.Question,
  	        ButtonsType.YesNo, 
  	        "Connection is busy. Do you really wish to abort the connection"
  	        + " and quit the application?");
  	    result = (ResponseType) md.Run();
  	    md.Destroy();
  	  }
  	  if (result == ResponseType.Yes) {
  	    if (_connthread != null) _connthread.Abort();
  		  Application.Quit ();
  		}
		}
		
		// Connect the Signals defined in Glade
  	private void OnWindowDeleteEvent (object sender, DeleteEventArgs args) {
  	  OnQuitEvent(null, null);
  		args.RetVal = true;
  	}
  	
  	private bool IsWorkOffline {
  	  get {
  	    return checkmenuitem3.Active;
  	  }
  	}
  	
  	private void OnWorkOfflineEvent(object sender, EventArgs args) {
  	  Console.WriteLine("Work offline status changed: " + IsWorkOffline);
  	  if (IsWorkOffline) { // Work OFF-line.
  	    if (FlickrCommunicator.GetInstance().IsBusy) {
  	      checkmenuitem3.Active = false;
  	      MessageDialog md = new MessageDialog(
  	          null, DialogFlags.DestroyWithParent, MessageType.Info,
  	          ButtonsType.Close, "Connection busy... Please try again later.");
  	      md.Run();
  	      md.Destroy();
  	      return;
  	    }
  	    FlickrCommunicator.GetInstance().Disconnect();
  	    if (_connthread != null) _connthread.Abort();
  	    _connthread = null;
  	    SetStatusLabel("Done. Working Offline");
  	  } else { // Work ON-line.
  	    if (_connthread != null) return;
  	    ThreadStart job = new ThreadStart(PeriodicallyTryConnecting);
  	    _connthread = new Thread(job);
  	    _connthread.Start();
  	  }
  	}
  	
  	// This method is only supposed to be executed inside a new thread.
  	// It utilizes Thread.Sleep method, which may cause the GUI to stall,
  	// if called directly from the main Gtk Application thread.
  	private void PeriodicallyTryConnecting() {
  	  if (IsWorkOffline) return;
  	  while (!IsWorkOffline) {
  	    FlickrCommunicator comm = FlickrCommunicator.GetInstance();
  	    while (comm.IsBusy) {
  	      Console.WriteLine("Already busy.. waiting for 5 mins");
  	      Thread.Sleep(5*60*1000); // wait for the connection to be finished.
  	    }
  	    Gtk.Application.Invoke (delegate {
  	      SetStatusLabel("Attempting connection...");
  	    });
  	    comm.AttemptConnection();
  	    if (comm.IsConnected) comm.RoutineCheck();
  	    Gtk.Application.Invoke (delegate {
  	      UpdateToolBarButtons();
  	      SetStatusLabel("Done. Counting time for reconnection...");
  	      SetLimitsProgressBar(10);
  	    });
  	    for (int i=0; i<10; i++) {
  	      Thread.Sleep(60*1000);
  	      Gtk.Application.Invoke (delegate {
  	        IncrementProgressBar(1);
  	      });
  	    }
  	  }
  	}
  	
  	private void ConnectionHandler(object sender, EventArgs e) {
  	  if (IsWorkOffline) {
  	    checkmenuitem3.Active = false;
  	  }
  	  if (FlickrCommunicator.GetInstance().IsConnected) {
  	    return;
  	  }
      OnWorkOfflineEvent(null, null);
  	}
  	
  	public void SetStatusLabel(string status) {
  	  label1.Text = status;
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
		      // Scenario 1: A user drags a new photo to set, the photo is added,
		      // and the entry is marked dirty. Now, he removes the photo from
		      // the set. So, the entry should be deleted.
		      // Scenario 2: A user deletes a photo from set. The photo is
		      // marked for deletion. Somehow (quite
		      // impossible though), he tries to delete the photo from set again.
		      // The entry should not be removed, basically, status quo should
		      // maintain.
		      bool isdirty = PersistentInformation.GetInstance()
		                        .IsAlbumToPhotoDirty(photoid, setid);
		      bool isdeleted = PersistentInformation.GetInstance()
		                        .IsAlbumToPhotoMarkedDeleted(photoid, setid);
		                        
		      if (isdirty && !isdeleted) {
		        PersistentInformation.GetInstance()
		                        .DeletePhotoEntryFromAlbum(photoid, setid);
		      } else {
		        PersistentInformation.GetInstance()
		                        .MarkPhotoForDeletionFromAlbum(photoid, setid, true);
		      }
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
    
    public void ClearServerPhotos() {
      _conflictedphotos.Clear();
    }
    
    public void AddServerPhoto(Photo serverphoto) {
      foreach (Photo p in _conflictedphotos) {
        if (p.Id.Equals(serverphoto.Id)) return;
      }
      _conflictedphotos.Add(serverphoto);
    }
    
    public Photo GetServerPhoto(string photoid) {
      foreach (Photo p in _conflictedphotos) {
        if (p.Id.Equals(photoid)) return p;
      }
      return null;
    }
    
    public void RemoveServerPhoto(string photoid) {
      for (int i=0; i < _conflictedphotos.Count; i++) {
        Photo serverphoto = (Photo) _conflictedphotos[i];
        if (serverphoto.Id.Equals(photoid)) {
          _conflictedphotos.RemoveAt(i);
          return;
        }
      }
    }
	}