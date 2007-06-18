
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
    Entry entry2;
    
    [Glade.Widget]
    ComboBox combobox1;
    
    [Glade.Widget]
    ComboBox combobox2;
    
    [Glade.Widget]
    IconView iconview1;
    
    [Glade.Widget]
    TextView textview3;
    
    [Glade.Widget]
    Button button3;
    
    [Glade.Widget]
    Button button4;
    
    [Glade.Widget]
    CheckButton checkbutton1;
    
    [Glade.Widget]
    Button button5;
    
    // Save and Close
    [Glade.Widget]
    Button button6;
    
    [Glade.Widget]
    Image image3;
    
    private ArrayList tags;
    private ArrayList selectedphotos;
    private int curphotoindex;
    
    // I haven't been able to figure out a way so that only the user
    // selection of combobox elements, would trigger the OnPrivacyChanged
    // and OnLicenseChanged methods. So, I'll go with a hack, by passing
    // in a boolean variable, which can tell these methods, when to not
    // heed to the changes done to the box.
    private bool ignorechangedevent;
    
		public PhotoEditorUI(ArrayList selectedphotos)
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window2", null);
		  gxml.Autoconnect (this);
		  
		  window2.Title = "Edit information for " + selectedphotos.Count + " photos";
		  window2.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Information");
		  notebook1.NextPage();
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Tags");
		  notebook1.PrevPage();
		  
		  table1.SetColSpacing(0, 50);
		  
		  label6.Text = "Edit";
		  // Set Labels
		  label5.Text = "Title:";
		  label4.Text = "Description:";
		  label3.Text = "Visibility:";
		  label2.Text = "License:";
		  
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
      entry2.Changed += new EventHandler(OnDescChanged);
      
      combobox1.Changed += new EventHandler(OnPrivacyChanged);
      combobox2.Changed += new EventHandler(OnLicenseChanged);
      
      textview3.Buffer.Changed += new EventHandler(OnTagsChanged);
      
      this.selectedphotos = selectedphotos;
      if (selectedphotos.Count == 1) {
        checkbutton1.Sensitive = false;
        ShowInformationForCurrentPhoto();
      } else {
        EmbedCommonInformation();
      }

		  window2.ShowAll();
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
		
		private void SetTagTreeView() {
		  ListStore tagstore = new ListStore(typeof(string));
		  tags = PersistentInformation.GetInstance().GetAllTags();
		  foreach(Tag t in PersistentInformation.GetInstance().GetAllTags()) {
		    tagstore.AppendValues(t.Name + "(" + t.NumberOfPics + ")");
		  }
		  iconview1.Model = tagstore;
		  iconview1.TextColumn = 0;
		  iconview1.ItemActivated += new ItemActivatedHandler(OnTagClicked);
		}
		
		private string GetPangoFormattedText(string text) {
		  //return String.Format("<span font_desc='FreeSerif 10'>{0}</span>", text);
		  return text;
		}
		
		private void EmbedCommonInformation() {
      // Work upon the selected photos here.
      Photo firstphoto = (Photo) selectedphotos[0];
		  string commonTitle = firstphoto.Title;
		  string commonDesc = firstphoto.Description;
		  int commonPrivacy = GetIndexOfPrivacyBox(firstphoto);
		  int commonLicense = GetIndexOfLicenseBox(firstphoto);
		  ArrayList tagschosen = firstphoto.Tags;
		  
		  foreach (Photo p in selectedphotos) {
		    if (!commonTitle.Equals(p.Title)) commonTitle = "";
		    if (!commonDesc.Equals(p.Description)) commonDesc = "";
		    if (commonPrivacy != GetIndexOfPrivacyBox(p)) commonPrivacy = 0;
		    if (commonLicense != GetIndexOfLicenseBox(p)) commonLicense = 0;
		    tagschosen = Utils.GetIntersection(tagschosen, p.Tags);
		  }
		  entry1.Text = commonTitle;
		  entry2.Text = commonDesc;
		  
		  ignorechangedevent = true;
		  {
		  combobox1.Active = commonPrivacy;
		  combobox2.Active = commonLicense;
		  textview3.Buffer.Text = Utils.GetDelimitedString(tagschosen, " ");
		  }
		  ignorechangedevent = false;
		  
		  image3.Sensitive = false;
		  image3.Pixbuf = null;
		  button3.Sensitive = false;
		  button4.Sensitive = false;
		  label7.Text = "";
		  label7.Sensitive = false;
		}
		
		private void ShowInformationForPhoto(Photo p) {
		  entry1.Text = p.Title;
		  entry2.Text = p.Description;
		  
		  ignorechangedevent = true;
		  {
		  combobox1.Active = GetIndexOfPrivacyBox(p);
		  combobox2.Active = GetIndexOfLicenseBox(p);
		  textview3.Buffer.Text = Utils.GetDelimitedString(p.Tags, " ");
		  }
		  ignorechangedevent = false;
		  
		  image3.Sensitive = true;
		  image3.Pixbuf = p.SmallImage;
		  label7.Sensitive = true;
      SetTitleTopRight(p.Title);
		}
		
		private void SetTitleTopRight(string title) {
		  label7.Markup = "<span weight='bold'>" + title + "</span>";
		}
		
		private void ShowInformationForCurrentPhoto() {
		  Photo p = (Photo) selectedphotos[curphotoindex];
		  ShowInformationForPhoto(p);
		}

		private void OnTagClicked(object o, ItemActivatedArgs args) {
		  Tag t = (Tag) tags[args.Path.Indices[0]];
		  ArrayList tagschosen;
		  
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    p.AddTag(t.Name);
		    tagschosen = p.Tags;
		  } else {
		    foreach (Photo p in selectedphotos) {
		      p.AddTag(t.Name);
		    }
		    tagschosen = ((Photo)selectedphotos[0]).Tags;
		    foreach (Photo p in selectedphotos) {
		      tagschosen = Utils.GetIntersection(tagschosen, p.Tags);
		    }
		  }
		  
		  ignorechangedevent = true;
		  textview3.Buffer.Text = Utils.GetDelimitedString(tagschosen, " ");
		  ignorechangedevent = false;
		}
		
		private void OnTagsChanged(object o, EventArgs args) {
		  ArrayList parsedtags = Utils.ParseTagsFromString(textview3.Buffer.Text);
		  if (parsedtags == null || ignorechangedevent) {
		    return;
		  }
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    p.Tags = parsedtags;
		  } else {
		    foreach (Photo p in selectedphotos) {
		      p.Tags = parsedtags;
		    }
		  }
		}
		
		private void OnTitleChanged(object o, EventArgs args) {
		  // if the entry is not being changed by the user, then 
		  // don't do the updates.
		  if (!entry1.IsFocus) return;
		  
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    p.Title = entry1.Text;
		  } else {
		    // Group of photos
		    foreach (Photo p in selectedphotos) {
		      p.Title = entry1.Text;
		    }
		  }
		  SetTitleTopRight(entry1.Text);
		}
		
		private void OnDescChanged(object o, EventArgs args) {
		  if (!entry2.IsFocus) return;

		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    // Per photo
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    p.Description = entry2.Text;
		  } else {
		    // Group of photos
		    foreach (Photo p in selectedphotos) {
		      p.Description = entry2.Text;
		    }
		  }
		}
		
		private void OnPrivacyChanged(object o, EventArgs args) {
      if (ignorechangedevent) return;
		  
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    SetPhotoPrivacyFromBox(ref p, combobox1.Active);
		  } else {
		    for (int i=0; i<selectedphotos.Count; i++) {
		      Photo p = (Photo) selectedphotos[i];
		      SetPhotoPrivacyFromBox(ref p, combobox1.Active);
		    }
		  }
		}
		
		private void OnLicenseChanged(object o, EventArgs args) {
		  if (ignorechangedevent) return;
		  
		  if (checkbutton1.Active || !checkbutton1.Sensitive) {
		    Photo p = (Photo) selectedphotos[curphotoindex];
		    SetPhotoLicenseFromBox(ref p, combobox2.Active);
		  } else {
		    for (int i=0; i<selectedphotos.Count; i++) {
		      Photo p = (Photo) selectedphotos[i];
		      SetPhotoLicenseFromBox(ref p, combobox2.Active);
		    }
		  }
    }
		
		private void OnPerPageCheckToggled(object o, EventArgs args) {
		  if (checkbutton1.Active) {
		    curphotoindex = 0;
		    button3.Sensitive = false; // Previous button
		    button4.Sensitive = true; // Next button
		    ShowInformationForCurrentPhoto();
		  } else {
		    EmbedCommonInformation();
		  }
		}
		
		private void OnNextButtonClick(object o, EventArgs args) {
		  curphotoindex += 1;
		  if (curphotoindex == selectedphotos.Count -1) {
		    button4.Sensitive = false;
		  }
		  if (curphotoindex == 1) {
		    button3.Sensitive = true;
		  }
		  ShowInformationForCurrentPhoto();
		}
		
		private void OnPrevButtonClick(object o, EventArgs args) {
		  curphotoindex -= 1;
		  if (curphotoindex == 0) {
		    button3.Sensitive = false;
		  }
		  if (curphotoindex == selectedphotos.Count - 2) {
		    button4.Sensitive = true;
		  }
		  ShowInformationForCurrentPhoto();
		}
		
		private void OnCancelButtonClick(object o, EventArgs args) {
		  window2.Destroy();
		}
		
		private void OnSaveButtonClick(object o, EventArgs args) {
		  foreach (Photo p in selectedphotos) {
        PersistentInformation.GetInstance().SavePhoto(p);
		  }
		  window2.Destroy();
		  DeskFlickrUI.GetInstance().UpdatePhotos();
		}
	}