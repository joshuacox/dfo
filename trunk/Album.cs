
using System;

	public class Album
	{
		private string setid;
	  private string title;
	  private string desc;
	  private string photoid;
	  
		public Album(string setid, string title, 
		             string desc, string photoid)
		{
		  this.setid = setid;
		  this.title = title;
		  this.desc = desc;
		  this.photoid = photoid;
		}
		
		public Album(Album a) {
		  this.setid = a.setid;
		  this.title = a.title;
		  this.desc = a.desc;
		  this.photoid = a.photoid;
		}
		
		public bool IsEqual(Album a) {
		  if (this == a)
		    return true;
		  bool isequal = true;
		  if (this.setid != a.setid)  isequal = false;
		  if (this.title != a.title)  isequal = false;
		  if (this.desc != a.desc) isequal = false;
		  if (this.photoid != a.photoid) isequal = false;
		  return isequal;
		}
		
		public string SetId {
		  get {
		    return setid;
		  }
		  set {
		    setid = value;
		  }
		}
		
		public string Title {
		  get {
		    return title;
		  }
		  set {
		    title = value;
		  }
		}
		
		public string Desc {
		  get {
		    return desc;
		  }
		  set {
		    desc = value;
		  }
		}
		
		public string PrimaryPhotoid {
		  get {
		    return photoid;
		  }
		  set {
		    photoid = value;
		  }
		}
		
		public int NumPics {
		  get {
		    return PersistentInformation.GetInstance().GetNumPhotosForAlbum(setid);
		  }
		}
	}