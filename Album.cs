
using System;

	public class Album
	{
		private string setid;
	  private string title;
	  private string desc;
	  private string photoid;
	  private int numpics;
	  
		public Album(string setid, string title, 
		             string desc, string photoid, int numpics)
		{
		  this.setid = setid;
		  this.title = title;
		  this.desc = desc;
		  this.photoid = photoid;
		  this.numpics = numpics;
		}
		
		public bool IsEqual(Album a) {
		  if (this == a)
		    return true;
		  bool isequal = true;
		  if (this.setid != a.setid)  isequal = false;
		  if (this.title != a.title)  isequal = false;
		  if (this.desc != a.desc) isequal = false;
		  if (this.photoid != a.photoid) isequal = false;
		  if (this.numpics != a.numpics) isequal = false;
		  return isequal;
		}
		
		public string GetInsertStatement() {
		  System.Text.StringBuilder strb = 
		      new System.Text.StringBuilder("insert into album");
		  strb.Append(" (setid, title, desc, photoid, numpics)");
		  strb.Append(" values(");
		  strb.AppendFormat("'{0}','{1}','{2}',", setid, title, desc);
		  strb.AppendFormat("'{0}',{1}", photoid, numpics);
		  strb.Append(");");
		  return strb.ToString();
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
		    return numpics;
		  }
		  set {
		    numpics = value;
		  }
		}
	}