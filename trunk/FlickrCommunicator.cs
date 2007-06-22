
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
        DeskFlickrUI.GetInstance().SetSensitivityConnectionButtons(!_isbusy);
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
		}
		
		public bool IsBusy {
		  get {
		    return _isbusy;
		  }
		}
		
		/*
		 * This method is the one which takes care of all the sync operations,
		 * uploading and downloading of photos. Call it the master method,
		 * everything else follows.
		 */
		public void RoutineCheck() {
		  _isbusy = true;
		  try {
        UpdateUIAboutConnection();
        UpdateStream();
  		  UpdateAlbums();
  		  foreach (Album a in PersistentInformation.GetInstance().GetAlbums()) {
  		    UpdatePhotosForAlbum(a);
  		    Gtk.Application.Invoke (delegate {
  		      DeskFlickrUI.GetInstance().RefreshLeftTreeView();
  		    });
  		  }
  		  // Sync photos at the end. It takes time for the changes done to server
  		  // to propagate. If we keep these sync methods before updates, then
  		  // the changes would be synced to server, however, the server would
  		  // respond back with the old ones to update methods. So, the application
  		  // would end up removing the changes, and only show them again at 
  		  // the next update.
  		  SyncDirtyPhotosToServer();
  		  SyncDirtyAlbumsToServer();
  		  CheckPhotosToDownload();
  		  CheckPhotosToUpload();
  		  UpdateUploadStatus();
  		} catch (Exception e) {
  		  Console.WriteLine(e.StackTrace);
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
		
		private FlickrNet.Sizes SafelyGetSizes(string photoid) {
		  FlickrNet.Sizes sizes = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      sizes = flickrObj.PhotosGetSizes(photoid);
		      return sizes;
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
		  throw new Exception("FlickrCommunicator: GetSizes(photoid) unreachable code");
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
		  
		  // Step 4: Retrieve tags. If no tags, then just return the photo.
		  if (pInfo.Tags.TagCollection == null) return photo;
		  // Otherwise, populate the tags, and then return the photo.
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
      // Iterate through the sets, and retrieve their primary photos.
		  foreach (Photoset s in sets) {
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		    setids.Add(s.PhotosetId);
		    // Skip the checkings, if the album is dirty.
		    if (PersistentInformation.GetInstance().IsAlbumDirty(s.PhotosetId)) 
		        continue;
		    
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
		  }
		  
		  // Now remove the albums which are no longer present on the server.
		  ArrayList cleanalbums = 
		      PersistentInformation.GetInstance().GetDirtyAlbums(false);
      foreach (Album album in cleanalbums) {
        if (!setids.Contains(album.SetId)) {
          PersistentInformation.GetInstance().DeleteAlbum(album.SetId);
          PersistentInformation.GetInstance().DeleteAllPhotosFromAlbum(album.SetId);
        }
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
		      photos = flickrObj.PhotosetsGetPhotos(
		                    album.SetId, PhotoSearchExtras.LastUpdated);
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
		  // Don't update photos if the album is dirty, we need to flush our
		  // changes first.
		  if (PersistentInformation.GetInstance().IsAlbumDirty(album.SetId)) {
		    Console.WriteLine("Album is dirty: " + album.Title);
		    return;
		  }
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel(
		        String.Format("Retrieving photos for set: {0}", album.Title));
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		  });
		  // Step 1: Get list of photos.
		  FlickrNet.PhotoCollection photos = SafelyGetPhotos(album);
		  if (photos == null) {
		    UpdateUIAboutConnection();
		    return;
		  }
		  Console.WriteLine("Got photos for album: " + album.Title + " num: " + photos.Length);
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Length);
		  });
		  
		  // Step 2: Ensure we have the latest photos.
		  UpdatePhotos(photos);
		  
		  // Step 3: Link the photos to the set, in the database.
		  PersistentInformation.GetInstance().DeleteAllPhotosFromAlbum(album.SetId);
		  foreach (FlickrNet.Photo p in photos) {
		    PersistentInformation.GetInstance().AddPhotoToAlbum(p.PhotoId, album.SetId);
		  }
		}
		
		private void UpdatePhotos(FlickrNet.PhotoCollection photos) {
		  foreach (FlickrNet.Photo p in photos) {
		  	Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		    if (PersistentInformation.GetInstance().
		          HasLatestPhoto(p.PhotoId, p.lastupdate_raw)) {
		      continue;
		    }
		    Photo photo = RetrievePhoto(p.PhotoId);
		    PersistentInformation.GetInstance().UpdatePhoto(photo);
		  }
		}
		
		private void UpdateStream() {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Updating photo stream...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
      });
      
		  FlickrNet.PhotoSearchOptions options = new PhotoSearchOptions();
		  options.UserId = "me";
		  options.Extras = PhotoSearchExtras.LastUpdated;
		  options.PerPage = 500;
		  options.Page = 1;
		  Photos photos = flickrObj.PhotosSearch(options);
		  Console.WriteLine("Got photos on page 1: " + photos.PhotoCollection.Count);
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar((int) photos.TotalPhotos);
		  });
		  UpdatePhotos(photos.PhotoCollection);
		  for (int curpage=2; curpage <= photos.TotalPages; curpage++) {
		    options.Page = curpage;
		    photos = flickrObj.PhotosSearch(options);
		    Console.WriteLine("Got photos on page " + curpage + " : " + photos.PhotoCollection.Count);
		    UpdatePhotos(photos.PhotoCollection);
		  }
		}
		
		private void UpdateUploadStatus() {
		  FlickrNet.UserStatus userstatus = flickrObj.PeopleGetUploadStatus();
		  Gtk.Application.Invoke(delegate {
		    DeskFlickrUI.GetInstance().SetUploadStatus(
		        userstatus.BandwidthMax, userstatus.BandwidthUsed);
		  });
		}
		
		private void SyncDirtyPhotosToServer() {
		  ArrayList photos = PersistentInformation.GetInstance().GetDirtyPhotos();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Syncing photos to server...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Count);
      });
		  foreach (Photo photo in photos) {
		    Console.WriteLine("syncing photo: " + photo.Title);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		    Photo serverphoto = RetrievePhoto(photo.Id);
		    bool ismodified = !serverphoto.LastUpdate.Equals(photo.LastUpdate);
		    if (ismodified) {
		      Console.WriteLine("Is modified");
          DeskFlickrUI.GetInstance().AddServerPhoto(serverphoto);
		      continue;
		    }
		    // Sync meta information.
		    bool ismetachanged = false;
		    if (!photo.Title.Equals(serverphoto.Title)) ismetachanged = true;
		    if (!photo.Description.Equals(serverphoto.Description)) ismetachanged = true;
		    if (ismetachanged) {
		      flickrObj.PhotosSetMeta(photo.Id, photo.Title, photo.Description);
		    }
		    // Sync Permissions.
		    bool isvischanged = false;
		    if (photo.IsPublic != serverphoto.IsPublic) isvischanged = true;
		    if (photo.IsFriend != serverphoto.IsFriend) isvischanged = true;
		    if (photo.IsFamily != serverphoto.IsFamily) isvischanged = true;
		    if (isvischanged) {
		      // TODO: Need to add ways to set the comment and add meta permissions.
		      flickrObj.PhotosSetPerms(photo.Id, photo.IsPublic, photo.IsFriend,
		                            photo.IsFamily, PermissionComment.Everybody, 
		                            PermissionAddMeta.Everybody);
		    }
		    // Sync tags as well.
		    bool istagschanged = !photo.IsSameTags(serverphoto.Tags);
		    if (istagschanged) {
		      flickrObj.PhotosSetTags(photo.Id, photo.TagString);
		    }
		    // Photo has been synced, now remove the dirty bit.
		    PersistentInformation.GetInstance().SetPhotoDirty(photo.Id, false);
		  }
		}
		
		private void SyncDirtyAlbumsToServer() {
		  ArrayList albums = PersistentInformation.GetInstance().GetDirtyAlbums(true);
		  if (albums.Count == 0) return;
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Syncing sets to server...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(albums.Count);
      });
      foreach (Album album  in albums) {
        Console.WriteLine("Syncing album: " + album.Title);
      	Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
        flickrObj.PhotosetsEditMeta(album.SetId, album.Title, album.Desc);
        // Sync the primary photo id of the set.
        ArrayList photoids = 
            PersistentInformation.GetInstance().GetPhotoIdsForAlbum(album.SetId);
        flickrObj.PhotosetsEditPhotos(album.SetId, album.PrimaryPhotoid,
                                      Utils.GetDelimitedString(photoids, ","));
        PersistentInformation.GetInstance().SetAlbumDirty(album.SetId, false);
      }
		}
		
		private void CheckPhotosToDownload() {
		  ArrayList entries = PersistentInformation.GetInstance().GetEntriesToDownload();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Downloading photos...");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(entries.Count);
		  });
		  foreach (PersistentInformation.DownloadEntry entry in entries) {
		  	FlickrNet.Sizes photosizes = SafelyGetSizes(entry.photoid);
		  	if (photosizes == null) continue;
		  	Photo p = PersistentInformation.GetInstance().GetPhoto(entry.photoid);
      	Gtk.Application.Invoke (delegate {
      	  string label = String.Format(
      	      "Downloading {0}... Saving to {1}", p.Title, entry.foldername);
      	  DeskFlickrUI.GetInstance().SetStatusLabel(label);
		    });

		    Hashtable table = new Hashtable();
		    foreach (FlickrNet.Size size in photosizes.SizeCollection) {
		      table.Add(size.Label.ToLower(), size.Source);
		    }
		    string sourceurl;
		    if (table.ContainsKey("original")) sourceurl = (string) table["original"];
		    else if (table.ContainsKey("large")) sourceurl = (string) table["large"];
		    else if (table.ContainsKey("medium")) sourceurl = (string) table["medium"];
		    else {
		      PersistentInformation.GetInstance().DeleteEntryFromDownload(entry);
		      continue;
		    }
		    Console.WriteLine("Downloading: " + sourceurl);
		    string safetitle = p.Title.Replace("/", "");
		    string filename = String.Format(
		          "{0}/{1}_{2}.jpg", entry.foldername, safetitle, p.Id);
        Utils.IfExistsDeleteFile(filename);
        try {
		      webclient.DownloadFile(sourceurl, filename);
		    } catch (Exception e) {
		      Console.WriteLine(e.Message);
		      continue;
		    }
      	Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		    PersistentInformation.GetInstance().DeleteEntryFromDownload(entry);
		  }
		}
		
		private void CheckPhotosToUpload() {
		  ArrayList filenames = PersistentInformation.GetInstance().GetEntriesToUpload();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Uploading photos...");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(filenames.Count);
		  });
		  foreach (string filename in filenames) {
		    Console.WriteLine("Uploading file: " + filename);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().SetStatusLabel("Uploading " + filename);
		    });
		    // Check if the file exists.
		    System.IO.FileInfo finfo = new System.IO.FileInfo(filename);
		    if (!finfo.Exists) continue;

		    // Upload the photo now.
		    //int lastindexofbackslash = filename.LastIndexOf("/");
		    //int indexofdot = filename.IndexOf(".", lastindexofbackslash);
		    //string title = filename.Substring(lastindexofbackslash, 
		    //                                  lastindexofbackslash - indexofdot);
		    // Set the title to photo name. Set the photo to private mode for now.
		    // Add a tag "dfo" to uploaded photo.
		    string photoid = flickrObj.UploadPicture(
		        filename, finfo.Name, "Uploaded through Flickr Desktop Organizer",
		        "dfo", false, false, false);
		    if (photoid == null) continue;
		    
		    // The photo has been successfully uploaded.
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		    PersistentInformation.GetInstance().DeleteEntryFromUpload(filename);
		    // Try if we can retrieve the photo information, this could be a bit
		    // to early for the information to be spread out to the server
		    // clusters. Keep our fingers crossed!
		    Photo photo = RetrievePhoto(photoid);
		    if (photo == null) continue;
		    PersistentInformation.GetInstance().UpdatePhoto(photo);
		  }
		}
	}
