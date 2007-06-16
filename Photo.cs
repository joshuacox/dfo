
using System;
using System.Collections;

	public class Photo
	{
		private string id;
		private string title;
		private string desc;
		private int license;
		private int ispublic;
		private int isfriend;
		private int isfamily;
		private string lastupdate;
		// these are string tags, not from class Tag.
		private ArrayList tags;
		
		public Photo(string id, string title, string desc, int license,
		             int ispublic, int isfriend, int isfamily,
		             string lastupdate)
		{
		  this.id = id;
		  this.title = title;
		  this.desc = desc;
		  this.license = license;
		  this.ispublic = ispublic;
		  this.isfriend = isfriend;
		  this.isfamily = isfamily;
		  this.lastupdate = lastupdate;
		  this.tags = new ArrayList();
		}
		
		public string LastUpdate {
		  get {
		    return lastupdate;
		  }
		  set {
		    lastupdate = value;
		  }
		}
		
		public ArrayList Tags {
		  get {
		    return tags;
		  }
		  set {
		    tags.Clear();
		    foreach (string t in value) {
		      AddTag(t);
		    }
		  }
		}
		
		public void AddTag(string tag) {
		  if (!tags.Contains(tag)) {
		    tags.Add(tag);
		  }
		}
		
		public string TagString {
      get {
        if (tags == null)
          return "";
        return Utils.GetDelimitedString(tags, " ");
      }
		}
		
		public string PrivacyInfo {
		  get {
		    string info = "";
		    if (ispublic == 1) info = "This photo is Public";
		    else if (isfriend == 1 && isfamily == 1) 
		      info = "Only Friends and Family see this";
		    else if (isfriend == 1) info = "Only Friends see this";
		    else if (isfamily == 1) info = "Only Family see this";
		    else info = "This photo is Private";
		    return info;
		  }
		}
	
		public Gdk.Pixbuf Thumbnail {
		  get {
		    return PersistentInformation.GetInstance().GetThumbnail(id);
		  }
		  set {
		    PersistentInformation.GetInstance().SetThumbnail(id, value);
		  }
		}
		
		public Gdk.Pixbuf SmallImage {
		  get {
		    return PersistentInformation.GetInstance().GetSmallImage(id);
		  }
		  set {
		    PersistentInformation.GetInstance().SetSmallImage(id, value);
		  }
		}
		
		public string Id {
		  get {
		    return id;
		  }
		  set {
		    id = value;
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
    
    public string Description {
      get {
        return desc;
      }
      set {
        desc = value;
      }
    }
    
    public int License {
		  get {
		    return license;
		  }
		  set {
		    license = value;
		  }
    }
    
    public string LicenseInfo {
      get {
        if (license == 1) return "Attribution-NonCommercial-ShareAlike License";
        else if (license == 2) return "Attribution-NonCommercial License";
        else if (license == 3) return "Attribution-NonCommercial-NoDerivs License";
        else if (license == 4) return "Attribution License";
        else if (license == 5) return "Attribution-ShareAlike License";
        else if (license == 6) return "Attribution-NoDerivs License";
        else return "All Rights Reserved";
      }
    }
    
    public int IsPublic {
		  get {
		    return ispublic;
		  }
		  set {
		    ispublic = value;
		  }
    }
    
    public int IsFamily {
		  get {
		    return isfamily;
		  }
		  set {
		    isfamily = value;
		  }
    }
    
    public int IsFriend {
		  get {
		    return isfriend;
		  }
		  set {
		    isfriend = value;
		  }
    }
	}