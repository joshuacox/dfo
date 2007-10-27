
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
		
		public bool IsTokenPresent() {
		  string token = PersistentInformation.GetInstance().Token;
		  return !token.Equals("");
		}
		
    public void AttemptConnection() {
    	string token = PersistentInformation.GetInstance().Token;
		  try {
		    flickrObj = new Flickr(_apikey, _secret, token);
		    flickrObj.TestLogin();
        _isConnected = true;
		  } catch (FlickrNet.FlickrApiException e) {
		    PrintException(e);
		    _isConnected = false;
		    return;
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
		
		private void DelegateIncrementProgressBar() {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		  });
		}
		
		private void PrintException(FlickrNet.FlickrApiException e) {
		  Console.WriteLine(e.Code + " : " + e.Verbose);
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
  		  UpdateUploadStatus();
        UpdateStream();
        PersistentInformation.GetInstance().DeleteAllOriginalPhotos();

  		  UpdateAlbums();
  		  foreach (Album a in PersistentInformation.GetInstance().GetAlbums()) {
  		    if (PersistentInformation.GetInstance().IsAlbumNew(a.SetId)) continue;
  		    UpdatePhotosForAlbum(a);
  		    Gtk.Application.Invoke (delegate {
  		      if (DeskFlickrUI.GetInstance().IsAlbumTabSelected()) {
  		        DeskFlickrUI.GetInstance().RefreshLeftTreeView();
  		      }
  		    });
  		  }
  		  UpdatePools();
  		  UpdateBlogs();
  		  // Sync photos at the end. It takes time for the changes done to server
  		  // to propagate. If we keep these sync methods before updates, then
  		  // the changes would be synced to server, however, the server would
  		  // respond back with the old values to update methods. So, the application
  		  // would end up removing the changes, and only show them again at 
  		  // the next update.
  		  SyncDirtyPhotosToServer();
  		  SyncDeletedCommentsToServer();
  		  SyncDirtyCommentsToServer();
  		  
  		  SyncNewAlbumsToServer();
  		  SyncDirtyAlbumsToServer();
        Gtk.Application.Invoke (delegate {
          DeskFlickrUI.GetInstance().UpdateToolBarButtons();
        });
        
        SyncPhotosDeletedFromPools();
        SyncPhotosAddedToPools();
        SyncBlogPosts();
        
  		  CheckPhotosToDownload();
  		  CheckPhotosToUpload();
  		  // Flickr server never catches up with updates soon. So, we'll 
  		  // do all the album updates required by photo deletion on our side, 
  		  // and then just flush them to server. Hope they spread around by 
  		  // the next time this routine runs.
        CheckPhotosToDelete();
        Gtk.Application.Invoke (delegate {
          DeskFlickrUI.GetInstance().RefreshLeftTreeView();
        });
  		} catch (Exception e) {
  		  Console.WriteLine(e.Message);
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
		    } catch(FlickrNet.FlickrApiException e) {
		      if (e.Code == 1) return null; // Photo not found.
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) {
		        PrintException(e);
		        if (e.Code == CODE_TIMEOUT) {
		        _isConnected = false;
		        throw e;
		        }
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
		    } catch(FlickrNet.FlickrApiException e) {
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
		    } catch(FlickrNet.FlickrApiException e) {
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
		
		private FlickrNet.Blog[] SafelyGetBlogList() {
		  FlickrNet.Blog[] blogs = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      blogs = flickrObj.BlogGetList().BlogCollection;
		      return blogs;
		    } catch(FlickrNet.FlickrApiException e) {
		      if (i == MAXTRIES-1) {
            PrintException(e);
            if (e.Code == CODE_TIMEOUT) _isConnected = false;
            return null;
          }
		      continue;
		    }
		  }
		  throw new Exception("FlickrCommunicator: GetBlogList unreachable code");
		}
		
		private FlickrNet.Photoset SafelyCreateNewAlbum(Album album) {
		  FlickrNet.Photoset photoset;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      photoset = flickrObj.PhotosetsCreate(album.Title, 
		                                           album.Desc, album.PrimaryPhotoid);
		      return photoset;
		    } catch(FlickrNet.FlickrApiException e) {
          // Status quo, if can't create any new sets. Let the information
          // be there, so that we can retry.
          if (e.Code == 3) {
            Gtk.Application.Invoke (delegate {
              DeskFlickrUI.GetInstance().ShowMessageDialog(
                  "Can't create a new set. You've reached the maximum number"
                  + " of photosets limit.");
            });
            return null; // No need to retry.
          }
		      if (i == MAXTRIES-1) {
		        PrintException(e);
		        if (e.Code == CODE_TIMEOUT) _isConnected = false;
		        return null;
		      }
		    }
		    continue;
		  }
		  throw new Exception("FlickrCommunicator: CreateNewAlbum unreachable code");
		}
		
		private ArrayList SafelyGetComments(string photoid) {
		  FlickrNet.Comment[] flickrcomments;
		  for (int i=0; i<MAXTRIES; i++) {
  		  try {
  		    flickrcomments = flickrObj.PhotosCommentsGetList(photoid);
  		    ArrayList comments = new ArrayList(); 
  		    foreach (FlickrNet.Comment flickrcomment in flickrcomments) {
  		      Comment comment = new Comment(flickrcomment.CommentId, 
  		                                    flickrcomment.CommentHtml,
  		                                    flickrcomment.AuthorUserName);
  		      comments.Add(comment);
  		    }
  		    return comments;
  		  } catch (FlickrNet.FlickrApiException e) {
  		    if (i == MAXTRIES-1) {
  		      PrintException(e);
  		      if (e.Code == CODE_TIMEOUT) _isConnected = false;
  		      return null;
  		    }
  		  }
  		  continue;
		  }
		  throw new Exception("FlickrCommunicator: SafelyGetComments unreachable code");
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
      photo.DatePosted = pInfo.Dates.raw_posted.ToString();
      
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
		  
		  // Step 5: Retrieve comments.
		  if (pInfo.CommentsCount > 0) {
		    ArrayList comments = SafelyGetComments(photoid);
		    if (comments != null) {
		      Dictionary<string, Comment> localcommentsdic = new Dictionary<string, Comment>();
		      foreach (Comment comment in 
		          PersistentInformation.GetInstance().GetCommentsForPhoto(photoid)) {
		        localcommentsdic.Add(comment.CommentId, comment);
		      }
		      
		      foreach (Comment comment in comments) {
		        if (localcommentsdic.ContainsKey(comment.CommentId)) {
		          // Comment is present in db. But check if the comment html text
		          // is still the same, or has been changed online.
		          if (!comment.CommentHtml.Equals(localcommentsdic[comment.CommentId])) {
		            PersistentInformation.GetInstance().UpdateComment(photoid,
		                comment.CommentId, comment.CommentHtml, false);
		          }
		        } else { // comment is not present locally.
		          PersistentInformation.GetInstance().InsertComment(photoid, 
		              comment.CommentId, comment.CommentHtml, comment.UserName);
		        }
		        localcommentsdic.Remove(comment.CommentId);
		      }
		      // The comments left in localcommentsdic are the ones which have
		      // been deleted from the server.
		      foreach (string commentid in localcommentsdic.Keys) {
		        PersistentInformation.GetInstance().DeleteComment(photoid, commentid);
		      }
		    }
		  } else { // There're no comments online. So, wipe out any if present in db.
		    PersistentInformation.GetInstance().DeleteCleanComments(photoid);
		  }
		  
		  return photo;
		}
		
		private void UpdateBlogs() {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Updating blogs...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
      });
      FlickrNet.Blog[] blogs = SafelyGetBlogList();
      if (blogs == null) {
        UpdateUIAboutConnection();
        return;
      }
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(blogs.Length);
		  });
		  ArrayList blogids = new ArrayList();
		  foreach (PersistentInformation.Entry entry 
		      in PersistentInformation.GetInstance().GetAllBlogs()) {
		    blogids.Add(entry.entry1);
		  }
		  PersistentInformation.GetInstance().DeleteAllBlogs();
		  foreach (FlickrNet.Blog blog in blogs) {
		    if (blog.NeedsPassword == 0)
		        PersistentInformation.GetInstance().InsertBlog(blog.BlogId, blog.BlogName);
		    DelegateIncrementProgressBar();
		  }
      foreach (string blogid in blogids) {
        if (!PersistentInformation.GetInstance().HasBlog(blogid)) {
          PersistentInformation.GetInstance().DeleteAllEntriesFromBlog(blogid);
        }
      }
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
        DelegateIncrementProgressBar();
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
		    // Well the photo should just have been updated by UpdateStream()
		    // method, but heck! this is just one photo. Just leave this method
		    // here for now, may be useful in some "impossible" kinda situation.
        PersistentInformation.GetInstance().UpdatePhoto(p);
        
        // Make sure not to overwrite any of user specified changes,
        // when doing updates, namely the isdirty=1 rows.
        PersistentInformation.GetInstance().AddPhotoToAlbum(p.Id, album.SetId);
		  }
		  
		  // Now remove the albums which are no longer present on the server.
		  ArrayList allalbums = PersistentInformation.GetInstance().GetAlbums();
      foreach (Album album in allalbums) {
        // If the album is new, skip the deletion.
        if (!setids.Contains(album.SetId) 
            && !PersistentInformation.GetInstance().IsAlbumNew(album.SetId)) {

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
		    if (DeskFlickrUI.GetInstance().IsAlbumTabSelected()) 
		        DeskFlickrUI.GetInstance().RefreshLeftTreeView();
		    DeskFlickrUI.GetInstance().ShowAllInWindow();
		  });
		}
		
		private FlickrNet.Photo[] SafelyGetPhotos(Album album) {
		  FlickrNet.Photo[] photos = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      photos = flickrObj.PhotosetsGetPhotos(
		                    album.SetId, PhotoSearchExtras.LastUpdated).PhotoCollection;
		      return photos;
		    } catch(FlickrNet.FlickrApiException e) {
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
		
		public void SafelyDeletePhotoFromServer(string photoid) {
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      flickrObj.PhotosDelete(photoid);
		    } catch(FlickrNet.FlickrApiException e) {
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) {
		        PrintException(e);
            if (e.Code == CODE_TIMEOUT) _isConnected = false;
		      }
		      continue;
		    }
		  }
		}
		
		private void UpdatePhotosForAlbum(Album album) {
		  // Don't update photos if the album is dirty, we need to flush our
		  // changes first.
		  if (PersistentInformation.GetInstance().IsAlbumDirty(album.SetId)) {
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
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Length);
		  });
		  
		  // Step 2: Link the photos to the set, in the database.
		  PersistentInformation.GetInstance().DeleteAllPhotosFromAlbum(album.SetId);
		  foreach (FlickrNet.Photo p in photos) {
		    DelegateIncrementProgressBar();
		    if (!PersistentInformation.GetInstance().HasPhoto(p.PhotoId)) continue;
		    PersistentInformation.GetInstance().AddPhotoToAlbum(p.PhotoId, album.SetId);
		  }
		}
		
		private void UpdatePhotos(FlickrNet.PhotoCollection photos, 
		                          ref ArrayList serverphotoids) {
		  foreach (FlickrNet.Photo p in photos) {
        DelegateIncrementProgressBar();
		    if (!PersistentInformation.GetInstance().HasLatestPhoto(
		            p.PhotoId, p.lastupdate_raw)) {
		      Photo photo = RetrievePhoto(p.PhotoId);
		      if (photo == null) continue;
		      PersistentInformation.GetInstance().UpdatePhoto(photo);
		    }
		    serverphotoids.Add(p.PhotoId);
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
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar((int) photos.TotalPhotos);
		  });
		  ArrayList serverphotoids = new ArrayList();
		  UpdatePhotos(photos.PhotoCollection, ref serverphotoids);
		  for (int curpage=2; curpage <= photos.TotalPages; curpage++) {
		    options.Page = curpage;
		    photos = flickrObj.PhotosSearch(options);
		    UpdatePhotos(photos.PhotoCollection, ref serverphotoids);
		  }
		  // Delete the photos not present on server.
		  foreach (string photoid in 
		      PersistentInformation.GetInstance().GetAllPhotoIds()) {
		    // DeletePhoto method takes care of deleting the tags as well.
		    if (serverphotoids.Contains(photoid)) continue;
		    PersistentInformation.GetInstance().DeletePhoto(photoid);
		  }
		}
		
		private void UpdateUploadStatus() {
		  FlickrNet.UserStatus userstatus = flickrObj.PeopleGetUploadStatus();
		  PersistentInformation.GetInstance().UserId = userstatus.UserId;
		  PersistentInformation.GetInstance().UserName = userstatus.UserName;
		  Gtk.Application.Invoke(delegate {
		    DeskFlickrUI.GetInstance().SetUploadStatus(
		        userstatus.BandwidthMax, userstatus.BandwidthUsed);
		  });
		}
		
		private void UpdatePools() {
			Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Updating group pools...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
      });
		  // Refresh all the pool entries in pool table. If the user has left
		  // any group, it would be removed in this process.
		  FlickrNet.MemberGroupInfo[] pools = flickrObj.GroupPoolGetGroups();
		  PersistentInformation.GetInstance().DeleteAllPools();
		  foreach (FlickrNet.MemberGroupInfo pool in pools) {
		    PersistentInformation.GetInstance().InsertPool(pool.GroupId, pool.GroupName);
		  }

		  foreach (FlickrNet.MemberGroupInfo pool in pools) {
		    Gtk.Application.Invoke (delegate {
          DeskFlickrUI.GetInstance().SetStatusLabel("Updating pool " + pool.GroupName);
        });
		    ArrayList photoids = PersistentInformation.GetInstance().GetPhotoidsForPool(pool.GroupId);
        bool alreadysetprogresslimit = false;
		    int totalpages = 1;
		    // Retrieve all the photos posted by the user.
		    for (int curpage=1; curpage<=totalpages; curpage++) {
		      FlickrNet.Photos photos;
		      try {
		        string userid = PersistentInformation.GetInstance().UserId;
		        photos = flickrObj.GroupPoolGetPhotos(pool.GroupId, null, userid,
		            PhotoSearchExtras.None, 500, curpage);
		      } catch (Exception) {
		        // This exception is thrown when trying to parse totalpages, if
		        // no photos are returned. Its a bug with FlickrNet library.
		        totalpages = 0;
		        continue;
		      }
		      if (!alreadysetprogresslimit) {
		        Gtk.Application.Invoke (delegate {
		          DeskFlickrUI.GetInstance().SetLimitsProgressBar((int) photos.TotalPhotos);
		        });
		      }
		      totalpages = (int) photos.TotalPages;
		      foreach (FlickrNet.Photo photo in photos.PhotoCollection) {
		        if (photoids.Contains(photo.PhotoId)) photoids.Remove(photo.PhotoId);
		        else {
		          PersistentInformation.GetInstance().InsertPhotoToPool(photo.PhotoId, pool.GroupId);
		        }
		        DelegateIncrementProgressBar();
		      }
		    }
		    // These are the photos which have been removed at the server side.
		    // Also, the photos which haven't been yet added to the server, will
		    // be present in the list. Don't delete them.
		    foreach (string photoid in photoids) {
		      if (PersistentInformation.GetInstance()
		                               .IsPhotoAddedToPool(photoid, pool.GroupId)) {
		        continue;
		      }
		      PersistentInformation.GetInstance().DeletePhotoFromPool(photoid, pool.GroupId);
		    }
		  }
		}
		
		private void SyncPhotosAddedToPools() {
		  ArrayList entries = PersistentInformation.GetInstance().GetPhotosAddedToPools();
      Gtk.Application.Invoke (delegate {
        DeskFlickrUI.GetInstance().SetStatusLabel("Syncing photos added to pools...");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(entries.Count);
      });
		  foreach (PersistentInformation.Entry entry in entries) {
		    DelegateIncrementProgressBar();
		    string groupid = entry.entry1;
		    string photoid = entry.entry2;
		    Photo p = PersistentInformation.GetInstance().GetPhoto(photoid);
		    string pooltitle = PersistentInformation.GetInstance().GetPoolTitle(groupid);
		    string operation = String.Format(
		        "Adding '{0}' to group pool '{1}'.", p.Title, pooltitle);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().SetStatusLabel(
		          "Syncing photos added to pools... " + operation);
		    });
		    try {
		      flickrObj.GroupPoolAdd(photoid, groupid); //photoid, groupid
		    } catch (FlickrNet.FlickrApiException e) {
		      if (e.Code == 3) {
		        PersistentInformation.GetInstance().MarkPhotoAddedToPool(photoid, groupid, false);
		      }
		      else {
		        Gtk.Application.Invoke (delegate {
		          string message = operation + "\n Got response: " + e.Message;
		          DeskFlickrUI.GetInstance().ShowMessageDialog(message);
		        });
		      }
		      continue;
		    }
		    PersistentInformation.GetInstance().MarkPhotoAddedToPool(photoid, groupid, false);
		  }
		}
		
		private void SyncPhotosDeletedFromPools() {
		  ArrayList entries = PersistentInformation.GetInstance().GetPhotosDeletedFromPools();
      Gtk.Application.Invoke (delegate {
        DeskFlickrUI.GetInstance().SetStatusLabel("Syncing photos deleted from pools...");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(entries.Count);
      });
		  foreach (PersistentInformation.Entry entry in entries) {
		    DelegateIncrementProgressBar();
		    string groupid = entry.entry1;
		    string photoid = entry.entry2;
		    Photo p = PersistentInformation.GetInstance().GetPhoto(photoid);
		    string pooltitle = PersistentInformation.GetInstance().GetPoolTitle(groupid);
		    string operation = String.Format(
		        "Deleting '{0}' from group pool '{1}'.", p.Title, pooltitle);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().SetStatusLabel(
		          "Syncing photos deleted from pools... " + operation);
		    });
		    try {
		      flickrObj.GroupPoolRemove(photoid, groupid); //photoid, groupid
		    } catch (FlickrNet.FlickrApiException e) {
		      if (e.Code == 2) { // not present in the pool.
		        PersistentInformation.GetInstance().DeletePhotoFromPool(photoid, groupid);
		      }
		      else {
		        Gtk.Application.Invoke (delegate {
		          string message = operation + "\n Got response: " + e.Message;
		          DeskFlickrUI.GetInstance().ShowMessageDialog(message);
		        });
		      }
		      continue;
		    }
		    PersistentInformation.GetInstance().DeletePhotoFromPool(photoid, groupid);
		  }
		}
		
		private void SyncDirtyPhotosToServer() {
		  ArrayList photos = PersistentInformation.GetInstance().GetDirtyPhotos();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Syncing photos to server...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Count);
      });
		  foreach (Photo photo in photos) {
        DelegateIncrementProgressBar();
		    Photo serverphoto = RetrievePhoto(photo.Id);
		    bool ismodified = !serverphoto.LastUpdate.Equals(photo.LastUpdate);
		    if (ismodified) {
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
		
		private void SyncDirtyCommentsToServer() {
		  ArrayList comments = PersistentInformation.GetInstance().GetDirtyComments();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Syncing comments to server...");
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(comments.Count);
		  });
		  foreach (Comment comment in comments) {
		    if (comment.CommentId.IndexOf("new:") > -1) { // new comment
		      string newcommentid = flickrObj.PhotosCommentsAddComment(comment.PhotoId,
		          comment.CommentHtml);
		      if (newcommentid != null && !newcommentid.Trim().Equals("")) {
		        // Added the comment.
		        PersistentInformation.GetInstance().DeleteComment(comment.PhotoId, comment.CommentId);
		        PersistentInformation.GetInstance().InsertComment(comment.PhotoId, 
		            newcommentid, comment.CommentHtml, comment.UserName);
		      }
		    } else { // edited comment.
		      flickrObj.PhotosCommentsEditComment(comment.CommentId, comment.CommentHtml);
		      PersistentInformation.GetInstance().SetCommentDirty(comment.PhotoId, 
		          comment.CommentId, false);
		    }
	      DelegateIncrementProgressBar();
		  }
		}
		
		private void SyncDeletedCommentsToServer() {
		  ArrayList comments = PersistentInformation.GetInstance().GetDeletedComments();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Syncing deleted comments to server...");
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(comments.Count);
		  });
		  foreach (Comment comment in comments) {
		    flickrObj.PhotosCommentsDeleteComment(comment.CommentId);
		    PersistentInformation.GetInstance().DeleteComment(comment.PhotoId, 
		        comment.CommentId);
		    DelegateIncrementProgressBar();
		  }
		}
		
		private void SyncBlogPosts() {
		  ArrayList blogentries = PersistentInformation.GetInstance().GetAllBlogEntries();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Posting blog entries...");
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(blogentries.Count);
		  });
		  foreach (BlogEntry blogentry in blogentries) {
		    string desc = blogentry.Desc 
		        + "\nPosted using <a href='http://code.google.com/p/dfo/'>"
		        + "Desktop Flickr Organizer</a>.";
		    try {
		      if (flickrObj.BlogPostPhoto(blogentry.Blogid, blogentry.Photoid, 
		          blogentry.Title, desc)) {
		        PersistentInformation.GetInstance().DeleteEntryFromBlog(
		            blogentry.Blogid, blogentry.Photoid);
		      }
		    } catch (FlickrNet.FlickrApiException e) {
	        Gtk.Application.Invoke (delegate {
	          string operation = "Posting '" + blogentry.Title + "' to blog.";
	          string message = operation + "\n Got response: " + e.Message;
	          DeskFlickrUI.GetInstance().ShowMessageDialog(message);
	        });
		    }
		    DelegateIncrementProgressBar();
		  }
		}
		
		private void SyncNewAlbumsToServer() {
		  ArrayList albums = PersistentInformation.GetInstance().GetNewAlbums();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Creating sets on server...");
        DeskFlickrUI.GetInstance().SetProgressBarText("");
        DeskFlickrUI.GetInstance().SetLimitsProgressBar(albums.Count);
      });
      foreach (Album album in albums) {
        Gtk.Application.Invoke (delegate {
          DeskFlickrUI.GetInstance().SetStatusLabel(
              "Creating sets on server... " + album.Title);
        });
        DelegateIncrementProgressBar();
        FlickrNet.Photoset photoset = SafelyCreateNewAlbum(album);
        if (photoset == null) continue;
        ArrayList photoids = PersistentInformation.GetInstance()
            .GetPhotoIdsForAlbum(album.SetId);
        // Remove the old fake album entry.
        PersistentInformation.GetInstance().DeleteAlbum(album.SetId);
        PersistentInformation.GetInstance().DeleteAllPhotosFromAlbum(album.SetId);
        // Create and add a new one.
        Album newalbum = new Album(photoset.PhotosetId, album.Title,
                                   album.Desc, album.PrimaryPhotoid);
        PersistentInformation.GetInstance().InsertAlbum(newalbum);
        // Set the album dirty, in case the photosetseditphotos operation
        // fails, in this round of updates. Then, we'll retry the updates
        // next time, without they being overridden.
        PersistentInformation.GetInstance().SetAlbumDirty(newalbum.SetId, true);
        foreach (string photoid in photoids) {
          PersistentInformation.GetInstance().AddPhotoToAlbum(photoid, newalbum.SetId);
        }
        // Add the photos to the new album.
        flickrObj.PhotosetsEditPhotos(newalbum.SetId, newalbum.PrimaryPhotoid,
                                      Utils.GetDelimitedString(photoids, ","));
        PersistentInformation.GetInstance().SetAlbumDirty(newalbum.SetId, false);
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
        DelegateIncrementProgressBar();
        flickrObj.PhotosetsEditMeta(album.SetId, album.Title, album.Desc);
        // Sync the primary photo id of the set.
        ArrayList photoids = 
            PersistentInformation.GetInstance().GetPhotoIdsForAlbum(album.SetId);
        // If no photos inside set, delete the set.
        if (photoids.Count == 0) {
          flickrObj.PhotosetsDelete(album.SetId);
          PersistentInformation.GetInstance().DeleteAlbum(album.SetId);
        } else {
          flickrObj.PhotosetsEditPhotos(album.SetId, album.PrimaryPhotoid,
                                        Utils.GetDelimitedString(photoids, ","));
          PersistentInformation.GetInstance().SetAlbumDirty(album.SetId, false);
        }
      }
		}
		
		private void CheckPhotosToDownload() {
		  ArrayList entries = PersistentInformation.GetInstance().GetEntriesToDownload();
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Downloading photos...");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(entries.Count);
		  });
		  foreach (PersistentInformation.Entry entry in entries) {
				string photoid = entry.entry1;
				string foldername = entry.entry2;
		  	FlickrNet.Sizes photosizes = SafelyGetSizes(photoid);
		  	if (photosizes == null) continue;
		  	Photo p = PersistentInformation.GetInstance().GetPhoto(photoid);
      	Gtk.Application.Invoke (delegate {
      	  string label = String.Format(
      	      "Downloading {0}... Saving to {1}", p.Title, foldername);
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
		      PersistentInformation.GetInstance().DeleteEntryFromDownload(photoid);
		      continue;
		    }
		    string safetitle = p.Title.Replace("/", "");
		    string filename = String.Format(
		          "{0}/{1}_{2}.jpg", foldername, safetitle, p.Id);
        Utils.IfExistsDeleteFile(filename);
        try {
		      webclient.DownloadFile(sourceurl, filename);
		    } catch (Exception e) {
		      Console.WriteLine(e.Message);
		      continue;
		    }
        DelegateIncrementProgressBar();
		    PersistentInformation.GetInstance().DeleteEntryFromDownload(photoid);
		  }
		}
		
		// Tried to use OnUploadProgress event handler provided, but it proved
		// to be no good use. The bytes uploaded would reach the file size in
		// no time, but then it would take long to finish the upload. Maybe its
		// practically just tracking the bytes "read", rather than "uploaded".
		private void CheckPhotosToUpload() {
		  ArrayList photos = PersistentInformation.GetInstance().GetPhotosToUpload();
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Uploading photos...");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Count);
		  });
		  
		  foreach (Photo photo in photos) {
		    // Check if the file exists.
		    string filename = photo.Id;
		    System.IO.FileInfo finfo = new System.IO.FileInfo(filename);
		    if (!finfo.Exists) continue;

		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().SetStatusLabel(
		          String.Format("Uploading file {0}", filename));
		    });

		    // Upload the photo now.
		    // Set the title to photo name. Set the photo to private mode for now.
		    // Add a tag "dfo" to uploaded photo.
		    string newphotoid = flickrObj.UploadPicture(
		        filename, photo.Title, photo.Description,
		        Utils.GetDelimitedString(photo.Tags, ","),
		        (photo.IsPublic == 1), (photo.IsFamily == 1),
		        (photo.IsFriend == 1));
		    if (newphotoid == null) continue;
		    
		    // The photo has been successfully uploaded.
        DelegateIncrementProgressBar();
		    PersistentInformation.GetInstance().DeleteThumbnail(photo.Id);
		    PersistentInformation.GetInstance().DeleteSmallImage(photo.Id);
		    PersistentInformation.GetInstance().DeleteEntryFromUpload(filename);
		    // Try if we can retrieve the photo information, this could be a bit
		    // to early for the information to be spread out to the server
		    // clusters. Keep our fingers crossed!
		    Photo p = RetrievePhoto(newphotoid);
		    if (p == null) continue;
		    PersistentInformation.GetInstance().UpdatePhoto(p);
		  }
		}
		
		private void CheckPhotosToDelete() {
		  ArrayList photoids = PersistentInformation.GetInstance().GetPhotoIdsDeleted();
			Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Deleting photos...");
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photoids.Count);
		  });
		  foreach (String photoid in photoids) {
		    SafelyDeletePhotoFromServer(photoid);
		    PersistentInformation.GetInstance().DeletePhoto(photoid);
        DelegateIncrementProgressBar();
		  }
		}
	}