
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
    ProgressBar progressbar2;
    
    [Glade.Widget]
    ImageMenuItem imagemenuitem2;
    
    [Glade.Widget]
    CheckMenuItem checkmenuitem3;
    
    [Glade.Widget]
    MenuItem menuitem3;
    
    [Glade.Widget]
    MenuItem menuitem4;
    
    [Glade.Widget]
    ImageMenuItem imagemenuitem5;
    
    [Glade.Widget]
    MenuItem menuitem2;
    
    [Glade.Widget]
    TreeView treeview1;
    
    [Glade.Widget]
    TreeView treeview2;
    
    [Glade.Widget]
    TextView textview2;
    
    [Glade.Widget]
    VPaned vpaned1;
    
    [Glade.Widget]
    HPaned hpaned1;
    
    [Glade.Widget]
    Toolbar toolbar1;
    
    [Glade.Widget]
    Toolbar toolbar2;
    
    [Glade.Widget]
    EventBox eventbox1;

    [Glade.Widget]
    EventBox eventbox2;
    
    [Glade.Widget]
    EventBox eventbox3;
    
    [Glade.Widget]
    EventBox eventbox4;
    
    [Glade.Widget]
    EventBox eventbox10;
    
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
    
    // Edited photos only check button
    [Glade.Widget]
    CheckButton checkbutton2;
    
    // Commented photos only check button
    [Glade.Widget]
    CheckButton checkbutton4;
    
    // Exclude Privacy and License check button
    [Glade.Widget]
    CheckButton checkbutton3;
    
    // Upload window
    [Glade.Widget]
    EventBox eventbox6;
    Label popuplabel;
    
    // Upload button
    [Glade.Widget]
    MenuItem menuitem5;
    
    // Download button
    [Glade.Widget]
    MenuItem menuitem6;
    
    // Photo count
    [Glade.Widget]
    EventBox eventbox11;
    
    [Glade.Widget]
    Label label19;
    
    ToggleToolButton lockbutton;
    ToggleToolButton streambutton;
    ToggleToolButton conflictbutton;
    ToggleToolButton uploadbutton;
    ToggleToolButton downloadbutton;
    ToolButton syncbutton;
    
    // For toolbar2 on top left
    ToolButton connectbutton;
    ToolButton uploadfilechooserbutton;
    
    Tooltips tips;
    
    private static string BASE_DIR = System.AppDomain.CurrentDomain.BaseDirectory;
    private static string IMAGE_DIR = System.IO.Path.Combine(BASE_DIR, "icons");
    public static string ICON_PATH = System.IO.Path.Combine(IMAGE_DIR, "Font-Book.ico");
    public static string THUMBNAIL_PATH = System.IO.Path.Combine(IMAGE_DIR, "FontBookThumbnail.png");
    public static string SQTHUMBNAIL_PATH = System.IO.Path.Combine(IMAGE_DIR, "FontBookSquareThumbnail.png");
    public static string FLICKR_ICON = System.IO.Path.Combine(IMAGE_DIR, "flickr_logo.gif");
    
    private static DeskFlickrUI deskflickr = null;
    public static Gdk.Color tabselectedcolor = new Gdk.Color(0x6A, 0x79, 0x7A);
    public static Gdk.Color tabcolor = new Gdk.Color(0xCC, 0xCC, 0xB8);
    
    // Needed to store the order of albums and photos shown in
    // left and right panes respectively. These two variables used to
    // store just the ids. However, an afterthought suggests that if they 
    // store the complete photo and set objects, it would be more efficient. So,
    // changing to that.
    private ArrayList _albums;
    private ArrayList _photos;
    private ArrayList _tags;
    private ArrayList _pools; // stores (poolid, pooltitle) entry.
    private ArrayList _blogs; // stores (blogid, blogtitle) entry.
    // Keep track of photos who are modified both here, and in the server.
    private ArrayList _conflictedphotos;
    private Gdk.Pixbuf _nophotothumbnail;
    
    private int leftcurselectedindex;
    private int selectedtab;
    private TargetEntry[] targets;
    private ListStore photoStore;
    private TreeModelFilter filter;
    
    private Thread _connthread;
    private Thread _populatephotosthread;
    private Thread _searchwaitthread;
    private bool _busysearching;
    
    public class SelectedPhoto {
      public Photo photo;
      public string path;
      
      public SelectedPhoto(Photo photo, string path) {
        this.photo = photo;
        this.path = path;
      }
    }
    
    public class BlogSelectedPhoto : SelectedPhoto {
      public BlogEntry blogentry;
      
      public BlogSelectedPhoto(Photo photo, BlogEntry blogentry, string path)
          : base (photo, path) {
        this.blogentry = blogentry;
      }
    }
    
    public enum ModeSelected {
      NormalMode,
      ConflictMode,
      UploadMode,
      BlogMode,
      BlogAndConflictMode
    }
    		
		public DeskFlickrUI.ModeSelected GetMode() {
      DeskFlickrUI.ModeSelected mode = ModeSelected.NormalMode;
      if (selectedtab == 3 && conflictbutton.Active) mode = ModeSelected.BlogAndConflictMode;
      else if (uploadbutton.Active) mode = ModeSelected.UploadMode;
      else if (conflictbutton.Active) mode = ModeSelected.ConflictMode;
      else if (selectedtab == 3) mode = ModeSelected.BlogMode; 
		  return mode;
		}
		
		public Gdk.Pixbuf GetDFOThumbnail() {
		  return _nophotothumbnail;
		}
		
		private DeskFlickrUI() {
		  _albums = new ArrayList();
		  _photos = new ArrayList();
		  _tags = new ArrayList();
		  _pools = new ArrayList();
		  _blogs = new ArrayList();
		  _conflictedphotos = new ArrayList();

		  leftcurselectedindex = 0;
		  selectedtab = 0;
      targets = new TargetEntry[] {
        new TargetEntry("text/uri-list", 0, 0)
      };
		}
		
		public void CreateGUI() {
		  Application.Init();
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window1", null);
		  gxml.Autoconnect (this);
		  
		  // Wao! Loading an image from file, didn't work when it was located
		  // in object constructor i.e. DeskFlickrUI(). Shifting it to this
		  // place, magically works!
		  _nophotothumbnail = new Gdk.Pixbuf(SQTHUMBNAIL_PATH);
      
		  tips = new Tooltips();
		  
		  // Popup upload window, and label box.
		  eventbox6.ModifyBg(StateType.Normal, tabcolor);
		  
		  // The value of stream button in this toolbar is being used by
		  // other initializations. So, this should be positioned _before_ them.
		  SetHorizontalToolBar();
		  SetTopLeftToolBar();
		  
		  // Set Text for the label
		  label1.Text = "Desktop Flickr Organizer";
      label12.Markup = "<span weight='bold'>Search: </span>";
      label19.Text = "";
      Gdk.Color greycolor = new Gdk.Color(0x7F, 0x7C, 0x7C);
      eventbox11.ModifyBg(StateType.Normal, greycolor);
      
      // Set Flames window label size.
      label11.Wrap = true;
      int height;
      int width;
      eventbox3.GetSizeRequest(out width, out height); 
      label11.SetSizeRequest(width, height);

      tips.SetTip(eventbox3, "Flames Window", "Flames Window");
      tips.Enable();
      // Set upload window label.
      popuplabel = new Label();
      
      entry5.Changed += new EventHandler(OnFilterEntryChanged);
		  checkbutton2.Toggled += new EventHandler(OnFilterEntryChanged);
		  checkbutton3.Toggled += new EventHandler(OnFilterEntryChanged);
		  checkbutton4.Toggled += new EventHandler(OnFilterEntryChanged);
		  
		  SetLeftTextView();
		  SetLeftTreeView();
		  SetRightTreeView();
		  
		  // Set the menu bar
		  SetMenuBar();
		  SetVerticalBar();
		  SetFlamesWindow();
		  
		  SetIsConnected(0);
		  progressbar2.Text = "Upload Status";
		  // Set window properties
		  window1.SetIconFromFile(ICON_PATH);
		  window1.DeleteEvent += OnWindowDeleteEvent;
		  RestoreWindow();
		  window1.ShowAll();
		  Application.Run();
		}
	  
	  private void RestoreWindow() {
		  int height = PersistentInformation.GetInstance().WindowHeight;
		  int width = PersistentInformation.GetInstance().WindowWidth;
		  if (width != 0 && height != 0) window1.Resize(width, height);
		  
		  int vpos = PersistentInformation.GetInstance().VerticalPosition;
		  if (vpos != 0) vpaned1.Position = vpos;
		  
		  int hpos = PersistentInformation.GetInstance().HorizontalPosition;
		  if (hpos != 0) hpaned1.Position = hpos;
	  }
	  
	  private string GetInfoAlbum(Album a) {
      System.Text.StringBuilder info = new System.Text.StringBuilder();
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0}</span>", 
          Utils.EscapeForPango(a.Title));
      info.AppendLine();
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0} pics</span>", a.NumPics);
      return info.ToString();
	  }
	  
    public void PopulateAlbums() {
      UpdateFlameWindowLabel();
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
        Gdk.Pixbuf thumbnail = _nophotothumbnail;
        if (primaryPhoto != null) {
          thumbnail = primaryPhoto.Thumbnail;
        }
        
        TreeIter curiter = albumStore.AppendValues(thumbnail, GetInfoAlbum(a));
        treeiters.Add(curiter);
        // Now add the setid to albums.
        this._albums.Add(a);
      }
      treeview1.Model = albumStore;
      DoSelection(treeiters);
      treeview1.ShowAll();
    }
    
    private void DoSelection(ArrayList treeiters) {
      if (treeiters.Count > 0) {
        // Scenario: There is only a single photo having a particular tag,
        // which appears at the end of the tag list. The user removes the
        // photo and the tag stops existing. Hence, the number of tag
        // entries have fallen below the selected tag index.
        if (leftcurselectedindex >= treeiters.Count) {
          leftcurselectedindex = treeiters.Count - 1;
        }
        TreeIter curiter = (TreeIter) treeiters[leftcurselectedindex];
        treeview1.Selection.SelectIter(curiter);
      }
    }
    
    private string GetInfoTag(string tag) {
      int numpics = PersistentInformation.GetInstance().GetCountPhotosForTag(tag);
      return GetInfoTag(tag, numpics.ToString());
    }
    
    private string GetInfoTag(string tag, string numpics) {
      System.Text.StringBuilder info = new System.Text.StringBuilder();
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0}</span>", tag);
      info.AppendLine();
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0} pics</span>", numpics);
      return info.ToString();
    }
    
    public void PopulateTags() {
      UpdateFlameWindowLabel();
      Gtk.ListStore tagStore = (Gtk.ListStore) treeview1.Model;
      if (tagStore == null) {
        tagStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(string));
      } else {
        tagStore.Clear();
      }
      this._tags.Clear();
      
      ArrayList treeiters = new ArrayList();
      foreach (PersistentInformation.Entry entry in 
                        PersistentInformation.GetInstance().GetAllTags()) {
        string tag = entry.entry1;
        string numpics = entry.entry2;
        Photo p = PersistentInformation.GetInstance().GetSinglePhotoForTag(tag);
        Gdk.Pixbuf thumbnail = _nophotothumbnail;
        if (p != null) {
          thumbnail = p.Thumbnail;
        }
        TreeIter curiter = tagStore.AppendValues(thumbnail, GetInfoTag(tag, numpics));
        treeiters.Add(curiter);
        // Now add the tag name to _tags.
        this._tags.Add(tag);
      }
      treeview1.Model = tagStore;
      DoSelection(treeiters);
      treeview1.ShowAll();
    }
    
    private string GetInfoPool(PersistentInformation.Entry entry) {
      int numpics = PersistentInformation.GetInstance()
                                         .GetPhotoidsForPool(entry.entry1).Count;
      return GetInfoPool(entry, numpics);
    }
    
    private string GetInfoPool(PersistentInformation.Entry entry, int numpics) {
      System.Text.StringBuilder info = new System.Text.StringBuilder();
      string pooltitle = entry.entry2;
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0}</span>", 
          Utils.EscapeForPango(pooltitle));
      info.AppendLine();
      info.AppendFormat(
          "<span font_desc='Times Bold 10'>{0} pics</span>", numpics);
      return info.ToString();
    }
    
    public void PopulatePools() {
      UpdateFlameWindowLabel();
      Gtk.ListStore poolStore = (Gtk.ListStore) treeview1.Model;
      if (poolStore == null) {
        poolStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(String));
      } else {
        poolStore.Clear();
      }
      this._pools.Clear();
      
      ArrayList treeiters = new ArrayList();
      foreach (PersistentInformation.Entry entry 
                    in PersistentInformation.GetInstance().GetAllPools()) {
        Photo p = PersistentInformation.GetInstance().GetSinglePhotoForPool(entry.entry1);
        Gdk.Pixbuf thumbnail = _nophotothumbnail;
        if (p != null) thumbnail = p.Thumbnail;
        TreeIter curiter = poolStore.AppendValues(thumbnail, GetInfoPool(entry));
        treeiters.Add(curiter);
        this._pools.Add(entry);
      }
      treeview1.Model = poolStore;
      DoSelection(treeiters);
      treeview1.ShowAll();
    }
    
    private string GetInfoBlog(PersistentInformation.Entry entry) {
      System.Text.StringBuilder info = new System.Text.StringBuilder();
      string blogid = entry.entry1;
      string blogtitle = entry.entry2;
      info.AppendFormat("<span font_desc='Times Bold 10'>{0}</span>", blogtitle);
      info.AppendLine();
      int numentries = 
          PersistentInformation.GetInstance().GetEntriesForBlog(blogid).Count;
      info.AppendFormat("<span font_desc='Times Bold 10'>{0} entries</span>", numentries);
      return info.ToString();
    }
    
    private void PopulateBlogs() {
      UpdateFlameWindowLabel();
      Gtk.ListStore blogStore = (Gtk.ListStore) treeview1.Model;
      if (blogStore == null)
          blogStore = new Gtk.ListStore(typeof(Gdk.Pixbuf), typeof(String));
      else blogStore.Clear();
      this._blogs.Clear();
      ArrayList treeiters = new ArrayList();
      foreach (PersistentInformation.Entry entry
                  in PersistentInformation.GetInstance().GetAllBlogs()) {
        Gdk.Pixbuf thumbnail = DeskFlickrUI.GetInstance().GetDFOThumbnail();
        TreeIter curiter = blogStore.AppendValues(thumbnail, GetInfoBlog(entry));
        treeiters.Add(curiter);
        this._blogs.Add(entry);
      }
      treeview1.Model = blogStore;
      DoSelection(treeiters);
      treeview1.ShowAll();
    }
    
    public bool IsAlbumTabSelected() {
      return selectedtab == 0;
    }
    
    public void RefreshLeftTreeView() {
      if (selectedtab == 0) PopulateAlbums();
      else if (selectedtab == 1) PopulateTags();
      else if (selectedtab == 2) PopulatePools();
      else if (selectedtab == 3) PopulateBlogs();
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
      Gtk.Drag.DestSet(treeview1, Gtk.DestDefaults.All, targets, Gdk.DragAction.Copy);
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
		    // TODO: Allow addition to sets.
		    if (uploadbutton.Active) return; // Doesn't allow addition to sets yet.
		    string setid = ((Album) _albums[destindex]).SetId;
		    
		    foreach (TreePath photospath in treeview2.Selection.GetSelectedRows()) {
          string photoid = GetPhoto(photospath).Id;
		      bool exists = PersistentInformation.GetInstance()
		                                         .HasAlbumPhoto(photoid, setid);
		      if (exists) {
		        TreePath albumselectedpath = treeview1.Selection.GetSelectedRows()[0];
            if (!streambutton.Active && !conflictbutton.Active
		            && treeview2.Selection.GetSelectedRows().Length == 1
		            // If dragged dest album is same as the one selected.
		            && albumselectedpath.Indices[0] == destindex) {
		            
	            // Scenario: The user is viewing the set, and decides to
	            // change the primary photo. He can do so by dragging the photo
	            // from the respective set, to the set itself. However, make
	            // sure that only one photo is selected.
              // The album selected is the same as the album the photo is
              // dragged on.
	            PersistentInformation.GetInstance().SetPrimaryPhotoForAlbum(setid, photoid);
	            PersistentInformation.GetInstance().SetAlbumDirtyIfNotNew(setid);
	            PopulateAlbums();
		        }
		      } else { // The photo isn't present in set.
		        PersistentInformation.GetInstance().AddPhotoToAlbum(photoid, setid);
		        if (PersistentInformation.GetInstance().GetPhotoIdsForAlbum(setid).Count == 1) {
		          PersistentInformation.GetInstance().SetPrimaryPhotoForAlbum(setid, photoid);
		        }
		        PersistentInformation.GetInstance().SetAlbumDirtyIfNotNew(setid);
		        UpdateAlbumAtPath(path, (Album) _albums[destindex]);
		      }
		    }
		  }
		  else if (selectedtab == 1) { // tags
		    ArrayList selectedphotos = new ArrayList();
		    foreach (TreePath photospath in treeview2.Selection.GetSelectedRows()) {
		      Photo photo = GetPhoto(photospath);
		      string tag = (string) _tags[destindex];
		      if (uploadbutton.Active) {
		        photo.AddTag(tag);
		        PersistentInformation.GetInstance().UpdateInfoForUploadPhoto(photo);
		      }
		      else {
  		      // Check if the original version is stored in db. Allow for revert.
  		      if (!PersistentInformation.GetInstance().HasOriginalPhoto(photo.Id)
  		          && !PersistentInformation.GetInstance().IsPhotoDirty(photo.Id)) {
  		        PersistentInformation.GetInstance().InsertOriginalPhoto(photo);
  		      }
  		      if (!PersistentInformation.GetInstance().HasTag(photo.Id, tag)) {
  		        PersistentInformation.GetInstance().InsertTag(photo.Id, tag);
  		        PersistentInformation.GetInstance().SetPhotoDirty(photo.Id, true);
  		      }
		      }

		      photo.AddTag(tag);
		      TreePath childpath = filter.ConvertPathToChildPath(photospath);
		      SelectedPhoto selphoto = new SelectedPhoto(photo, childpath.ToString());
		      selectedphotos.Add(selphoto);
		    }
		    // UpdatePhotos will replace the old photos, with the new ones containing
		    // the tag information.
		    UpdatePhotos(selectedphotos);
		    UpdateTagAtPath(path, (string) _tags[destindex]);
		  }
		  else if (selectedtab == 2) { // pools
		    // TODO: Allow addition to pools.
		    if (uploadbutton.Active) return; // Doesn't allow addition to sets yet.
		    PersistentInformation.Entry entry = (PersistentInformation.Entry) _pools[destindex];
		    string groupid = entry.entry1;
		    foreach (TreePath photospath in treeview2.Selection.GetSelectedRows()) {
          Photo photo = GetPhoto(photospath);
		      if (!PersistentInformation.GetInstance().HasPoolPhoto(photo.Id, groupid)) {
		        PersistentInformation.GetInstance().InsertPhotoToPool(photo.Id, groupid);
		        PersistentInformation.GetInstance().MarkPhotoAddedToPool(photo.Id, groupid, true);
		      }
		      else if (PersistentInformation.GetInstance()
		                               .IsPhotoDeletedFromPool(photo.Id, groupid)) {
		        PersistentInformation.GetInstance()
		                             .MarkPhotoDeletedFromPool(photo.Id, groupid, false);
		      }
		    }
		    UpdatePoolAtPath(path, entry);
		  }
		  else if (selectedtab == 3) { // blogs
		    if (uploadbutton.Active) return;
		    PersistentInformation.Entry entry = (PersistentInformation.Entry) _blogs[destindex];
		    string blogid = entry.entry1;
		    ArrayList selectedphotos = new ArrayList();
		    foreach (TreePath photospath in treeview2.Selection.GetSelectedRows()) {
          Photo photo = GetPhoto(photospath);
		      if (!PersistentInformation.GetInstance().HasBlogPhoto(blogid, photo.Id)) {
		        BlogEntry blogentry = 
		            new BlogEntry(blogid, photo.Id, photo.Title, photo.Description);
		        PersistentInformation.GetInstance().InsertEntryToBlog(blogentry);
		      }
		      TreePath childpath = filter.ConvertPathToChildPath(photospath);
		      SelectedPhoto selphoto = new SelectedPhoto(photo, childpath.ToString());
		      selectedphotos.Add(selphoto);
		    }
		    UpdatePhotos(selectedphotos);
		    UpdateBlogAtPath(path, entry);
		  }
		}
		
		private void UpdateAlbumAtPath(TreePath path, Album a) {
		  ListStore albumStore = (ListStore) treeview1.Model;
		  TreeIter iter;
		  albumStore.GetIter(out iter, path);
		  albumStore.SetValue(iter, 1, GetInfoAlbum(a));
		}
		
		private void UpdateTagAtPath(TreePath path, string tag) {
		  ListStore tagStore = (ListStore) treeview1.Model;
		  TreeIter iter;
		  tagStore.GetIter(out iter, path);
		  tagStore.SetValue(iter, 1, GetInfoTag(tag));
		}
		
		private void UpdatePoolAtPath(TreePath path, PersistentInformation.Entry entry) {
		  ListStore poolStore = (ListStore) treeview1.Model;
		  TreeIter iter;
		  poolStore.GetIter(out iter, path);
		  ArrayList photoids = PersistentInformation.GetInstance()
                                         .GetPhotoidsForPool(entry.entry1);
      poolStore.SetValue(iter, 1, GetInfoPool(entry, photoids.Count));
      if (photoids.Count > 0) {
        Gdk.Pixbuf thumbnail = _nophotothumbnail;
        Photo p = PersistentInformation.GetInstance().GetPhoto((string) photoids[0]);
        if (p != null) thumbnail = p.Thumbnail;
        poolStore.SetValue(iter, 0, thumbnail);
      }
		}
		
		private void UpdateBlogAtPath(TreePath path, PersistentInformation.Entry entry) {
		  ListStore blogStore = (ListStore) treeview1.Model;
		  TreeIter iter;
		  blogStore.GetIter(out iter, path);
		  blogStore.SetValue(iter, 1, GetInfoBlog(entry));
		}
		
		private void UpdateLeftTextView(string title, string desc) {
      TextBuffer buf = textview2.Buffer;
      buf.Clear();
     
      // Set the buffer here.
      buf.Text = "\n" + title + "\n\n" + desc;
      TextIter start;
      TextIter end;
      start = buf.GetIterAtLine(1);
      end = buf.GetIterAtLine(2);
      buf.ApplyTag("headline", start, end);
      buf.ApplyTag("paragraph", end, buf.EndIter);
      textview2.Buffer = buf;
      textview2.ShowAll();
		}
		
		// This method is a general purpose method, meant to take of changes
		// done to albums, or tags, shown in the left pane.
		private void OnSelectionLeftTree(object o, EventArgs args) {
      TreePath[] treepaths = ((TreeSelection)o).GetSelectedRows();
      if (treepaths.Length > 0) {
        leftcurselectedindex = (treepaths[0]).Indices[0];
      } else return;
      bool ismodebuttonactivated = streambutton.Active || conflictbutton.Active
                                   || lockbutton.Active || uploadbutton.Active
                                   || downloadbutton.Active;
      ArrayList photos = null;
      if (selectedtab == 0) { // albums
        // It is obvious that there would be at least one album, because
        // otherwise left treeview model wouldn't be formed, and the user
        // would have nothing to click upon.
        Album album = (Album) _albums[leftcurselectedindex];
        UpdateLeftTextView(album.Title, album.Desc);
        // Set photos here
        if (!ismodebuttonactivated) {
          photos = PersistentInformation.GetInstance().GetPhotosForAlbum(album.SetId);
        }
      }
      else if (selectedtab == 1) { // tags
        string tag = (string) _tags[leftcurselectedindex];
        UpdateLeftTextView(tag, "");
        UpdateTagAtPath(treepaths[0], tag);
        if (!ismodebuttonactivated) {
          photos = PersistentInformation.GetInstance().GetPhotosForTag(tag);
        }
      } else if (selectedtab == 2) { // pools
        PersistentInformation.Entry poolentry = (PersistentInformation.Entry) _pools[leftcurselectedindex];
        string groupid = poolentry.entry1;
        string grouptitle = poolentry.entry2;
        UpdateLeftTextView(grouptitle, "");
        if (!ismodebuttonactivated) {
          photos = PersistentInformation.GetInstance().GetPhotosForPool(groupid);
        }
      } else if (selectedtab == 3) { // blogs
        PersistentInformation.Entry bentry = (PersistentInformation.Entry) _blogs[leftcurselectedindex];
        string blogid = bentry.entry1;
        string blogtitle = bentry.entry2;
        UpdateLeftTextView(blogtitle, "");
        photos = new ArrayList();
        if (!ismodebuttonactivated) {
          foreach (BlogEntry blogentry in 
              PersistentInformation.GetInstance().GetEntriesForBlog(blogid)) {
            Photo photo = PersistentInformation.GetInstance().GetPhoto(blogentry.Photoid);
            photos.Add(photo);
          }
        }
      }
      
      if (!ismodebuttonactivated) {
        PopulatePhotosTreeView(photos);
      }
		}
		
		public void OnDoubleClickLeftView(object o, RowActivatedArgs args) {
		  if (selectedtab != 0) return; // if not albums, then don't care.
		  int index = args.Path.Indices[0];
		  Album album = new Album((Album) _albums[index]);
		  AlbumEditorUI.FireUp(album);
		}
		
		private string GetCol1Data(Photo p) {
      System.Text.StringBuilder pangoTitle = new System.Text.StringBuilder();
      pangoTitle.AppendFormat(
          "<span font_desc='Times Bold 10'>{0}</span>", 
          Utils.EscapeForPango(p.Title));
      if (!uploadbutton.Active // Not an upload photo entry.
          && PersistentInformation.GetInstance().IsPhotoDirty(p.Id)) {
        pangoTitle.Append(
            "<span font_desc='Times Bold 8' color='red'> [Edited]</span>");
      }
      if (!uploadbutton.Active
          && PersistentInformation.GetInstance().IsPhotoBlogged(p.Id)) {
        pangoTitle.Append(
            "<span font_desc='Times Bold 8' color='#026600'> [Blog Post]</span>");
      }
      if (!uploadbutton.Active
          && PersistentInformation.GetInstance().HasComment(p.Id)) {
          // #937504 #A17F01
        pangoTitle.Append(
            "<span font_desc='Times Bold 8' color='#937504'> [Comments]</span>");
      }
      pangoTitle.AppendLine();
      if (!uploadbutton.Active
          && PersistentInformation.GetInstance().IsDownloadEntryExists(p.Id)) {
        string foldername = PersistentInformation.GetInstance().GetFolderNameForPhotoId(p.Id);
        foldername = Utils.EscapeForPango(foldername);
        pangoTitle.Append(
            "<span font_desc='Times Bold 8' color='blue'>Download to: " + foldername + "</span>");
        pangoTitle.AppendLine();
      }
      pangoTitle.AppendFormat(
          "<span font_desc='Times Italic 10'>{0}</span>", 
          Utils.EscapeForPango(p.Description));
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

    private static string POPULATING_MSG = "Populating...";
    private static string STOPPING_SYNC_MSG = "Stopping Sync...";

    private System.Collections.Generic.IList<string> messagearray = null; 
    private void SetLabelPopup(bool isactivated, string message) {
      if (messagearray == null) messagearray = 
          new System.Collections.Generic.List<string>();
      
      if (isactivated) {
        if (!messagearray.Contains(message)) {
          messagearray.Add(message);
        }
      } else {
        messagearray.Remove(message);
      }
      
      if (messagearray.Count > 0) {
        string fullmessage = "";
        foreach (string s in messagearray) {
          fullmessage += s + "  ";
        }
        popuplabel.Markup = "<span font_desc='Times Bold 12'>" + fullmessage
            + "</span>";
        if (popuplabel.Parent == null) eventbox6.Add(popuplabel);
        eventbox6.HeightRequest = 25;
        eventbox6.ShowAll();
      } else {
        if (popuplabel.Parent != null) eventbox6.Remove(popuplabel);
        eventbox6.HeightRequest = 0;
      }
    }

		private void SetActivatePhotosTreeView(bool isactivated) {
	    if (!isactivated) {
	      SetLabelPopup(true, POPULATING_MSG);
	      treeview2.Sensitive = false;
	    } else {
	      SetLabelPopup(false, POPULATING_MSG);
	      treeview2.Sensitive = true;
	    }
		}
		
		// To be run in a thread by PopulatePhotosTreeView method.
		private void PopulateStore() {
		  Gtk.Application.Invoke(delegate{
		    SetActivatePhotosTreeView(false);
		  });
		  // Note that whatever change we do to photoStore, i.e. addition or
		  // editing of entries, filter is triggered. So, we'll create a store
		  // first, and then just assign this new store to the global photoStore.
		  ListStore store = new Gtk.ListStore(
		                                 typeof(Gdk.Pixbuf), typeof(string),
		                                 typeof(string), typeof(string), 
		                                 typeof(string));

		  foreach (Photo p in _photos) {
		    store.AppendValues(p.Thumbnail, GetCol1Data(p), GetCol2Data(p),
		                            GetCol3Data(p), GetCol4Data(p));
		  }
		  
		  Gtk.Application.Invoke(delegate{
		    photoStore = store;
		    filter = new TreeModelFilter(photoStore, null);
		    filter.VisibleFunc = new TreeModelFilterVisibleFunc(FilterPhotos);
		    treeview2.Model = filter;
		    SetActivatePhotosTreeView(true);
		    if (uploadbutton.Active) {
		      SetUploadWindow(true);
		    }
		    uploadfilechooserbutton.Sensitive = true;
		    treeview2.ShowAll();
		  });
		}
		
		private void UpdatePhotoCountLabel() {
		  int count = 0;
		  foreach (Photo p in _photos) {
		    if (FilterPhoto(p)) count++;
		  }
		  label19.Markup = "<span foreground='white' font_desc='Times' weight='bold'> "
		      + count + " photos </span>";
		}
		
		public void PopulatePhotosTreeView(ArrayList photos) {
		  if (photos == null) return;
		  if (_populatephotosthread != null) _populatephotosthread.Abort();
		  _photos.Clear();
		  _photos.AddRange(photos);
      UpdatePhotoCountLabel();
      ThreadStart job = new ThreadStart(PopulateStore);
      _populatephotosthread = new Thread(job);
      _populatephotosthread.Start();
    }
    
    private bool FilterPhotos(TreeModel model, TreeIter iter) {
      int index = model.GetPath(iter).Indices[0];
      Photo p = (Photo) _photos[index];
      return FilterPhoto(p);
    }
    
    private bool FilterPhoto(Photo p) {
      // Photo has to be dirty if 'Only Edited Photos' is ticked.
      if (checkbutton2.Active 
          && !uploadbutton.Active
          && !PersistentInformation.GetInstance().IsPhotoDirty(p.Id))
          return false;
      // Now check for 'Commented Photos Only'
      if (checkbutton4.Active
          && !uploadbutton.Active
          && !PersistentInformation.GetInstance().HasComment(p.Id))
          return false;
      
      string query = entry5.Text;
      if (query == "") return true;
      bool flag = false;
      System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase;
      if (p.Title.IndexOf(query, comp) > -1) flag = true;
      else if (p.Description.IndexOf(query, comp) > -1) flag = true;
      else if (p.TagString.IndexOf(query, comp) > -1) flag = true;
      else if (!checkbutton3.Active 
               && p.PrivacyInfo.IndexOf(query, comp) > -1) flag = true;
      else if (!checkbutton3.Active 
               && p.LicenseInfo.IndexOf(query, comp) > -1) flag = true;
      else if (checkbutton4.Active
               && PersistentInformation.GetInstance()
                                       .HasCommentText(p.Id, query)) flag = true;
      return flag;
    }
    
    // If the user has used search tab, the path
    // provided here wouldn't exactly show the absolute position in the
    // model, because entries would have been filtered out.
    private Photo GetPhoto(TreePath path) {
      TreePath childpath = filter.ConvertPathToChildPath(path);
      return (Photo) _photos[childpath.Indices[0]];
    }
    
    private void OnFilterEntryChanged(object o, EventArgs args) {
      if (_searchwaitthread != null && !_busysearching) _searchwaitthread.Abort();
      _searchwaitthread = new Thread(new ThreadStart(DoWait));
      _searchwaitthread.Start();
    }
    
    private void DoWait() {
      Thread.Sleep(500);
      Thread searchthread = new Thread(new ThreadStart(DoFilterEntries));
      searchthread.Start();
    }
    
    private void DoFilterEntries() {
      if (filter == null) return;
      Gtk.Application.Invoke(delegate{
        filter.Refilter();
        UpdatePhotoCountLabel();
      });
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
        foreach (Photo src in _conflictedphotos) {
          Photo p = PersistentInformation.GetInstance().GetPhoto(src.Id);
          if (p == null) continue;
          photos.Add(p);
        }
      }
      else if (selectedtab == 0) {
        string setid = ((Album) _albums[leftcurselectedindex]).SetId;
        photos = PersistentInformation.GetInstance().GetPhotosForAlbum(setid);
      }
      else if (selectedtab == 1) {
        string tag = (string) _tags[leftcurselectedindex];
        photos = PersistentInformation.GetInstance().GetPhotosForTag(tag);
      }
      PopulatePhotosTreeView(photos);
    }

    public void UpdatePhotos(ArrayList selectedphotos) {
      foreach (SelectedPhoto sel in selectedphotos) {
        // SelectedPhoto stores childpath.
        TreePath childpath = new TreePath(sel.path);
        _photos.RemoveAt(childpath.Indices[0]);
        _photos.Insert(childpath.Indices[0], sel.photo);
        TreeIter iter;
        photoStore.GetIter(out iter, childpath);
        photoStore.SetValue(iter, 1, GetCol1Data(sel.photo));
        photoStore.SetValue(iter, 2, GetCol2Data(sel.photo));
        photoStore.SetValue(iter, 3, GetCol3Data(sel.photo));
        photoStore.SetValue(iter, 4, GetCol4Data(sel.photo));
      }
    }
    
		public void SetRightTreeView() {
		  Gtk.CellRendererText titleRenderer = new Gtk.CellRendererText();
		  titleRenderer.WrapWidth = 510;
		  titleRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  Gtk.CellRendererText tagRenderer = new Gtk.CellRendererText();
		  tagRenderer.WrapWidth = 150;
		  tagRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  Gtk.CellRendererText viewableRenderer = new Gtk.CellRendererText();
		  viewableRenderer.WrapWidth = 90;
		  viewableRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  Gtk.CellRendererText licenseRenderer = new Gtk.CellRendererText();
		  licenseRenderer.WrapWidth = 140;
		  licenseRenderer.WrapMode = Pango.WrapMode.Word;
		  
		  treeview2.AppendColumn("Thumbnail", new Gtk.CellRendererPixbuf(), "pixbuf", 0);
		  treeview2.AppendColumn("Title/Description", titleRenderer, "markup", 1);
		  treeview2.AppendColumn("Tags", tagRenderer, "markup", 2);
		  treeview2.AppendColumn("Viewable", viewableRenderer, "markup", 3);
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
		
		public void SetUploadWindow(bool isactivated) {
		  if (isactivated) {
		    if (!uploadbutton.Active) return; // No point having the upload window
		    // if its not in upload mode.
		    
  		  // Allow dropping photos for easy drag-n-drop uploading.
  		  //treeview2.EnableModelDragDest(targets, Gdk.DragAction.Copy);
  		  Gtk.Drag.DestSet(eventbox6, Gtk.DestDefaults.All, targets, Gdk.DragAction.Copy);
        eventbox6.DragDataReceived += OnPhotoDraggedForUploading;
        eventbox6.HeightRequest = 100;
        popuplabel.Text = "Drop individual or multiple photos here to add them to upload list."
          + " You can also drag and drop the entire folder to add all the photos"
          + " present inside the folder.";
        popuplabel.Wrap = true;
        int height;
        int width;
        eventbox6.GetSizeRequest(out width, out height); 
        popuplabel.SetSizeRequest(width, height);
        if (popuplabel.Parent == null) eventbox6.Add(popuplabel);
        eventbox6.ShowAll();
      } else {
        eventbox6.HeightRequest = 0;
        if (popuplabel.Parent != null) eventbox6.Remove(popuplabel);
        Gtk.Drag.DestUnset(eventbox6);
      }
    }
    
		private void OnPhotoDraggedForUploading(object o, DragDataReceivedArgs args) {
		  Gtk.Application.Invoke(delegate{
		    uploadfilechooserbutton.Sensitive = false;
		    SetUploadWindow(false);
		  });
		  
		  System.IO.StringReader droppedreader = new System.IO.StringReader(
		      System.Text.Encoding.UTF8.GetString(args.SelectionData.Data));
		  string droppedline = droppedreader.ReadLine();
		  while (droppedline != null) {
		    string filename;
		    if (droppedline.IndexOf("file:///") > -1) {
		      filename = droppedline.Substring(7);
		      filename = System.Uri.UnescapeDataString(filename);
		    } else {
		      Console.WriteLine("Unexpected file path: " + droppedline);
		      droppedline = droppedreader.ReadLine();
		      continue;
		    }
		    if (System.IO.Directory.Exists(filename)) {
		      foreach (string fileindir in System.IO.Directory.GetFiles(filename)) {
		        PersistentInformation.GetInstance().InsertEntryToUpload(fileindir);
		      }
		    } else {
		      PersistentInformation.GetInstance().InsertEntryToUpload(filename);
		    }
		    droppedline = droppedreader.ReadLine();
		  }
		  // Refresh the photo view. It will take care of making the upload
		  // file chooser button sensitive.
		  RefreshUploadPhotos();
		}
		
		private void OnDoubleClickPhoto(object o, RowActivatedArgs args) {
		  ArrayList selectedphotos = new ArrayList();
		  // Make sure that we don't point to the photo object stored here.
		  // Otherwise, even if the editing is not saved, it would be stored
		  // in _photos. So, create a new Photo object.
      Photo p = new Photo(GetPhoto(args.Path));
      if (!uploadbutton.Active
          && !PersistentInformation.GetInstance().HasOriginalPhoto(p.Id)
          && !PersistentInformation.GetInstance().IsPhotoDirty(p.Id)) {
		    PersistentInformation.GetInstance().InsertOriginalPhoto(p);
		  }
      TreePath childpath = filter.ConvertPathToChildPath(args.Path);
      if (GetMode() == DeskFlickrUI.ModeSelected.BlogMode) {
        string blogid = ((PersistentInformation.Entry) _blogs[leftcurselectedindex]).entry1;
        BlogEntry blogentry = PersistentInformation.GetInstance().GetEntryForBlog(blogid, p.Id);
        BlogSelectedPhoto bsel = new BlogSelectedPhoto(p, blogentry, childpath.ToString());
        selectedphotos.Add(bsel);
      } else {
        SelectedPhoto sel = new SelectedPhoto(p, childpath.ToString());
        selectedphotos.Add(sel);
      }
		  PhotoEditorUI.FireUp(selectedphotos, GetMode());
		}
				
		private void OnEditButtonClicked(object o, EventArgs args) {
		  
		  // Check right tree first.
		  if (treeview2.Selection.GetSelectedRows().Length > 0) {
		    ArrayList selectedphotos = new ArrayList();
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          Photo p = new Photo(GetPhoto(path));
          // Check if the original version is stored in db. Allow for revert.
		      if (!uploadbutton.Active
		          && !PersistentInformation.GetInstance().HasOriginalPhoto(p.Id)
		          && !PersistentInformation.GetInstance().IsPhotoDirty(p.Id)) {
		        PersistentInformation.GetInstance().InsertOriginalPhoto(p);
		      }
          TreePath childpath = filter.ConvertPathToChildPath(path);
          if (GetMode() == DeskFlickrUI.ModeSelected.BlogMode) {
            string blogid = ((PersistentInformation.Entry) _blogs[leftcurselectedindex]).entry1;
            BlogEntry blogentry = PersistentInformation.GetInstance().GetEntryForBlog(blogid, p.Id);
            BlogSelectedPhoto bsel = new BlogSelectedPhoto(p, blogentry, childpath.ToString());
            selectedphotos.Add(bsel);
          } else {
            SelectedPhoto sel = new SelectedPhoto(p, childpath.ToString());
            selectedphotos.Add(sel);
          }
        }
        PhotoEditorUI.FireUp(selectedphotos, GetMode());
  		} // check left tree now.
  		else if (treeview1.Selection.GetSelectedRows().Length > 0) {
  		  if (selectedtab == 1) return; // Tags can't be edited.
  		  TreePath path = treeview1.Selection.GetSelectedRows()[0];
  		  Album album = new Album((Album) _albums[path.Indices[0]]);
  		  AlbumEditorUI.FireUp(album);
  		}
		}
	  
	  private void OnDownloadPhotosMenuItemClicked(object o, EventArgs args) {
	    FireUpFileChooserForDownloading();
	  }
	  
		private void FireUpFileChooserForDownloading() {
		  if (uploadbutton.Active) return;
      FileChooserDialog chooser = new FileChooserDialog(
          "Select a folder", null, FileChooserAction.SelectFolder,
          Stock.Open, ResponseType.Ok, Stock.Cancel, ResponseType.Cancel);
      
      chooser.SetIconFromFile(DeskFlickrUI.ICON_PATH);
      chooser.SetFilename(PersistentInformation.GetInstance().DownloadFoldername);
      ResponseType choice = (ResponseType) chooser.Run();
      string foldername = "";
		  if (choice == ResponseType.Ok) {
       foldername = chooser.Filename;
       // Set the default path to be opened next time file chooser runs. 
       PersistentInformation.GetInstance().DownloadFoldername = foldername;
      }
      chooser.Destroy();
      if (foldername.Equals("")) return;
      
      // Selected folder for downloading.
      ArrayList photoids = new ArrayList();
      ArrayList selectedphotos = new ArrayList();
      bool refreshview = false;
		  if (treeview2.Selection.GetSelectedRows().Length > 0) {
		    foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
		      Photo p = new Photo(GetPhoto(path));
		      TreePath childpath = filter.ConvertPathToChildPath(path);
		      SelectedPhoto sel = new SelectedPhoto(p, childpath.ToString());
		      selectedphotos.Add(sel);
		      photoids.Add(p.Id);
		    }
		  }
	    else if (treeview1.Selection.GetSelectedRows().Length > 0) {
	      // Going through the complex way of figuring out photos for the
	      // different tab modes. Easier way would be to just copy contents
	      // of _photos, as it already takes care of left tree selection. However,
	      // when the view is locked to 'downloads', _photos wouldn't be updated.
	      // Hence the need to figure out photoids.
		    TreePath path = treeview1.Selection.GetSelectedRows()[0];
		    if (selectedtab == 0) { // albums
    		  Album album = new Album((Album) _albums[path.Indices[0]]);
    		  photoids = PersistentInformation.GetInstance().GetPhotoIdsForAlbum(album.SetId);
    		  //foldername = String.Format("{0}/{1}", foldername, album.Title);
  		  } else if (selectedtab == 1) { // tags
  		    string tag = (string) _tags[path.Indices[0]];
  		    photoids = PersistentInformation.GetInstance().GetPhotoIdsForTag(tag);
  		    //foldername = String.Format("{0}/{1}", foldername, tag);
  		  } else if (selectedtab == 2) {
  		    PersistentInformation.Entry poolentry = 
  		        (PersistentInformation.Entry) _pools[path.Indices[0]];
  		    string poolid = poolentry.entry1;
  		    //string pooltitle = poolentry.entry2;
  		    photoids = PersistentInformation.GetInstance().GetPhotoidsForPool(poolid);
  		    //foldername = String.Format("{0}/{1}", foldername, pooltitle);
  		  } else if (selectedtab == 3) {
  		    PersistentInformation.Entry blog =
  		        (PersistentInformation.Entry) _blogs[path.Indices[0]];
  		    foreach (BlogEntry blogentry in 
  		        PersistentInformation.GetInstance().GetEntriesForBlog(blog.entry1)) {
  		      photoids.Add(blogentry.Photoid);
  		    }
  		    //foldername = String.Format("{0}/{1}", foldername, blog.entry2);
  		  }
  		  refreshview = true;
		  }
		  Utils.EnsureDirectoryExists(foldername);
		  foreach (string id in photoids) {
		    if (PersistentInformation.GetInstance().IsDownloadEntryExists(id)) {
		      PersistentInformation.GetInstance().DeleteEntryFromDownload(id);
		    }
		    PersistentInformation.GetInstance().InsertEntryToDownload(id, foldername);
		  }
		  UpdateToolBarButtons();
      if (downloadbutton.Active) RefreshDownloadPhotos();
      else if (refreshview) RefreshLeftTreeView();
      else UpdatePhotos(selectedphotos);
		}
		
		private bool IsNormalModeSelected() {
		  return !lockbutton.Active && !streambutton.Active 
		         && !conflictbutton.Active && !uploadbutton.Active
		         && !downloadbutton.Active;
		}
		
		// These methods are only called when the state of the buttons is toggled.
		private void OnLockButtonClicked(object o, EventArgs args) {
		  if (lockbutton.Active) {
		    // The problem here is this, the user selects say photostream, and then
		    // directly clicks on lock button. Result is that the photostream
		    // is locked; similary for upload/download photos etc.
		    if (streambutton.Active || conflictbutton.Active 
		        || uploadbutton.Active || downloadbutton.Active) {
		      streambutton.Active = false;
		      conflictbutton.Active = false;
		      uploadbutton.Active = false;
		      downloadbutton.Active = false;
		      // Now toggle lock button, so that RefreshLeftTreeView() is called
		      // in normal mode, where no button is active.
		      lockbutton.Active = false;
		    }
		    // If none of the other buttons is active, then this following command
		    // would just act like a NOP. Otherwise, it would do the necessary
		    // toggling of lock button back to active state.
		    lockbutton.Active = true;
		  } else if (IsNormalModeSelected()) {
		    RefreshLeftTreeView();
		  }
		  if (lockbutton.Active) UpdateFlameWindowLabel();
		}
		
		private void OnStreamButtonClicked(object o, EventArgs args) {
		  if (streambutton.Active) {
		    lockbutton.Active = false;
		    conflictbutton.Active = false;
		    uploadbutton.Active = false;
		    downloadbutton.Active = false;
		    ArrayList photos = PersistentInformation.GetInstance().GetAllPhotos();
		    SetPhotoCountTip(streambutton, photos.Count);
		    PopulatePhotosTreeView(photos);
		  } else if (IsNormalModeSelected()) {
		    RefreshLeftTreeView();
		  }
		  if (streambutton.Active) UpdateFlameWindowLabel();
		}
		
		public void OnConflictButtonClicked(object o, EventArgs args) {
		  if (conflictbutton.Active) {
		    lockbutton.Active = false;
		    streambutton.Active = false;
		    uploadbutton.Active = false;
		    downloadbutton.Active = false;
	      ArrayList photos = new ArrayList();
	      // Get the photoid from these source (server) photos, and 
	      // populate the tree with the ones that we have.
	      foreach (Photo src in _conflictedphotos) {
	        Photo p = PersistentInformation.GetInstance().GetPhoto(src.Id);
	        if (p == null) continue;
	        photos.Add(p);
	      }
	      PopulatePhotosTreeView(photos);
		  } else if (IsNormalModeSelected()) {
		    RefreshLeftTreeView();
		  }
		  if (conflictbutton.Active) UpdateFlameWindowLabel();
		}
		
		public void RefreshUploadPhotos() {
		  if (!uploadbutton.Active) return;
		  ArrayList photos = PersistentInformation.GetInstance().GetPhotosToUpload();
		  SetPhotoCountTip(uploadbutton, photos.Count);
		  PopulatePhotosTreeView(photos);
		}
		
		private void OnUploadButtonClicked(object o, EventArgs args) {
		  if (uploadbutton.Active) {
		    lockbutton.Active = false;
		    streambutton.Active = false;
		    conflictbutton.Active = false;
		    downloadbutton.Active = false;
		    RefreshUploadPhotos();
        // Populate Photos treeview method will take care of setting
        // the upload window. We can't do it here, because populate method
        // runs a thread, and returns control before the entire population
        // is complete. So, even if the window is set, it gets overridden
        // when population is finished, and eventbox6 is made hidden.
		  } else {
		    SetUploadWindow(false);
		    if (IsNormalModeSelected()) RefreshLeftTreeView();
		  }
		  if (uploadbutton.Active) UpdateFlameWindowLabel();
		}
		
		private void RefreshDownloadPhotos() {
		  ArrayList photos = new ArrayList();
		  foreach (PersistentInformation.Entry entry in 
		           PersistentInformation.GetInstance().GetEntriesToDownload()) {
		    string photoid = entry.entry1;
		    Photo photo = PersistentInformation.GetInstance().GetPhoto(photoid);
		    photos.Add(photo);
		  }
		  SetPhotoCountTip(downloadbutton, photos.Count);
		  PopulatePhotosTreeView(photos);
		}
		
		private void OnDownloadButtonClicked(object o, EventArgs args) {
		  if (downloadbutton.Active) {
		    lockbutton.Active = false;
		    streambutton.Active = false;
		    conflictbutton.Active = false;
		    uploadbutton.Active = false;
		    RefreshDownloadPhotos();
		  } else if (IsNormalModeSelected()) {
		    RefreshLeftTreeView();
		  }
		  if (downloadbutton.Active) UpdateFlameWindowLabel();
		}
		
		private void OnUploadPhotosMenuItemClicked(object o, EventArgs args) {
		  FireUpFileChooserForUploading();
		}
		
		private void FireUpFileChooserForUploading() {
      UploadFileChooserUI.FireUp();
		}
		
		private void SetTopLeftToolBar() {
		  connectbutton = new ToolButton(Stock.Refresh);
		  connectbutton.Sensitive = true;
		  connectbutton.Clicked += new EventHandler(ConnectionHandler);
		  connectbutton.SetTooltip(tips, "Sync Now", "Sync Now");
		  toolbar2.Insert(connectbutton, -1);
      
		  ToolButton addsetbutton = new ToolButton(Stock.Add);
		  addsetbutton.Sensitive = true;
		  addsetbutton.Clicked += new EventHandler(OnAddNewSetEvent);
		  addsetbutton.SetTooltip(tips, "Add New Set", "Add New Set");
		  toolbar2.Insert(addsetbutton, -1);
		  
		  ToolButton editbutton = new ToolButton(Stock.Edit);
		  editbutton.Sensitive = true;
		  editbutton.Clicked += new EventHandler(OnEditButtonClicked);
		  editbutton.SetTooltip(tips, "Edit", "Edit");
		  toolbar2.Insert(editbutton, -1);
		  
		  uploadfilechooserbutton = new ToolButton(Stock.SortAscending);
		  uploadfilechooserbutton.Sensitive = true;
		  uploadfilechooserbutton.Clicked += new EventHandler(OnUploadPhotosMenuItemClicked);
		  uploadfilechooserbutton.SetTooltip(tips, "Upload Photos", "Upload Photos");
		  toolbar2.Insert(uploadfilechooserbutton, -1);
		  
		  ToolButton downloadfilechooserbutton = new ToolButton(Stock.SortDescending);
		  downloadfilechooserbutton.Sensitive = true;
		  downloadfilechooserbutton.Clicked += new EventHandler(OnDownloadPhotosMenuItemClicked);
		  downloadfilechooserbutton.SetTooltip(tips, "Download Photos", "Download Photos");
		  toolbar2.Insert(downloadfilechooserbutton, -1);
		  
		  ToolButton quitbutton = new ToolButton(Stock.Quit);
		  quitbutton.Sensitive = true;
		  quitbutton.Clicked += new EventHandler(OnQuitEvent);
		  quitbutton.SetTooltip(tips, "Quit", "Quit");
		  toolbar2.Insert(quitbutton, -1);
		}
		
		private void SetHorizontalToolBar() {
		  ToolButton editbutton = new ToolButton(Stock.Edit);
		  editbutton.IsImportant = true;
		  editbutton.Sensitive = true;
		  editbutton.Clicked += new EventHandler(OnEditButtonClicked);
		  toolbar1.Insert(editbutton, -1);
		  
		  lockbutton = new ToggleToolButton(Stock.DndMultiple);
		  lockbutton.IsImportant = true;
		  lockbutton.Sensitive = true;
		  lockbutton.Label = "Lock View";
		  lockbutton.Clicked += new EventHandler(OnLockButtonClicked);
		  toolbar1.Insert(lockbutton, -1);
		  
		  streambutton = new ToggleToolButton(Stock.SelectAll);
		  streambutton.IsImportant = true;
		  streambutton.Sensitive = true;
		  streambutton.Label = "Photostream";
		  streambutton.Clicked += new EventHandler(OnStreamButtonClicked);
		  toolbar1.Insert(streambutton, -1);
		  
		  uploadbutton = new ToggleToolButton(Stock.SortAscending);
		  uploadbutton.IsImportant = true;
		  uploadbutton.Sensitive = true;
		  uploadbutton.Label = "Uploads";
		  uploadbutton.Clicked += new EventHandler(OnUploadButtonClicked);
		  toolbar1.Insert(uploadbutton, -1);
		  
		  downloadbutton = new ToggleToolButton(Stock.SortDescending);
		  downloadbutton.IsImportant = true;
		  downloadbutton.Sensitive = true;
		  downloadbutton.Label = "Downloads";
		  downloadbutton.Clicked += new EventHandler(OnDownloadButtonClicked);
		  toolbar1.Insert(downloadbutton, -1);
		  
		  conflictbutton = new ToggleToolButton(Stock.DialogWarning);
		  conflictbutton.IsImportant = true;
		  conflictbutton.Sensitive = true;
		  conflictbutton.Label = "Conflicts";
		  conflictbutton.Clicked += new EventHandler(OnConflictButtonClicked);
		  toolbar1.Insert(conflictbutton, -1);
		  
		  syncbutton = new ToolButton(Stock.Refresh);
		  syncbutton.IsImportant = true;
		  syncbutton.Sensitive = true;
		  syncbutton.Label = "Sync Now";
		  syncbutton.Clicked += new EventHandler(ConnectionHandler);
		  toolbar1.Insert(syncbutton, -1);
		  
		  // Update the stream and conflict buttons with their respective number
		  // of photos.
		  UpdateToolBarButtons();
		}

    private void SetPhotoCountTip(Gtk.ToolItem item, int numphotos) {
      item.SetTooltip(tips, numphotos + " pics", numphotos + " pics");
    }
    
  	public void UpdateToolBarButtons() {
  	  int countphotos = PersistentInformation.GetInstance().GetCountPhotos();
  	  SetPhotoCountTip(streambutton, countphotos);
  	  
  	  SetPhotoCountTip(conflictbutton, _conflictedphotos.Count);
  	  conflictbutton.Sensitive = (_conflictedphotos.Count != 0);
  	  
  	  countphotos = PersistentInformation.GetInstance().GetPhotosToUpload().Count;
  	  SetPhotoCountTip(uploadbutton, countphotos);

  	  countphotos = PersistentInformation.GetInstance().GetEntriesToDownload().Count;
  	  SetPhotoCountTip(downloadbutton, countphotos);
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
      
      Label poolLabel = new Label();
      poolLabel.Markup = "<span foreground='white'>Pools</span>";
      poolLabel.Angle = 90;
      eventbox4.Add(poolLabel);
      eventbox4.ButtonPressEvent += PoolTabSelected;
      
      Label blogLabel = new Label();
      blogLabel.Markup = "<span foreground='white'>Blogs</span>";
      blogLabel.Angle = 90;
      eventbox10.Add(blogLabel);
      eventbox10.ButtonPressEvent += BlogTabSelected;
      
      // Set the default tab.
      AlbumTabSelected(null, null);
		}
		
		private void UpdateFlameWindowLabel() {
		  if (lockbutton.Active) {
		    label11.Markup =
		        "<span style='italic'>View has been locked. Flames window is of no"
		        + " good use in this mode.</span>";
		  } else if (streambutton.Active) {
		    label11.Markup = 
		        "<span style='italic'>Drop photos here to delete them permanently.</span>";
		  } else if (conflictbutton.Active) {
		    label11.Markup =
		        "<span style='italic'>Conflict Resolution Mode. Flames window is of"
		        + " no good use in this mode.</span>";
		  } else if (uploadbutton.Active) {
		    label11.Markup =
		        "<span style='italic'>Drop photos here to remove them from upload list.</span>";
		  } else if (downloadbutton.Active) {
		    label11.Markup =
		        "<span style='italic'>Drop photos here to remove them from download list.</span>";
		  } else if (selectedtab == 0) {
		    label11.Markup = 
		        "<span style='italic'>Drop photos here to remove from set."
		        + " Drop all photos here to delete the set entirely.</span>";
		  } else if (selectedtab == 1) {
		    label11.Markup = 
		        "<span style='italic'>Drop photos here to remove tag.</span>";
		  } else if (selectedtab == 2) {
		    label11.Markup = 
		        "<span style='italic'>Drop photos here to remove from pool.</span>";
		  } else if (selectedtab == 3) {
		    label11.Markup =
		        "<span style='italic'>Drop photos here to remove blog entry.</span>";
		  } else {
		    label11.Markup =
		        "<span style='italic'>Wao! This almost never happens.</span>";
		  }
		}
		
		private void SetAllTabsColorToNormal() {
		  eventbox1.ModifyBg(StateType.Normal, tabcolor);
		  eventbox2.ModifyBg(StateType.Normal, tabcolor);
		  eventbox4.ModifyBg(StateType.Normal, tabcolor);
		  eventbox10.ModifyBg(StateType.Normal, tabcolor);
		}
		
		private void AlbumTabSelected(object o, EventArgs args) {
		  SetAllTabsColorToNormal();
		  eventbox1.ModifyBg(StateType.Normal, tabselectedcolor);
		  selectedtab = 0;
		  leftcurselectedindex = 0;
		  PopulateAlbums();
		}
		
		private void TagTabSelected(object o, EventArgs args) {
      SetAllTabsColorToNormal();
		  eventbox2.ModifyBg(StateType.Normal, tabselectedcolor);
		  selectedtab = 1;
		  leftcurselectedindex = 0;
		  textview2.Buffer.Text = "";
		  PopulateTags();
		}
		
		private void PoolTabSelected(object o, EventArgs args) {
      SetAllTabsColorToNormal();
		  eventbox4.ModifyBg(StateType.Normal, tabselectedcolor);
		  selectedtab = 2;
		  leftcurselectedindex = 0;
		  PopulatePools();
		}
		
		private void BlogTabSelected(object o, EventArgs args) {
		  SetAllTabsColorToNormal();
		  eventbox10.ModifyBg(StateType.Normal, tabselectedcolor);
		  selectedtab = 3;
		  leftcurselectedindex = 0;
		  PopulateBlogs();
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
		
		public void SetUploadStatus(long max, long val) {
		  progressbar2.Adjustment.Lower = 0;
		  progressbar2.Adjustment.Upper = max;
		  progressbar2.Adjustment.Value = val;
		  int percentage = (int) ((val*100)/max);
		  progressbar2.Text = percentage + " % used";
		}
		
		public void SetLimitsProgressBar(int max) {
		  progressbar1.Adjustment.Lower = 0;
		  progressbar1.Adjustment.Upper = max;
		  progressbar1.Adjustment.Value = 0;
		  if (max > 0) IncrementProgressBar(0);
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
      // Work online/offline menu item.
      checkmenuitem3.Activated += new EventHandler(OnWorkOfflineEvent);
      // Add new set
      menuitem3.Activated += new EventHandler(OnAddNewSetEvent);
      // Edit
      menuitem4.Activated += new EventHandler(OnEditButtonClicked);
      // Quit menu item.
      imagemenuitem5.Activated += new EventHandler(OnQuitEvent);
      // About button.
      menuitem2.Activated += new EventHandler(OnAboutButtonClicked);
      // Upload button
      menuitem5.Activated += new EventHandler(OnUploadPhotosMenuItemClicked);
      // Download button
      menuitem6.Activated += new EventHandler(OnDownloadPhotosMenuItemClicked);
    }
    
    public void ShowMessageDialog(string message) {
  	    MessageDialog md = new MessageDialog(
  	        window1, DialogFlags.DestroyWithParent, MessageType.Info,
  	        ButtonsType.Close, message);
  	    md.Run();
  	    md.Destroy();
    }
    
    private void OnAddNewSetEvent(object sender, EventArgs args) {
      // If Upload mode is active, then do nothing.
      if (uploadbutton.Active) return;
      
      if (treeview2.Selection.CountSelectedRows() == 0) {
        ShowMessageDialog("Please select a photo to be used as primary photo"
  	        + " for the new Photo Set. You can change the primary photo later as well.");
  	    return;
      }
      
      TreePath path = treeview2.Selection.GetSelectedRows()[0];
      Photo p = GetPhoto(path);
      Random rand = new Random();
      int setid = rand.Next(100);
      while (PersistentInformation.GetInstance().HasAlbum(setid.ToString())) {
        setid = rand.Next(100);
      }
      // Got a new unique setid.
      Album album = new Album(setid.ToString(), "New Album", "", p.Id);
      AlbumEditorUI.FireUp(album, true);
    }
    
		private void OnQuitEvent (object sender, EventArgs args) {
  	  ResponseType result = ResponseType.Yes;
  	  // If the connection is busy, notify the user that he's aborting
  	  // the connection.
  	  if (FlickrCommunicator.GetInstance().IsBusy) {
  	    MessageDialog md = new MessageDialog(
  	        window1, DialogFlags.DestroyWithParent, MessageType.Question,
  	        ButtonsType.YesNo, 
  	        "Connection is busy. Do you really wish to abort the connection"
  	        + " and quit the application?");
  	    result = (ResponseType) md.Run();
  	    md.Destroy();
  	  }
  	  if (result == ResponseType.Yes) {
  	    StoreWindowSize();  
  	    if (_connthread != null) _connthread.Abort();
  		  Application.Quit ();
  		}
		}
		
		private void StoreWindowSize() {
      int width;
		  int height;
		  window1.GetSize(out width, out height);
		  PersistentInformation.GetInstance().WindowWidth = width;
		  PersistentInformation.GetInstance().WindowHeight = height;
		  PersistentInformation.GetInstance().VerticalPosition = vpaned1.Position;
		  PersistentInformation.GetInstance().HorizontalPosition = hpaned1.Position;
		}
		
		// Connect the Signals defined in Glade
  	private void OnWindowDeleteEvent (object sender, DeleteEventArgs args) {
  	  OnQuitEvent(null, null);
  		args.RetVal = true;
  	}
  	
  	private void OnAboutButtonClicked(object o, EventArgs args) {
  	  AboutUI.FireUp();
  	}
  	
  	private bool IsWorkOffline {
  	  get {
  	    return checkmenuitem3.Active;
  	  }
  	}
  	
  	public void SetSensitivityConnectionButtons(bool issensitive) {
  	  imagemenuitem2.Sensitive = issensitive;
      checkmenuitem3.Sensitive = issensitive;
      if (issensitive) {
        if (syncbutton.Label == "Cancel Sync") SetLabelPopup(false, STOPPING_SYNC_MSG);
        syncbutton.StockId = Stock.Refresh;
        syncbutton.Label = "Sync Now";
      } else {
        syncbutton.StockId = Stock.Stop;
        syncbutton.Label = "Cancel Sync";
      }
      connectbutton.Sensitive = issensitive;
    }
    
  	private void OnWorkOfflineEvent(object sender, EventArgs args) {
  	  if (IsWorkOffline) { // Work OFF-line.
  	    FlickrCommunicator.GetInstance().Disconnect();
  	    if (_connthread != null) _connthread.Abort();
  	    _connthread = null;
  	    SetStatusLabel("Done. Working Offline");
  	    SetLimitsProgressBar(0);
  	    SetProgressBarText("");
  	  } else { // Work ON-line.
  	    if (syncbutton.Label == "Cancel Sync" 
  	        && FlickrCommunicator.GetInstance().IsBusy) {
  	      SetLabelPopup(true, STOPPING_SYNC_MSG);
  	      FlickrCommunicator.GetInstance().DoStopSync();
  	      return;
  	    }
  	    if (_connthread != null) _connthread.Abort();
  	    _connthread = null;
  	    FlickrCommunicator comm = FlickrCommunicator.GetInstance();
  	    // The communicator can't be busy, because the connection thread
  	    // has been aborted.
  	    Gtk.Application.Invoke( delegate {
  	      SetStatusLabel("Authenticating application...");
  	    });
  	    // If the token is not present, fire up the first time authentication
  	    // GUI. The thread later makes sure that we do get the token.
        if (!comm.IsTokenPresent()) FirstTimeAuthentication.FireUp();
  	    
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
  	    while (!comm.IsTokenPresent()) {
  	      Thread.Sleep(6*1000); // 6 seconds.
  	    }
  	    // TODO: This particular part of the code would probably never be called.
  	    while (comm.IsBusy) {
  	      Thread.Sleep(5*60*1000); // wait for the connection to be finished.
  	    }
  	    Gtk.Application.Invoke( delegate {
  	      SetStatusLabel("Attempting connection...");
  	    });
  	    comm.AttemptConnection();
  	    if (comm.IsConnected) comm.RoutineCheck();
  	    Gtk.Application.Invoke (delegate {
  	      UpdateToolBarButtons();
  	      string label = "Done.";
  	      if (!comm.IsConnected) label = "Connection failed.";
  	      SetStatusLabel(label + " Counting time for reconnection...");
  	      SetLimitsProgressBar(100);
  	    });
  	    // Wait for 30 minutes.
  	    for (int i=0; i<100; i++) {
  	      Thread.Sleep(18*1000); // 18 seconds
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
    
    private void PhotoDeletionHandler(string photoid) {
      PersistentInformation.GetInstance().SetPhotoForDeletion(photoid);
      // Remove from conflicts if present.
      RemoveServerPhoto(photoid);
      
      // Remove it from all the albums.
      foreach (Album album in PersistentInformation.GetInstance().GetAlbums()) {
        PersistentInformation.GetInstance().DeletePhotoFromAlbum(photoid, album.SetId);
        // Check if the photo is the primary photo in the  album, and replace
        // it with a randomly selected new one. Set the album dirty, so that
        // the new photo would be set as primary in the next update.
        if (album.PrimaryPhotoid.Equals(photoid)) {
          ArrayList photoidsforalbum = 
              PersistentInformation.GetInstance().GetPhotoIdsForAlbum(album.SetId);
          int newindex = (new Random()).Next(photoidsforalbum.Count);
          string newphotoid = (string) photoidsforalbum[newindex];
          PersistentInformation.GetInstance().SetPrimaryPhotoForAlbum(album.SetId, newphotoid);
          PersistentInformation.GetInstance().SetAlbumDirtyIfNotNew(album.SetId);
        }
      }
      // Now delete the tags associated with the photo.
      PersistentInformation.GetInstance().DeleteAllTags(photoid);
    }
    
    private void RemovePhotoFromModel(TreePath path) {
	    // Convert path to childpath.
      TreePath childpath = filter.ConvertPathToChildPath(path);
      TreeIter iter;
      photoStore.GetIter(out iter, childpath);
      photoStore.Remove(ref iter);
      _photos.RemoveAt(childpath.Indices[0]);
  	}
  	        
    private void OnPhotoDraggedForDeletion(object o, DragDataReceivedArgs args) {
      // Don't do anything if conflict button is on.
      if (conflictbutton.Active || lockbutton.Active) return;
      if (streambutton.Active) {
        int countphotos = treeview2.Selection.CountSelectedRows();
        MessageDialog md = new MessageDialog(
  	        window1, DialogFlags.DestroyWithParent, MessageType.Question,
  	        ButtonsType.YesNo, 
  	        "Do you really wish to permanently delete " + countphotos
  	        + " photo(s) from the server?");
  	    ResponseType response = ResponseType.No;
  	    response = (ResponseType) md.Run();
  	    md.Destroy();
  	    if (response == ResponseType.No) return;
  	    md = new MessageDialog(
  	        window1, DialogFlags.DestroyWithParent, MessageType.Question,
  	        ButtonsType.YesNo, 
  	        "Are you _really_ sure you wish to delete these " + countphotos
  	        + " photo(s)? They will be deleted from the flickr server forever!");
  	    response = ResponseType.No;
  	    response = (ResponseType) md.Run();
  	    md.Destroy();
  	    if (response == ResponseType.No) return;
  	    if (response == ResponseType.Yes) {
  	      TreePath[] selectedpaths = treeview2.Selection.GetSelectedRows();
  	      for (int i=0; i<selectedpaths.Length; i++) {
  	        // The problem here that I'm solving is that, everytime we
  	        // remove an entry from the photoStore, the paths that we
  	        // get from selection, point to different photos. Basically,
  	        // it would point to one photo below the selected one. So,
  	        // I'm making adjustments to the path itself, to make it
  	        // point to the original selected photo.
  	        TreePath path = selectedpaths[i];
  	        for (int j=0; j<i; j++) {
  	          path.Prev();
  	        }
  	        Photo photo = GetPhoto(path);
  	        string photoid = photo.Id;
  	        // Handle the magic of deletion of photos, and updates database
  	        // wide.
            PhotoDeletionHandler(photoid);
            RemovePhotoFromModel(path);
  	      }
  	    SetPhotoCountTip(streambutton, _photos.Count);
  	    }
      }
      else if (uploadbutton.Active) {
        TreePath[] selectedpaths = treeview2.Selection.GetSelectedRows();
        for (int i=0; i<selectedpaths.Length; i++) {
          TreePath path = selectedpaths[i];
          // This method is explained above in stream button section.
          for (int j=0; j<i; j++) {
            path.Prev();
          }
          Photo photo = GetPhoto(path);
          PersistentInformation.GetInstance().DeleteThumbnail(photo.Id);
          PersistentInformation.GetInstance().DeleteSmallImage(photo.Id);
          string filename = photo.Id.Replace("'", "''");
          PersistentInformation.GetInstance().DeleteEntryFromUpload(filename);
          RemovePhotoFromModel(path);
        }
        SetPhotoCountTip(uploadbutton, _photos.Count);
      } 
      else if (downloadbutton.Active) {
        TreePath[] selectedpaths = treeview2.Selection.GetSelectedRows();
        for (int i=0; i<selectedpaths.Length; i++) {
          TreePath path = selectedpaths[i];
          // This method is explained above in stream button section.
          for (int j=0; j<i; j++) {
            path.Prev();
          }
          Photo photo = GetPhoto(path);
          PersistentInformation.GetInstance().DeleteEntryFromDownload(photo.Id);
          RemovePhotoFromModel(path);
        }
        SetPhotoCountTip(downloadbutton, _photos.Count);
      }
      else if (selectedtab == 0) { // albums
        string setid = ((Album) _albums[leftcurselectedindex]).SetId;
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
		      string photoid = GetPhoto(path).Id;
		      PersistentInformation.GetInstance().DeletePhotoFromAlbum(photoid, setid);
		      PersistentInformation.GetInstance().SetAlbumDirtyIfNotNew(setid);
        }
        if (PersistentInformation.GetInstance().GetPhotoIdsForAlbum(setid).Count == 0) {
          PersistentInformation.GetInstance().SetPrimaryPhotoForAlbum(setid, "0");
          // Delete the album if it is new.
          if (PersistentInformation.GetInstance().IsAlbumNew(setid)) {
            PersistentInformation.GetInstance().DeleteAlbum(setid);
          }
        }
      }
      else if (selectedtab == 1) { // tags
        string tag = (string) _tags[leftcurselectedindex];
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          Photo photo = GetPhoto(path);
          // Check if the original version is stored in db. Allow for revert.
		      if (!PersistentInformation.GetInstance().HasOriginalPhoto(photo.Id)
		          && !PersistentInformation.GetInstance().IsPhotoDirty(photo.Id)) {
		        PersistentInformation.GetInstance().InsertOriginalPhoto(photo);
		      }
		      if (PersistentInformation.GetInstance().HasTag(photo.Id, tag)) {
		        PersistentInformation.GetInstance().DeleteTag(photo.Id, tag);
		        PersistentInformation.GetInstance().SetPhotoDirty(photo.Id, true);
		      }
		    }
      }
      else if (selectedtab == 2) { // pools
        PersistentInformation.Entry entry = 
            (PersistentInformation.Entry) _pools[leftcurselectedindex];
        string groupid = entry.entry1;
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          string photoid = GetPhoto(path).Id;
          if (PersistentInformation.GetInstance().IsPhotoAddedToPool(photoid, groupid)) {
            PersistentInformation.GetInstance().DeletePhotoFromPool(photoid, groupid);
          } else {
            PersistentInformation.GetInstance()
                                 .MarkPhotoDeletedFromPool(photoid, groupid, true);
          }
        }
      }
      else if (selectedtab == 3) { // blogs
        PersistentInformation.Entry entry =
            (PersistentInformation.Entry) _blogs[leftcurselectedindex];
        string blogid = entry.entry1;
        foreach (TreePath path in treeview2.Selection.GetSelectedRows()) {
          string photoid = GetPhoto(path).Id;
          PersistentInformation.GetInstance().DeleteEntryFromBlog(blogid, photoid);
        }
      } // if else ends here.
      UpdatePhotoCountLabel();
      RefreshLeftTreeView();
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
