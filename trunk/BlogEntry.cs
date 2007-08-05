
using System;
	
	public class BlogEntry
	{
		private string blogid;
	  private string photoid;
	  private string title;
	  private string desc;
	  
		public BlogEntry(string blogid, string photoid,
		                 string title, string desc)
		{
		  this.blogid = blogid;
		  this.photoid = photoid;
		  this.title = title;
		  this.desc = desc;
		}
		
		public string Blogid {
		  get {
		    return blogid;
		  }
		  set {
		    blogid = value;
		  }
		}
		
		public string Photoid {
		  get {
		    return photoid;
		  }
		  set {
		    photoid = value;
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
	}