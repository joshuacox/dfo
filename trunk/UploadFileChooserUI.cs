
using System;
using System.Threading;
using System.IO;
using Gtk;
using Glade;

	public class UploadFileChooserUI
	{
    [Glade.Widget]
    FileChooserDialog filechooserdialog1;
    
    [Glade.Widget]
    Image image6;
    
    [Glade.Widget]
    Button button10;
    
    [Glade.Widget]
    Button button11;
    
    [Glade.Widget]
    EventBox eventbox7;
    
    [Glade.Widget]
    EventBox eventbox8;
    
    [Glade.Widget]
    EventBox eventbox9;
    
    [Glade.Widget]
    Label label16;
    
    [Glade.Widget]
    ProgressBar progressbar3;
    
    // private static UploadFileChooserUI uploader;
    private ThreadStart _job;
    private Thread _previewthread;
    private Gdk.Pixbuf _buf;
    private Thread _processfilesthread;
    
    //private static Gdk.Color bgcolor = new Gdk.Color(0xA8, 0xA8, 0x94);
    private static Gdk.Color bgcolor = new Gdk.Color(0xFF, 0xFF, 0xFF);
    
		private UploadFileChooserUI()
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "filechooserdialog1", null);
		  gxml.Autoconnect (this);
		  _job = new ThreadStart(ProcessThumbnail);
		  _previewthread = new Thread(_job);
		  
		  label16.WidthRequest = eventbox7.WidthRequest;
		  
		  eventbox7.ModifyBg(Gtk.StateType.Normal, bgcolor);
		  eventbox8.ModifyBg(Gtk.StateType.Normal, bgcolor);
		  eventbox9.ModifyBg(Gtk.StateType.Normal, bgcolor);
		  
		  filechooserdialog1.Title = "Select files to upload";
		  filechooserdialog1.SetIconFromFile(DeskFlickrUI.ICON_PATH);
		  filechooserdialog1.SetFilename(PersistentInformation.GetInstance().UploadFilename);
      filechooserdialog1.SelectMultiple = true;
      
      FileFilter imagefilter = new FileFilter();
      imagefilter.AddMimeType("image/jpeg");
      imagefilter.AddMimeType("image/png");
      filechooserdialog1.Filter = imagefilter;
      
      filechooserdialog1.SelectionChanged += new EventHandler(OnFileSelectedChanged);
      filechooserdialog1.FileActivated += new EventHandler(OnOpenButtonClicked);
		  button10.Clicked += new EventHandler(OnOpenButtonClicked);
		  button11.Clicked += new EventHandler(OnCancelButtonClicked);
		  DeskFlickrUI.GetInstance().SetUploadWindow(false);
      filechooserdialog1.ShowAll();
		}
		
		public static void FireUp() {
      new UploadFileChooserUI();
		}
		
		private void OnFileSelectedChanged(object sender, EventArgs args) {
		  if (_processfilesthread != null && _processfilesthread.IsAlive) return;
		  _previewthread.Abort();
		  _previewthread = new Thread(_job);
		  _previewthread.Start();
		}
		
		private string GetInfo(string filename) {
		  if (System.IO.Directory.Exists(filename)) {
		    return "<span font_desc='Times Bold 10'>Directory</span>";
		  }
		  FileInfo finfo = new FileInfo(filename);
		  if (!finfo.Exists) {
		    return "<span font_desc='Times Bold 10'>Unable to process file " + finfo.Name + "</span>";
		  }
		  System.Text.StringBuilder strb = new System.Text.StringBuilder(); 
		  strb.AppendLine("<span font_desc='Times Bold 10'>" + finfo.Name + "</span>");
		  string sizestr;
		  long size = finfo.Length;
		  if (size < 1024) {
		    sizestr = size + " bytes";
		  } else if (size < 1024*1024) {
		    sizestr = String.Format("{0:###.##} KB", ((float) size)/1024);
		  } else {
		    sizestr = String.Format("{0:###.##} MB", ((float) size)/(1024*1024));
		  }
		  strb.AppendLine("<span font_desc='Times 10'>" + sizestr + "</span>");
		  return strb.ToString();
		}
		
		// This method is run through a thread.
		private void ProcessThumbnail() {
		  Gtk.Application.Invoke(delegate{
		    progressbar3.Text = "Processing...";
		    //progressbar3.PulseStep = 0.3;
		    //progressbar3.Pulse();
		  });
		  Thread.Sleep(700); // wait for 0.7 seconds. This way, if the user gets
		  // jumpy and selects a lot of files quickly, the thread would be
		  // interrupted before it reaches the processing of file stage, hence
		  // saving processing power and RAM consumption.
		  string[] filenames = filechooserdialog1.Filenames;
		  if (filenames == null || filenames.Length == 0) return;
		  string filename = filenames[filenames.Length - 1];
		  
		  if (System.IO.Directory.Exists(filename)) {
		    _buf = DeskFlickrUI.GetInstance().GetDFOThumbnail();
		  }
		  else {
  		  try {
  		    // Scalesimple creates a new pixbuf buffer, with the scaled
  		    // version of the image. If we do, _buf = _buf.ScaleSimple, the
  		    // originally loaded buffer remains referenced, and hence,
  		    // causes memory leak. Instead, we use a different buffer to load
  		    // file, and then dispose it once the image is scaled.
  		    Gdk.Pixbuf original = new Gdk.Pixbuf(filename);
    		  _buf = original.ScaleSimple(150, 150, Gdk.InterpType.Bilinear);
    		  original.Dispose();
    		} catch (GLib.GException) {
    		  _buf = DeskFlickrUI.GetInstance().GetDFOThumbnail();
    		}
  		}
		  Gtk.Application.Invoke (delegate {
  		  image6.Pixbuf = _buf;
  		  label16.Markup = GetInfo(filename);
  		  progressbar3.Text = "";
		  });
		}
		
		// To be run in a thread by OnOpenButtonClicked method.
		private void ProcessFiles() {
		  string[] filenames = null;
		  filenames = filechooserdialog1.Filenames;
		  if (filenames == null) return;
      PersistentInformation.GetInstance().UploadFilename = filenames[0];
      Gdk.Pixbuf thumbnail;
      Gdk.Pixbuf smallimage;
      Gdk.Pixbuf sqimage;
      Gtk.Application.Invoke(delegate{
        progressbar3.Adjustment.Lower = 0;
        progressbar3.Adjustment.Upper = filenames.Length;
        progressbar3.Adjustment.Value = 0;
        progressbar3.Text = "Processing files...";
        button10.Sensitive = false;
      });
      foreach (string filename in filenames) {
        Gtk.Application.Invoke(delegate{
          progressbar3.Adjustment.Value += 1;
        });
        try {
          Gdk.Pixbuf buf = new Gdk.Pixbuf(filename);
          thumbnail = buf.ScaleSimple(75, 75, Gdk.InterpType.Bilinear);
          smallimage = buf.ScaleSimple(240, 180, Gdk.InterpType.Bilinear);
          sqimage = buf.ScaleSimple(150, 150, Gdk.InterpType.Bilinear);
          buf.Dispose();
        } catch (GLib.GException) {
          continue;
          // Couldn't process the file.
        }
        Gtk.Application.Invoke(delegate{
          label16.Markup = GetInfo(filename);
          image6.Pixbuf = sqimage;
        });
        PersistentInformation.GetInstance().SetThumbnail(filename, thumbnail);
        thumbnail.Dispose();
        PersistentInformation.GetInstance().SetSmallImage(filename, smallimage);
        smallimage.Dispose();
        PersistentInformation.GetInstance().InsertEntryToUpload(filename);
      }
      Gtk.Application.Invoke(delegate{
        DeskFlickrUI.GetInstance().UpdateToolBarButtons();
        DeskFlickrUI.GetInstance().RefreshUploadPhotos();
        filechooserdialog1.Destroy();
        DeskFlickrUI.GetInstance().SetUploadWindow(true);
      });
		}
		
		private void OnOpenButtonClicked(object sender, EventArgs args) {
		  if (_processfilesthread != null && _processfilesthread.IsAlive) return; 
		  _previewthread.Abort();
      ThreadStart job = new ThreadStart(ProcessFiles);
      _processfilesthread = new Thread(job);
      _processfilesthread.Start();
		}
		
		private void OnCancelButtonClicked(object sender, EventArgs args) {
		  if (_processfilesthread != null) {
		    _processfilesthread.Abort();
		    DeskFlickrUI.GetInstance().UpdateToolBarButtons();
		    DeskFlickrUI.GetInstance().RefreshUploadPhotos();
		  }
		  filechooserdialog1.Destroy();
		  DeskFlickrUI.GetInstance().SetUploadWindow(true);
		}
	}