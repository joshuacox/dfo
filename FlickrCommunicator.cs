
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using FlickrNet;

	public class FlickrCommunicator
	{
	  private static string _apikey = "413051629a00e140b4f448fd22d715d2";
    private static string _secret = "9aa8b5eef280f665";
    private static int MAXTRIES = 3;
    private static int CODE_TIMEOUT = 9999;
    
    private static FlickrCommunicator comm = null;
    
    private bool _isConnected;
    private bool _isbusy;
    private Flickr flickrObj;
    private System.Net.WebClient webclient;
    
		private FlickrCommunicator()
		{
		  webclient = new System.Net.WebClient();
      _isConnected = false;
      _isbusy = false;
    }
		
		public static FlickrCommunicator GetInstance() {
		  if (comm == null) {
		    comm = new FlickrCommunicator();
		  }
		  return comm;
		}
		
    public void AttemptConnection() {
		  string token = PersistentInformation.GetInstance().Token;
		  if (token.Equals("")) {
  		  try {
  		    FirstTimeAuthentication auth = new FirstTimeAuthentication();
          auth.ConnectToFlickr();
          _isConnected = true;
  		  } catch (FlickrNet.FlickrException e) {
  		    Console.WriteLine(e.Code + " : " + e.Verbose);
  		    _isConnected = false;
  		  }
		  }
      else {
        TestLogin();
      }
      Gtk.Application.Invoke (delegate {
        if (_isConnected) {
          DeskFlickrUI.GetInstance().SetStatusLabel("Login Successful.");
        } else {
          DeskFlickrUI.GetInstance().SetStatusLabel("Unable to connect.");
        }
      });
      UpdateUIAboutConnection();
		}
		
		public bool IsConnected {
		  get {
		    return _isConnected;
		  }
		}
		
		public void Disconnect() {
		  if (_isbusy) return;
		  _isConnected = false;
		  UpdateUIAboutConnection();
		}
		
		private void UpdateUIAboutConnection() {
		  Gtk.Application.Invoke (delegate {
		    int status = 0;
		    if (_isConnected) status = 1;
		    if (_isbusy) status = 2;
        DeskFlickrUI.GetInstance().SetIsConnected(status);
      });
		}
		
		private void PrintException(FlickrNet.FlickrException e) {
		  Console.WriteLine(e.Code + " : " + e.Verbose);
		}
		
		public void TestLogin() {
		  string token = PersistentInformation.GetInstance().Token;
		  try {
		    flickrObj = new Flickr(_apikey, _secret, token);
		    flickrObj.TestLogin();
        _isConnected = true;
		  } catch (FlickrNet.FlickrException e) {
		    PrintException(e);
		    _isConnected = false;
		    return;
		  }
      ThreadStart job = new ThreadStart(RoutineCheck);
      Thread t = new Thread(job);
      t.Start();
		}
		
		public bool IsBusy {
		  get {
		    return _isbusy;
		  }
		}
		
		public void RoutineCheck() {
		  _isbusy = true;
      UpdateUIAboutConnection();
		  UpdateAlbums();
		  foreach (Album a in PersistentInformation.GetInstance().GetAlbums()) {
		    UpdatePhotosForAlbum(a);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().RefreshLeftTreeView();
		    });
		  }
		  _isbusy = false;
		  UpdateUIAboutConnection();
		}
		
		private FlickrNet.PhotoInfo SafelyGetInfo(string photoid) {
		  FlickrNet.PhotoInfo pInfo = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      pInfo = flickrObj.PhotosGetInfo(photoid);
		      return pInfo;
		    } catch(FlickrNet.FlickrException e) {
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) {
		        PrintException(e);
		        if (e.Code == CODE_TIMEOUT) _isConnected = false;
		        return null;
		      }
		      continue;
		    }
		  }
		  throw new Exception("FlickrCommunicator: GetInfo(photoid) unreachable code");
		}
		
		private Gdk.Pixbuf SafelyDownloadPhoto(string address) {
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
          System.IO.Stream stream = webclient.OpenRead(address);
          return new Gdk.Pixbuf(stream);
        } catch(Exception e) {
          if (i == MAXTRIES-1) {
            Console.WriteLine(e.Message);
            return null;
          }
          continue;
        }
      }
      throw new Exception("FlickrCommunicator: DownloadPhoto unreachable code");
		}
		
		private FlickrNet.Photoset[] SafelyGetAlbumList() {
      FlickrNet.Photoset[] sets = null;
      for (int i=0; i<MAXTRIES; i++) {
        try {
		      sets = flickrObj.PhotosetsGetList().PhotosetCollection;
		      return sets;
		    } catch(FlickrNet.FlickrException e) {
		      // Maximum attempts over.
          if (i == MAXTRIES-1) {
            PrintException(e);
            if (e.Code == CODE_TIMEOUT) _isConnected = false;
            return null;
          }
		      continue;
		    }
		  }
		  throw new Exception("FlickrCommunicator: GetAlbumList unreachable code");
		}
		
		// To keep it simple, if any of the steps involved in retrieving
		// a photo fails, we'll assume that the entire sequence has failed,
		// and it should be retried from step 1.
		// This method only retrieves the photo from flickr, but doesn't store
		// it automatically in database.
		// The reason is that the APIs don't provide the last updated timestamp
		// information when you retrieve full information (a bug). So, instead
		// of leaving this field empty, its expected of the calling method to
		// fill it up, and then save it in database.
		public Photo RetrievePhoto(string photoid) {
		  if (!_isConnected) return null;
		  // Step 1: Retrieve photo information.
      FlickrNet.PhotoInfo pInfo = SafelyGetInfo(photoid);
      if (pInfo == null) {
        UpdateUIAboutConnection();
        return null;
      }
      
		  Photo photo = new Photo(photoid, pInfo.Title, pInfo.Description,
		                          pInfo.License, pInfo.Visibility.IsPublic,
		                          pInfo.Visibility.IsFriend, 
		                          pInfo.Visibility.IsFamily, 
		                          pInfo.Dates.raw_lastupdate.ToString());

      // Step 2: Retrieve Square Thumbnail.
		  Gdk.Pixbuf thumbnail = 
		      PersistentInformation.GetInstance().GetThumbnail(photoid);
		  if (thumbnail == null) {
		    // This determines what size is download for the image.
		    Gdk.Pixbuf buf = SafelyDownloadPhoto(pInfo.SquareThumbnailUrl);
		    if (buf == null) {
		      UpdateUIAboutConnection();
		      return null;
		    }
		    photo.Thumbnail = buf;
		  }
		  
		  // Step 3: Retrieve Small image.
		  Gdk.Pixbuf smallimage = 
		      PersistentInformation.GetInstance().GetSmallImage(photoid);
		  if (smallimage == null) {
		    Gdk.Pixbuf buf = SafelyDownloadPhoto(pInfo.SmallUrl);
		    if (buf == null) {
		      UpdateUIAboutConnection();
		      return null;
		    }
		    photo.SmallImage = buf;
		  }

		  ArrayList tags = new ArrayList();
		  foreach (FlickrNet.PhotoInfoTag tag in pInfo.Tags.TagCollection) {
        tags.Add(tag.TagText);
		  }
		  photo.Tags = tags;
		  return photo;
		}
		
		private void UpdateAlbums() {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Updating photo sets...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
      });
      
      // Step 1: Retrieve album list.
      FlickrNet.Photoset[] sets = SafelyGetAlbumList();
      if (sets == null) {
        UpdateUIAboutConnection();
        return;
      }
      
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(sets.Length);
		  });
		  
      ArrayList setids = new ArrayList();
      // Iterate through the sets, and retrieve their photos.
		  foreach (Photoset s in sets) {
		    setids.Add(s.PhotosetId);
		    Album album = new Album(s.PhotosetId, s.Title, 
		                            s.Description, s.PrimaryPhotoId);

		    Photo p = PersistentInformation.GetInstance().GetPhoto(s.PrimaryPhotoId);
		    if (p == null) p = RetrievePhoto(s.PrimaryPhotoId);
		    if (p == null) {
		      UpdateUIAboutConnection();
		      return;
		    }
		    PersistentInformation.GetInstance().UpdateAlbum(album);
        PersistentInformation.GetInstance().UpdatePhoto(p);
        // Make sure not to overwrite any of user specified changes,
        // when doing updates, namely the isdirty=1 rows.
        PersistentInformation.GetInstance().AddPhotoToAlbum(p.Id, album.SetId);
        
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		  }

		  PersistentInformation.GetInstance().OrderedSetsList =
		      Utils.GetDelimitedString(setids, ",");
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Done");
		    DeskFlickrUI.GetInstance().SetValueProgressBar(0);
		    DeskFlickrUI.GetInstance().SetProgressBarText("Photo Sets Updated");
		    DeskFlickrUI.GetInstance().PopulateAlbums();
		    DeskFlickrUI.GetInstance().ShowAllInWindow();
		  });
		}
		
		private FlickrNet.Photo[] SafelyGetPhotos(Album album) {
		  FlickrNet.Photo[] photos = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      photos = flickrObj.PhotosetsGetPhotos(album.SetId);
		      return photos;
		    } catch(FlickrNet.FlickrException e) {
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) {
		        PrintException(e);
            if (e.Code == CODE_TIMEOUT) _isConnected = false;
            return null;
		      }
		      continue;
		    }
		  }
		  throw new Exception("FlickrCommunicator: SafelyGetPhotos: unreachable code");
		}
		
		private void UpdatePhotosForAlbum(Album album) {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel(
		        String.Format("Retrieving photos for set: {0}", album.Title));
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		  });
		  // Step 1: Get list of photos.
		  FlickrNet.Photo[] photos = SafelyGetPhotos(album);
		  if (photos == null) {
		    UpdateUIAboutConnection();
		    return;
		  }
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Length);
		  });
		  
		  // Step 2: Ensure we have the latest photos.
		  foreach (FlickrNet.Photo p in photos) {
		    if (PersistentInformation.GetInstance().
		        HasLatestPhoto(p.PhotoId, p.lastupdate_raw)) {
		      continue;
		    }
		    Photo photo = RetrievePhoto(p.PhotoId);
		    PersistentInformation.GetInstance().UpdatePhoto(photo);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		  }
		  
		  // Step 3: Link the photos to the set, in the database.
		  PersistentInformation.GetInstance().DeleteCleanPhotoEntriesFromAlbum(album.SetId);
		  foreach (FlickrNet.Photo p in photos) {
		    PersistentInformation.GetInstance().AddPhotoToAlbum(p.PhotoId, album.SetId);
		  }
		}
	}
