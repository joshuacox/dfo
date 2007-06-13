
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
    
    private static FlickrCommunicator comm = null;
    
    private Flickr flickrObj;
    private System.Net.WebClient webclient;
    
		private FlickrCommunicator()
		{
		  webclient = new System.Net.WebClient();
		  
		  string token = PersistentInformation.GetInstance().Token;
		  if (token.Equals("")) {
  		  try {
  		    FirstTimeAuthentication auth = new FirstTimeAuthentication();
          auth.ConnectToFlickr();
  		  } catch (FlickrNet.FlickrException e) {
  		    Console.WriteLine(e.Code + " : " + e.Verbose);
  		  }
		  }
      else {
        TestLogin();
      }
		}
		
		public static FlickrCommunicator GetInstance() {
		  if (comm == null) {
		    comm = new FlickrCommunicator();
		  }
		  return comm;
		}
		
		public void TestLogin() {
		  string token = PersistentInformation.GetInstance().Token;
		  try {
		    flickrObj = new Flickr(_apikey, _secret, token);
		    flickrObj.TestLogin();
		    Console.WriteLine("tested login");
		    DeskFlickrUI.GetInstance().SetStatusLabel("Login Successful.");
		    ThreadStart job = new ThreadStart(RoutineCheck);
		    Thread t = new Thread(job);
		    t.Start();
		    // Interestingly, using the above thread actually makes the
		    // application unresponsive. So, directly embed the function.
		    // RoutineCheck();
		    // DeskFlickrUI.GetInstance().PopulateAlbumTreeView(GetAlbums());
		  } catch (FlickrNet.FlickrException e) {
		    Console.WriteLine(e.Code + " : " + e.Verbose);
		  }
		}
		
		public void RoutineCheck() {
		  UpdateAlbums();
		  foreach (Album a in PersistentInformation.GetInstance().GetAlbums()) {
		    UpdatePhotosForAlbum(a);
		  }
		}
		
		public Photo RetrievePhoto(string photoid) {
		  FlickrNet.PhotoInfo pInfo = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      pInfo = flickrObj.PhotosGetInfo(photoid);
		    } catch(Exception ex) {
		      Console.WriteLine(ex.Message);
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) throw ex;
		      continue;
		    }
		    break;
		  }
		  Gdk.Pixbuf pix = PersistentInformation.GetInstance().GetThumbnail(photoid);
		  if (pix == null) {
		    System.IO.Stream stream = webclient.OpenRead(pInfo.SquareThumbnailUrl);
		    pix = new Gdk.Pixbuf(stream);
		  }
		  Console.WriteLine("Got description: " + pInfo.Description);
		  Photo photo = new Photo(photoid, pInfo.Title, pInfo.Description,
		                          pInfo.License, pInfo.Visibility.IsPublic,
		                          pInfo.Visibility.IsFriend, 
		                          pInfo.Visibility.IsFamily, "", pix);
		  // Couldn't add the last updated information, coz the library doesn't
		  // provide those methods unfortunately.
		  
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
      
      Photoset[] sets = null;
      for (int i=0; i<MAXTRIES; i++) {
        try {
		      sets = flickrObj.PhotosetsGetList().PhotosetCollection;
		    } catch(Exception ex) {
		      Console.WriteLine(ex.Message);
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) throw ex;
		      continue;
		    }
		    break;
		  }
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(sets.Length);
		  });
		  
		  foreach (Photoset s in sets) {
		    Album album = new Album(s.PhotosetId, s.Title, 
		                            s.Description, s.PrimaryPhotoId, 
		                            s.NumberOfPhotos);
		    Photo p = RetrievePhoto(s.PrimaryPhotoId);
		    PersistentInformation.GetInstance().UpdateAlbum(album);
        PersistentInformation.GetInstance().UpdatePhoto(p);
        
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		  }
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel("Done");
		    DeskFlickrUI.GetInstance().SetValueProgressBar(0);
		    DeskFlickrUI.GetInstance().SetProgressBarText("Photo Sets Updated");
		    DeskFlickrUI.GetInstance().PopulateAlbumTreeView();
		    DeskFlickrUI.GetInstance().ShowAllInWindow();
		  });
		}
		
		private void UpdatePhotosForAlbum(Album album) {
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetStatusLabel(
		        String.Format("Retrieving photos for set: {0}", album.Title));
		    DeskFlickrUI.GetInstance().SetProgressBarText("");
		  });
		  
		  FlickrNet.Photo[] photos = null;
		  for (int i=0; i<MAXTRIES; i++) {
		    try {
		      photos = flickrObj.PhotosetsGetPhotos(album.SetId);
		    } catch(Exception ex) {
		      Console.WriteLine(ex.Message);
		      // Maximum attempts over.
		      if (i == MAXTRIES-1) throw ex;
		      continue;
		    }
		    // if no exception
		    break;
		  }
		  
		  Gtk.Application.Invoke (delegate {
		    DeskFlickrUI.GetInstance().SetLimitsProgressBar(photos.Length);
		  });
		  
		  PersistentInformation.GetInstance().RemoveAllPhotosFromAlbum(album.SetId);
		  
		  foreach (FlickrNet.Photo p in photos) {
		    PersistentInformation.GetInstance().
		        AddPhotoToAlbum(p.PhotoId, album.SetId);
		    if (PersistentInformation.GetInstance().
		        HasLatestPhoto(p.PhotoId, p.lastupdate_raw)) {
		      continue;
		    }
		    Photo photo = RetrievePhoto(p.PhotoId);
		    photo.LastUpdate = p.lastupdate_raw;
		    PersistentInformation.GetInstance().UpdatePhoto(photo);
		    Gtk.Application.Invoke (delegate {
		      DeskFlickrUI.GetInstance().IncrementProgressBar(1);
		    });
		  }
		}
	}
