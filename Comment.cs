
using System;

	public class Comment
	{
		private string _id;
		private string _html;
		private string _username;
		private string _photoid;
		
		public Comment(string commentid, string commenthtml, string username) {
		  this._id = commentid;
		  this._html = commenthtml;
		  this._username = username;
		}
		
		public Comment(string photoid, string commentid, string commenthtml, 
		    string username) : this(commentid, commenthtml, username) {
		  this._photoid = photoid;
		}
		
		public string CommentId {
		  get {
		    return _id;
		  }
		}
		
		public string CommentHtml {
		  get {
		    return _html;
		  }
		}
		
		public string UserName {
		  get {
		    return _username;
		  }
		}
		
		public string PhotoId {
		  get {
		    return _photoid;
		  }
		}
	}