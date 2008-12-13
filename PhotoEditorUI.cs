
using System;
using System.Collections;
using Gtk;
using Glade;

	public class PhotoEditorUI
	{
    [Glade.Widget]
    Window window2;
    
    [Glade.Widget]
    Notebook notebook1;
    
    [Glade.Widget]
    Table table1;
    
    [Glade.Widget]
    Label label2;
    
    [Glade.Widget]
    Label label3;
    
    [Glade.Widget]
    Label label4;
    
    [Glade.Widget]
    Label label5;
    
    [Glade.Widget]
    Label label6;
    
    [Glade.Widget]
    Label label7;
    
    [Glade.Widget]
    Entry entry1;
    
    [Glade.Widget]
    TextView textview5;
    
    [Glade.Widget]
    ComboBox combobox1;
    
    [Glade.Widget]
    ComboBox combobox2;
    
    [Glade.Widget]
    IconView iconview1;
    
    [Glade.Widget]
    TextView textview3;
    
    [Glade.Widget]
    TextView textview4;
    
    [Glade.Widget]
    Button button3;
    
    [Glade.Widget]
    Button button4;
    
    [Glade.Widget]
    CheckButton checkbutton1;
    
    [Glade.Widget]
    Button button5;
    
    [Glade.Widget]
    EventBox eventbox5;
    
    [Glade.Widget]
    Label label14;
    
    // Save and Close
    [Glade.Widget]
    Button button6;
    
    [Glade.Widget]
    Image image3;
    
    // Search tag
    [Glade.Widget]
    Label label15;
    
    [Glade.Widget]
    Entry entry2;
    
    // Revert button
    [Glade.Widget]
    Button button9;
    
    [Glade.Widget]
    Label label17;
    
    [Glade.Widget]
    Label label18;
    
    [Glade.Widget]
    Entry entry4;
    
    [Glade.Widget]
    TextView textview7;
    
    // Comments Tab
    [Glade.Widget]
    Toolbar toolbar3;
    
    [Glade.Widget]
    TreeView treeview3;
    
    private ArrayList _tags;
    private ArrayList _selectedphotos;
    private System.Collections.Generic.IDictionary<string, Photo> _originalphotos;
    private int _curphotoindex;
    private string _currenturl;
    private TreeModelFilter _filter;
    private ArrayList _comments;
    
    // I haven't been able to figure out a way so that only the user
    // selection of combobox elements, would trigger the OnPrivacyChanged
    // and OnLicenseChanged methods. So, I'll go with a hack, by passing
    // in a boolean variable, which can tell these methods, when to not
    // heed to the changes done to the box.
    private bool _ignorechangedevent;
    
    private bool _isconflictmode;
    private bool _isuploadmode;
    private bool _isblogmode;
		private Tooltips tips;
		private ToolButton _addcommentbutton;
		private ToolButton _editcommentbutton;
		private ToolButton _deletecommentbutton;
		
		private PhotoEditorUI(ArrayList selectedphotos, DeskFlickrUI.ModeSelected mode)
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window2", null);
		  gxml.Autoconnect (this);
		  _isconflictmode = (mode == DeskFlickrUI.ModeSelected.ConflictMode);
		  _isuploadmode = (mode == DeskFlickrUI.ModeSelected.UploadMode);
      _isblogmode = (mode == DeskFlickrUI.ModeSelected.BlogMode);
      if (mode == DeskFlickrUI.ModeSelected.BlogAndConflictMode) {
        _isconflictmode = true;
        _isblogmode = true;
      }
      _tags = new ArrayList();
      _comments = new ArrayList();
		  window2.Title = "Edit information for " + selectedphotos.Count + " photos";
		  window2.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Information");
		  notebook1.NextPage();
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Tags");
		  notebook1.NextPage();
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Comments");
		  
		  tips = new Tooltips();
		  SetCommentsToolBar();
		  tips.Enable();
		  SetCommentsTree();
		  
      if (_isconflictmode) {
		    notebook1.NextPage();
		    notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Information at Server");
		  } else {
		    notebook1.RemovePage(3);
		  }
		  
		  if (_isblogmode) {
		    notebook1.NextPage();
		    notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Blog Entry");
		    notebook1.Page = 3; // Default page is blog entry if editor is in Blog mode.
		  } else {
		    if (_isconflictmode) notebook1.RemovePage(4);
		    else notebook1.RemovePage(3);
		    notebook1.Page = 0; // Default page is photo editing.
		  }
		  
		  
		  table1.SetColSpacing(0, 50);
		  // Set Labels
		  label6.Text = "Edit";
		  label5.Text = "Title:";
		  label4.Text = "Description:";
		  label3.Text = "Visibility:";
		  label2.Text = "License:";
		  if (_isuploadmode) label2.Sensitive = false;
		  // Labels for blog tab.
		  label17.Text = "Title: ";
		  label18.Text = "Description: ";
		  
		  // Search box
      label15.Markup = "<span weight='bold'>Search: </span>";
      entry2.Changed += new EventHandler(OnFilterEntryChanged);
      
      // Revert button
      button9.Label = "Revert Photo(s)";
      button9.Clicked += new EventHandler(OnRevertButtonClicked);
      
		  // entry1.ModifyFont(Pango.FontDescription.FromString("FreeSerif 10"));
		  SetPrivacyComboBox();
		  SetLicenseComboBox();
		  SetTagTreeView();
		  
		  // Make previous and next buttons insensitive. They'll become sensitive
		  // only when the user ticks the 'Per Photo' checkbutton.
      button3.Sensitive = false;
      button4.Sensitive = false;
      checkbutton1.Toggled += new EventHandler(OnPerPageCheckToggled);
      button3.Clicked += new EventHandler(OnPrevButtonClick);
      button4.Clicked += new EventHandler(OnNextButtonClick);
      button5.Clicked += new EventHandler(OnSaveButtonClick);
      button6.Clicked += new EventHandler(OnCancelButtonClick);
      
      entry1.Changed += new EventHandler(OnTitleChanged);
      textview5.Buffer.Changed += new EventHandler(OnDescChanged);
      
      combobox1.Changed += new EventHandler(OnPrivacyChanged);
      combobox2.Changed += new EventHandler(OnLicenseChanged);
      
      entry4.Changed += new EventHandler(OnBlogTitleChanged);
      textview7.Buffer.Changed += new EventHandler(OnBlogDescChanged);
      
      textview3.Buffer.Changed += new EventHandler(OnTagsChanged);

      TextTag texttag = new TextTag("conflict");
      texttag.Font = "Times Italic 10";
      texttag.WrapMode = WrapMode.Word;
      texttag.ForegroundGdk = new Gdk.Color(0x99, 0, 0);
      textview4.Buffer.TagTable.Add(texttag);
      
      // Showing photos should be the last step.
      this._selectedphotos = selectedphotos;
      if (selectedphotos.Count == 1) {
        checkbutton1.Sensitive = false;
        ShowInformationForCurrentPhoto();
      } else {
        EmbedCommonInformation();
      }
      // Save a copy of the original photos, so that only those photos
      // which  have been edited, would have their dirty bit set. Advantage:
      // this would reduce the number of dirty photos, and hence there'll 
      // be lesser photos to update when sycing with server.
      _originalphotos = new System.Collections.Generic.Dictionary<string, Photo>();
      foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
        Photo p = sel.photo;
        _originalphotos.Add(p.Id, new Photo(p));
      }

      eventbox5.ButtonPressEvent += OnLinkPressed;
      eventbox5.EnterNotifyEvent += MouseOnLink;
      eventbox5.LeaveNotifyEvent += MouseLeftLink;
      
		  window2.ShowAll();
		}
		
		public static void FireUp(ArrayList selectedphotos, DeskFlickrUI.ModeSelected mode) {
		  new PhotoEditorUI(selectedphotos, mode);
		}
		
		private void SetCommentsToolBar() {
		  toolbar3.ToolbarStyle = ToolbarStyle.Icons;
		  _addcommentbutton = new ToolButton(Stock.Add);
		  _addcommentbutton.Sensitive = true;
		  _addcommentbutton.SetTooltip(tips, "Add Comment", "Add Comment");
		  _addcommentbutton.Clicked += new EventHandler(OnAddCommentButtonClicked);
		  toolbar3.Insert(_addcommentbutton, -1);
		  
		  _editcommentbutton = new ToolButton(Stock.Edit);
		  _editcommentbutton.Sensitive = true;
		  _editcommentbutton.SetTooltip(tips, "Edit Comment", "Edit Comment");
		  _editcommentbutton.Clicked += new EventHandler(OnEditCommentButtonClicked);
		  toolbar3.Insert(_editcommentbutton, -1);
		  
		  _deletecommentbutton = new ToolButton(Stock.Delete);
		  _deletecommentbutton.Sensitive = true;
		  _deletecommentbutton.SetTooltip(tips, "Delete Comment", "Delete Comment");
		  _deletecommentbutton.Clicked += new EventHandler(OnDeleteCommentButtonClicked);
		  toolbar3.Insert(_deletecommentbutton, -1);
		}
		
		private void SetActivateComments(bool issensitive) {
		  _addcommentbutton.Sensitive = issensitive;
		  _editcommentbutton.Sensitive = issensitive;
		  _deletecommentbutton.Sensitive = issensitive;
		  if (!issensitive) {
		    treeview3.Model = null; 
		  }
		}
		
		private bool RunInputTextDialog(string title, string button1, 
		                                string originalcomment, out string comment) {
		  Dialog d = new Dialog();
		  d.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  d.Title = title;
		  d.AddButton(button1, Gtk.ResponseType.Ok);
		  d.AddButton(Stock.Cancel, Gtk.ResponseType.Cancel);
		  ScrolledWindow swin = new ScrolledWindow();
		  swin.HscrollbarPolicy = Gtk.PolicyType.Never;
		  swin.VscrollbarPolicy = Gtk.PolicyType.Automatic;
		  swin.ShadowType = Gtk.ShadowType.EtchedIn;
		  TextView tv = new TextView();
		  tv.WrapMode = Gtk.WrapMode.WordChar;
		  tv.BorderWidth = 2;
		  tv.Buffer.Text = originalcomment;
		  swin.Add(tv);
		  d.VBox.Add(swin);
		  d.WidthRequest = 400;
		  d.HeightRequest = 200;
		  d.ShowAll();
		  Gtk.ResponseType response = (Gtk.ResponseType) d.Run();
		  comment = tv.Buffer.Text;
		  d.Destroy();
		  if (response == Gtk.ResponseType.Cancel) return false;
		  else if (comment.Trim() == "") return false;
		  else return true;
		}
		
		private void OnAddCommentButtonClicked(object o, EventArgs args) {
		  string comment;
		  if (!RunInputTextDialog("Add New Comment", Stock.Add, "", out comment)) return;
		  Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		  PersistentInformation.GetInstance().InsertNewComment(p.Id, comment);
		  PopulateComments(p.Id);
		}
		
		private void OnEditCommentButtonClicked(object o, EventArgs args) {
		  if (treeview3.Selection.GetSelectedRows().Length == 0) return;
		  Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		  TreePath path = treeview3.Selection.GetSelectedRows()[0];
		  Comment comment = (Comment) _comments[path.Indices[0]];
		  string originalcomment = comment.CommentHtml;
		  string commenthtml;
		  if (!RunInputTextDialog(
		      "Edit Comment", Stock.Ok, originalcomment, out commenthtml)) return;
		  PersistentInformation.GetInstance().UpdateComment(
		      p.Id, comment.CommentId, commenthtml, true);
		  PopulateComments(p.Id);
    }
    
		private void OnDeleteCommentButtonClicked(object o, EventArgs args) {
		  if (treeview3.Selection.GetSelectedRows().Length == 0) return;
		  MessageDialog md = new MessageDialog(
  	      window2, DialogFlags.DestroyWithParent, MessageType.Question,
  	      ButtonsType.YesNo, 
  	      "Are you sure you want to delete the comment?");
  	  ResponseType response = ResponseType.No;
  	  response = (ResponseType) md.Run();
  	  md.Destroy();
  	  if (response == ResponseType.No) return;
		  Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		  TreePath path = treeview3.Selection.GetSelectedRows()[0];
		  string commentid = ((Comment) _comments[path.Indices[0]]).CommentId;
		  if (commentid.IndexOf("new:") > -1) { // new comment.
		    PersistentInformation.GetInstance().DeleteComment(p.Id, commentid);
		  } else {
		    PersistentInformation.GetInstance().MarkCommentForDeletion(p.Id, commentid);
		  }
		  PopulateComments(p.Id);
		}
		
		private void SetCommentsTree() {
		  Gtk.CellRendererText textcell = new Gtk.CellRendererText();
		  textcell.WrapMode = Pango.WrapMode.WordChar;
		  textcell.WrapWidth = 450;
		  
		  treeview3.AppendColumn("Comments", textcell, "markup", 0);
		  treeview3.HeadersVisible = false;
		  treeview3.RulesHint = true;
		  treeview3.RowActivated += new RowActivatedHandler(OnEditCommentButtonClicked);
		}
		
	  private int GetIndexOfPrivacyBox(Photo p) {
	    if (p.IsPublic == 1) return 1;
	    else if ((p.IsFriend == 1) && (p.IsFamily == 1)) return 2;
	    else if (p.IsFriend == 1) return 3;
	    else if (p.IsFamily == 1) return 4;
	    else return 5;
		}
		
		private void SetPhotoPrivacyFromBox(ref Photo p, int index) {
		  if (index == 1) {
		    p.IsPublic = 1; p.IsFriend = 0; p.IsFamily = 0;
		  } else if (index == 2) {
		    p.IsPublic = 0; p.IsFriend = 1; p.IsFamily = 1;
		  } else if (index == 3) {
		    p.IsPublic = 0; p.IsFriend = 1; p.IsFamily = 0;
		  } else if (index == 4) {
		    p.IsPublic = 0; p.IsFriend = 0; p.IsFamily = 1;
		  } else {
		    p.IsPublic = 0; p.IsFriend = 0; p.IsFamily = 0;
		  }
		}
		
		private void SetPrivacyComboBox() {
			// Set Combo boxes - Privacy Level
		  combobox1.Clear();
		  CellRendererText cell = new CellRendererText();
		  combobox1.PackStart(cell, false);
		  combobox1.AddAttribute(cell, "text", 0);
		  ListStore store1 = new ListStore(typeof(string));
		  store1.AppendValues("");
		  store1.AppendValues(GetPangoFormattedText("Public"));
		  store1.AppendValues(GetPangoFormattedText("Only Friends and Family"));
		  store1.AppendValues(GetPangoFormattedText("Only Friends"));
		  store1.AppendValues(GetPangoFormattedText("Only Family"));
		  store1.AppendValues(GetPangoFormattedText("Private"));
		  combobox1.Model = store1;
		}
		
		private int GetIndexOfLicenseBox(Photo p) {
		  return p.License + 1;
		}
		
		private Photo SetPhotoLicenseFromBox(ref Photo p, int index) {
		  p.License = index - 1;
		  return p;
		}
		
		private void SetLicenseComboBox() {
		  if (_isuploadmode) combobox2.Sensitive = false;
			// Set Combo boxes - License
		  combobox2.Clear();
		  CellRendererText cell2 = new CellRendererText();
		  combobox2.PackStart(cell2, false);
		  combobox2.AddAttribute(cell2, "text", 0);
		  ListStore store2 = new ListStore(typeof(string));
		  store2.AppendValues("");
		  store2.AppendValues(GetPangoFormattedText("All Rights Reserved"));
		  store2.AppendValues(GetPangoFormattedText("Attribution-NonCommercial-ShareAlike License"));
		  store2.AppendValues(GetPangoFormattedText("Attribution-NonCommercial License"));
		  store2.AppendValues(GetPangoFormattedText("Attribution-NonCommercial-NoDerivs License"));
		  store2.AppendValues(GetPangoFormattedText("Attribution License"));
		  store2.AppendValues(GetPangoFormattedText("Attribution-ShareAlike License"));
		  store2.AppendValues(GetPangoFormattedText("Attribution-NoDerivs License"));
		  combobox2.Model = store2;
		}
		
		private void OnFilterEntryChanged(object o, EventArgs args) {
		  _filter.Refilter();
		}
		
		private void SetTagTreeView() {
		  ListStore tagstore = new ListStore(typeof(string));
		  _tags.Clear();
		  foreach(PersistentInformation.Entry entry in 
		                        PersistentInformation.GetInstance().GetAllTags()) {
		    string tag = entry.entry1;
		    string numpics = entry.entry2;
		    _tags.Add(tag);
		    tagstore.AppendValues(tag + "(" + numpics + ")");
		  }
		  _filter = new TreeModelFilter(tagstore, null);
		  _filter.VisibleFunc = new TreeModelFilterVisibleFunc(FilterTags);
		  iconview1.Model = _filter;
		  iconview1.TextColumn = 0;
		  iconview1.ItemActivated += new ItemActivatedHandler(OnTagClicked);
		}
		
		private bool FilterTags(TreeModel model, TreeIter iter) {
		  int index = model.GetPath(iter).Indices[0];
		  string tag = (string) _tags[index];
		  string query = entry2.Text;
		  System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase;
		  return (tag.IndexOf(query, comp) > -1);
		}
		
		private string GetPangoFormattedText(string text) {
		  //return String.Format("<span font_desc='FreeSerif 10'>{0}</span>", text);
		  return text;
		}
		
		private void SetPhotoLink(string photoid) {
		  string userid = PersistentInformation.GetInstance().UserId;
		  if (_isuploadmode || photoid.Equals("") || userid.Equals("")) {
		    _currenturl = "";
		    label14.Text = "";
		    return;
		  }
		  _currenturl = String.Format(
		      "http://www.flickr.com/photos/{0}/{1}", userid, photoid);
		  label14.Markup = "<span foreground='#666666' style='italic'>View in browser</span>";
		}
		
		private void OnLinkPressed(object sender, EventArgs args) {
		  if (_currenturl.Equals("")) return;
		  System.Diagnostics.Process.Start(_currenturl);
		}
				
		private void MouseOnLink(object sender, EnterNotifyEventArgs args) {
		  if (_currenturl.Equals("")) return;
		  label14.Markup = 
		      "<span foreground='#666666' underline='low' style='italic'>View in browser</span>";
		}
		
		private void MouseLeftLink(object sender, LeaveNotifyEventArgs args) {
		  if (_currenturl.Equals("")) return;
		  label14.Markup = "<span foreground='#666666' style='italic'>View in browser</span>";
		}
		
		private bool IsPhotoEdited(Photo p) {
		  if (!PersistentInformation.GetInstance().HasOriginalPhoto(p.Id)) return false;
		  Photo originalphoto = PersistentInformation.GetInstance().GetOriginalPhoto(p.Id);
		  return !originalphoto.isEqual(p);
		}
		
		private void EmbedCommonInformation() {
      // Work upon the selected photos here.
      Photo firstphoto = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[0]).photo;
		  string commonTitle = firstphoto.Title;
		  string commonDesc = firstphoto.Description;
		  int commonPrivacy = GetIndexOfPrivacyBox(firstphoto);
		  int commonLicense = GetIndexOfLicenseBox(firstphoto);
		  ArrayList tagschosen = firstphoto.Tags;
		  bool isanyphotodirty = false;  
		  foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		    Photo p = sel.photo;
		    if (!commonTitle.Equals(p.Title)) commonTitle = "";
		    if (!commonDesc.Equals(p.Description)) commonDesc = "";
		    if (commonPrivacy != GetIndexOfPrivacyBox(p)) commonPrivacy = 0;
		    if (commonLicense != GetIndexOfLicenseBox(p)) commonLicense = 0;
		    tagschosen = Utils.GetIntersection(tagschosen, p.Tags);
		    if (!_isuploadmode && IsPhotoEdited(p)) isanyphotodirty = true;
		  }
		  entry1.Text = commonTitle;
		  textview5.Buffer.Text = commonDesc;
		  
		  _ignorechangedevent = true;
		  {
		  combobox1.Active = commonPrivacy;
		  combobox2.Active = commonLicense;
		  textview3.Buffer.Text = Utils.GetDelimitedString(tagschosen, " ");
		  }
		  _ignorechangedevent = false;
		  
		  if (_isblogmode) {
		    BlogEntry firstblogentry = ((DeskFlickrUI.BlogSelectedPhoto) _selectedphotos[0]).blogentry;
		    string commonblogtitle = firstblogentry.Title;
		    string commonblogdesc = firstblogentry.Desc;
		    foreach (DeskFlickrUI.BlogSelectedPhoto bsel in _selectedphotos) {
		      if (!commonblogtitle.Equals(bsel.blogentry.Title)) commonblogtitle = "";
		      if (!commonblogdesc.Equals(bsel.blogentry.Desc)) commonblogdesc = "";
		    }
		    entry4.Text = commonblogtitle;
		    textview7.Buffer.Text = commonblogdesc;
		  }
		  
		  image3.Sensitive = false;
		  image3.Pixbuf = null;
		  button3.Sensitive = false;
		  button4.Sensitive = false;
		  label7.Text = "";
		  label7.Sensitive = false;
		  SetPhotoLink("");
		  button9.Sensitive = isanyphotodirty;
		  SetActivateComments(false);
		}
		
		private void ApplyConflictTagToLine(int startline, int endline) {
		  TextBuffer buf = textview4.Buffer;
		  TextIter start = buf.GetIterAtLine(startline);
		  TextIter end = buf.GetIterAtLine(endline);
		  buf.ApplyTag("conflict", start, end);
		  textview4.Buffer = buf;
		}
		
		private void HighlightDifferences(Photo serverphoto, Photo photo) {
		  if (!serverphoto.Title.Equals(photo.Title))
		      ApplyConflictTagToLine(0, 1);
		  if (!serverphoto.Description.Equals(photo.Description))
		      ApplyConflictTagToLine(1, 2);
		  if (!serverphoto.PrivacyInfo.Equals(photo.PrivacyInfo))
		      ApplyConflictTagToLine(2, 3);
		  if (!serverphoto.LicenseInfo.Equals(photo.LicenseInfo))
		      ApplyConflictTagToLine(3, 4);
		  if (!serverphoto.TagString.Equals(photo.TagString))
		      ApplyConflictTagToLine(4, 5);
		}
		
		private void ActivateRevertButton() {
		  if (!_isuploadmode) button9.Sensitive = true;
		}
		
		private void SetTitleTopRight(string title) {
		  if (title.Length > 30) {
		    title = title.Substring(0, 30) + "...";
		  }
		  label7.Markup = "<span weight='bold'>" + Utils.EscapeForPango(title) 
		      + "</span>";
		}
		
		private void PopulateComments(string photoid) {
		  _comments.Clear();
		  ListStore store = new Gtk.ListStore(typeof(string));
		  foreach (Comment comment in 
		      PersistentInformation.GetInstance().GetCommentsForPhoto(photoid)) {
		    string username = comment.UserName;
		    string safecomment = Utils.EscapeForPango(comment.CommentHtml);
		    string text = String.Format(
		        "<span weight='bold'>{0}:</span> {1}",
		        username, safecomment);
		    store.AppendValues(text);
		    _comments.Add(comment);
		  }
		  treeview3.Model = store;
		  treeview3.ShowAll();
		}
		
		private void ShowInformationForCurrentPhoto() {
		  DeskFlickrUI.SelectedPhoto sel = 
		      (DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex];
		  Photo p = sel.photo;
		  entry1.Text = p.Title;
		  textview5.Buffer.Text = p.Description;
		  
		  _ignorechangedevent = true;
		  {
		  combobox1.Active = GetIndexOfPrivacyBox(p);
		  combobox2.Active = GetIndexOfLicenseBox(p);
		  textview3.Buffer.Text = Utils.GetDelimitedString(p.Tags, " ");
		  }
		  _ignorechangedevent = false;
		  
      if (_isconflictmode) {
        Photo serverphoto = DeskFlickrUI.GetInstance().GetServerPhoto(p.Id);
        string text = String.Format(
              "Title:\t\t{0}\n"
            + "Description:\t{1}\n"
            + "Visibility:\t\t{2}\n"
            + "License:\t\t{3}\n"
            + "Tags:\t\t{4}\n",
            serverphoto.Title, serverphoto.Description, 
            serverphoto.PrivacyInfo, serverphoto.LicenseInfo,
            serverphoto.TagString);
        textview4.Buffer.Text = text;
        HighlightDifferences(serverphoto, p);
      }
      
      if (_isblogmode) {
        BlogEntry blogentry = ((DeskFlickrUI.BlogSelectedPhoto) sel).blogentry;
        entry4.Text = blogentry.Title;
        textview7.Buffer.Text = blogentry.Desc;
      }
      
		  image3.Sensitive = true;
		  image3.Pixbuf = p.SmallImage;
		  label7.Sensitive = true;
      SetTitleTopRight(p.Title);
      SetPhotoLink(p.Id);
      button9.Sensitive = !_isuploadmode && IsPhotoEdited(p);
      PopulateComments(p.Id);
      SetActivateComments(!_isuploadmode);
		}

		private void OnTagClicked(object o, ItemActivatedArgs args) {
		  TreePath childpath = _filter.ConvertPathToChildPath(args.Path);
		  string tag = (string) _tags[childpath.Indices[0]];
		  ArrayList tagschosen;
		  
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    p.AddTag(tag);
		    tagschosen = p.Tags;
		  } else {
		    foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		      Photo p = sel.photo;
		      p.AddTag(tag);
		    }
		    tagschosen = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[0]).photo.Tags;
		    foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		      Photo p = sel.photo;
		      tagschosen = Utils.GetIntersection(tagschosen, p.Tags);
		    }
		  }
		  ActivateRevertButton();
		  _ignorechangedevent = true;
		  textview3.Buffer.Text = Utils.GetDelimitedString(tagschosen, " ");
		  _ignorechangedevent = false;
		}
		
		private void OnTagsChanged(object o, EventArgs args) {
		  ArrayList parsedtags = Utils.ParseTagsFromString(textview3.Buffer.Text);
		  if (parsedtags == null || _ignorechangedevent) {
		    return;
		  }
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    p.Tags = parsedtags;
		  } else {
		    foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		      Photo p = sel.photo;
		      p.Tags = parsedtags;
		    }
		  }
		  ActivateRevertButton();
		}
		
		private void OnTitleChanged(object o, EventArgs args) {
		  // if the entry is not being changed by the user, then 
		  // don't do the updates.
		  if (!entry1.IsFocus) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    p.Title = entry1.Text;
		  } else {
		    // Group of photos
		    foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		      Photo p = sel.photo;
		      p.Title = entry1.Text;
		    }
		  }
		  SetTitleTopRight(entry1.Text);
		  ActivateRevertButton();
		}
		
		private void OnDescChanged(object o, EventArgs args) {
		  if (!textview5.IsFocus) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    p.Description = textview5.Buffer.Text;
		  } else {
		    // Group of photos
		    foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		      Photo p = sel.photo;
		      p.Description = textview5.Buffer.Text;
		    }
		  }
		  ActivateRevertButton();
		}
		
		private void OnPrivacyChanged(object o, EventArgs args) {
      if (_ignorechangedevent) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    SetPhotoPrivacyFromBox(ref p, combobox1.Active);
		  } else {
		    for (int i=0; i < _selectedphotos.Count; i++) {
		      Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[i]).photo;
		      SetPhotoPrivacyFromBox(ref p, combobox1.Active);
		    }
		  }
		  ActivateRevertButton();
		}
		
		private void OnLicenseChanged(object o, EventArgs args) {
		  if (_ignorechangedevent) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    SetPhotoLicenseFromBox(ref p, combobox2.Active);
		  } else {
		    for (int i=0; i < _selectedphotos.Count; i++) {
		      Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[i]).photo;
		      SetPhotoLicenseFromBox(ref p, combobox2.Active);
		    }
		  }
		  ActivateRevertButton();
    }

    private void OnBlogTitleChanged(object o, EventArgs args) {
      if (!entry4.IsFocus) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    BlogEntry blogentry = 
		        ((DeskFlickrUI.BlogSelectedPhoto) _selectedphotos[_curphotoindex]).blogentry;
		    blogentry.Title = entry4.Text;
		  } else {
		    // Group of photos
		    foreach (DeskFlickrUI.BlogSelectedPhoto bsel in _selectedphotos) {
		      BlogEntry blogentry = bsel.blogentry;
		      blogentry.Title = entry4.Text;
		    }
		  }
    }

    private void OnBlogDescChanged(object o, EventArgs args) {
      if (!textview7.IsFocus) return;
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    BlogEntry blogentry = 
		        ((DeskFlickrUI.BlogSelectedPhoto) _selectedphotos[_curphotoindex]).blogentry;
		    blogentry.Desc = textview7.Buffer.Text;
		  } else {
		    // Group of photos
		    foreach (DeskFlickrUI.BlogSelectedPhoto bsel in _selectedphotos) {
		      BlogEntry blogentry = bsel.blogentry;
		      blogentry.Desc = textview7.Buffer.Text;
		    }
		  }
    }
    
		private void OnRevertButtonClicked(object sender, EventArgs args) {
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[_curphotoindex]).photo;
		    Photo originalp = PersistentInformation.GetInstance().GetOriginalPhoto(p.Id);
		    if (originalp != null) p.CopyContent(originalp);
		    ShowInformationForCurrentPhoto();
		  }
		  else {
		    for (int i=0; i < _selectedphotos.Count; i++) {
		      Photo p = ((DeskFlickrUI.SelectedPhoto) _selectedphotos[i]).photo;
          Photo originalp = PersistentInformation.GetInstance().GetOriginalPhoto(p.Id);
          if (originalp != null) p.CopyContent(originalp);
		    }
		    EmbedCommonInformation();
		  }
		  button9.Sensitive = false;
		}
		
		private void OnPerPageCheckToggled(object o, EventArgs args) {
		  if (checkbutton1.Active) {
		    _curphotoindex = 0;
		    button3.Sensitive = false; // Previous button
		    button4.Sensitive = true; // Next button
		    ShowInformationForCurrentPhoto();
		  } else {
		    EmbedCommonInformation();
		  }
		}
		
		private void OnNextButtonClick(object o, EventArgs args) {
		  _curphotoindex += 1;
		  if (_curphotoindex == _selectedphotos.Count -1) {
		    button4.Sensitive = false;
		  }
		  if (_curphotoindex == 1) {
		    button3.Sensitive = true;
		  }
		  ShowInformationForCurrentPhoto();
		}
		
		private void OnPrevButtonClick(object o, EventArgs args) {
		  _curphotoindex -= 1;
		  if (_curphotoindex == 0) {
		    button3.Sensitive = false;
		  }
		  if (_curphotoindex == _selectedphotos.Count - 2) {
		    button4.Sensitive = true;
		  }
		  ShowInformationForCurrentPhoto();
		}
		
		private void OnCancelButtonClick(object o, EventArgs args) {
		  window2.Destroy();
		}
		
		private void OnSaveButtonClick(object o, EventArgs args) {
		  foreach (DeskFlickrUI.SelectedPhoto sel in _selectedphotos) {
		    Photo p = sel.photo;
		    if (_isuploadmode) {
		      PersistentInformation.GetInstance().UpdateInfoForUploadPhoto(p);
		    }
		    else {
  		    if (_isconflictmode) {
  		      Photo serverphoto = DeskFlickrUI.GetInstance().GetServerPhoto(p.Id);
  		      p.LastUpdate = serverphoto.LastUpdate;
  		      DeskFlickrUI.GetInstance().RemoveServerPhoto(p.Id);
  		    }
  		    if (_isblogmode) {
  		      BlogEntry blogentry = ((DeskFlickrUI.BlogSelectedPhoto) sel).blogentry;
  		      PersistentInformation.GetInstance().UpdateEntryToBlog(blogentry);
  		    }
  		    // This original photo is the photo sent to the editor initially.
  		    // Note that this photo is different from the photos stored in
  		    // originalphoto table, which are the ones originally retrieved from
  		    // the server.
  		    Photo originalp = _originalphotos[p.Id];
  		    bool ischanged = false;
  		    if (!p.isMetaDataEqual(originalp)) {
  		      ischanged = true;    
  		      PersistentInformation.GetInstance().UpdateMetaInfoPhoto(p);
  		    }
  		    if (!p.isTagsEqual(originalp)) {
  		      ischanged = true;
  		      p.SortTags();
  		      PersistentInformation.GetInstance().UpdateTagsForPhoto(p);
  		    }
          
  		    // Get the originally retrieved photo from server.
  		    Photo originalcleanphoto = PersistentInformation.GetInstance().GetOriginalPhoto(p.Id);
  		    if (originalcleanphoto == null || p.isEqual(originalcleanphoto)) {
  		      PersistentInformation.GetInstance().SetPhotoDirty(p.Id, false);
  		    }
  		    else if (ischanged) {
  		      PersistentInformation.GetInstance().SetPhotoDirty(p.Id, true);
  		    } else {
            Console.Error.WriteLine(
                "Inconsistent State: Photo seems to be different than stored"
                + " in server, but the metadata and tags are unchanged.");
                    Console.Out.WriteLine("=== Local photo ===");
            Console.Out.WriteLine(p.PrettyPrint());
            Console.Out.WriteLine("=== Server Photo ===");
            Console.Out.WriteLine(originalcleanphoto.PrettyPrint());
          }
  		  }
		  }
		  window2.Destroy();
		  DeskFlickrUI.GetInstance().UpdateToolBarButtons();
		  if (_isconflictmode) DeskFlickrUI.GetInstance().OnConflictButtonClicked(null, null);
		  else DeskFlickrUI.GetInstance().UpdatePhotos(_selectedphotos); 
		}
	}