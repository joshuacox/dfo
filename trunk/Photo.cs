
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
		private Gdk.Pixbuf thumbnail;
		// these are string tags, not from class Tag.
		private ArrayList tags;
		
		public Photo(string id, string title, string desc, int license,
		             int ispublic, int isfriend, int isfamily,
		             string lastupdate, Gdk.Pixbuf thumbnail)
		{
		  this.id = id;
		  this.title = title;
		  this.desc = desc;
		  this.license = license;
		  this.ispublic = ispublic;
		  this.isfriend = isfriend;
		  this.isfamily = isfamily;
		  this.lastupdate = lastupdate;
		  this.thumbnail = thumbnail;
		  this.tags = null;
		}
		
		public string GetInsertStatement() {
		  System.Text.StringBuilder strb = 
		      new System.Text.StringBuilder("insert into photo");
		  strb.Append(" (id, title, desc, license, ispublic, isfriend, isfamily)");
		  strb.Append(" values(");
		  string safeTitle = title.Replace("'", "''");
		  string safeDesc = desc.Replace("'", "''");
		  strb.AppendFormat("'{0}','{1}','{2}',", id, safeTitle, safeDesc);
		  strb.AppendFormat("{0},{1},{2},{3});", 
		                    license, ispublic, isfriend, isfamily);
		  return strb.ToString();
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
		    tags = value;
		  }
		}
		
		public string TagString {
      get {
        if (tags == null)
          return "";
        return Utils.GetTagString(tags);
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
		    return thumbnail;
		  }
		  set {
		    thumbnail = value;
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