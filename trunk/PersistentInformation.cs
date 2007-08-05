
using GConf;
using System;
using System.Data;
using System.IO;
using System.Collections;
using Mono.Data.SqliteClient;

	public class PersistentInformation
	{
    private static PersistentInformation info = null;
		private GConf.Client client;

		private readonly object _photolock;
		private readonly object _commentlock;
		private readonly object _albumlock;
		private readonly object _setphotolock;
		private readonly object _taglock;
		private readonly object _poollock;
		private readonly object _poolphotolock;
		private readonly object _bloglock;
		private readonly object _blogphotolock;
		private readonly object _downloadlock;
		private readonly object _uploadlock;
		private readonly object _writelock;
		
		private static string GCONF_APP_PATH = "/apps/DesktopFlickrOrganizer";
		private static string SECRET_TOKEN = GCONF_APP_PATH + "/token";
		private static string ORDERED_SETS_LIST = GCONF_APP_PATH + "/sets";
		private static string DOWNLOAD_FOLDER = GCONF_APP_PATH + "/downloadfolder";
		private static string UPLOAD_FILE = GCONF_APP_PATH + "/uploadfile";
		private static string USER_NSID = GCONF_APP_PATH + "/userid";
		private static string USER_NAME = GCONF_APP_PATH + "/username";
		private static string HEIGHT = GCONF_APP_PATH + "/height";
		private static string WIDTH = GCONF_APP_PATH + "/width";
		private static string VPOSITION = GCONF_APP_PATH + "/vpos";
		private static string HPOSITION = GCONF_APP_PATH + "/hpos";
		
		private static string HOME = System.IO.Path.Combine(
		    System.Environment.GetEnvironmentVariable("HOME"), ".desktopflickr");
		private static string THUMBNAIL_DIR = System.IO.Path.Combine(HOME, "thumbnails");
		private static string SMALL_IMAGE_DIR = System.IO.Path.Combine(HOME, "small_images");
    private static string DB_PATH = "URI=file:" + HOME + "/sqlite.db,version=3,busy_timeout=30000";
    
    private static string CREATE_PHOTO_TABLE = 
        "create table photo (\n"
        + " id varchar(25) primary key,\n"
        + " title varchar(256),\n"
        + " desc text,\n"
        + " license integer,\n"
        + " ispublic integer,\n"
        + " isfriend integer,\n"
        + " isfamily integer,\n"
        + " lastupdate varchar(25) default '',\n"
        // Entries after this, can be ordered in any way. They're not
        // being read by select all.
        + " dateposted varchar(25) default '',\n"
        + " isdeleted integer default 0,\n"
        + " isdirty integer default 0\n"
        + ");";
    
    private static string CREATE_ORIGINAL_PHOTO_TABLE = 
        "create table originalphoto (\n"
        + " id varchar(25) primary key,\n"
        + " title varchar(256),\n"
        + " desc text,\n"
        + " license integer,\n"
        + " ispublic integer,\n"
        + " isfriend integer,\n"
        + " isfamily integer,\n"
        + " lastupdate varchar(25) default '',\n"
        + " tags text\n"
        + ");";
    
    private static string CREATE_COMMENT_TABLE =
        "create table comment (\n"
        + " photoid varchar(25),\n"
        + " commentid varchar(25),\n"
        + " commenthtml text,\n"
        + " username varchar(25) default '',\n"
        + " isdirty integer default 0,\n"
        + " isdeleted integer default 0\n"
        + ");";

    private static string CREATE_ALBUM_TABLE = 
        "create table album (\n"
        + " setid varchar(25) primary key,\n"
        + " title varchar(256),\n"
        + " desc text,\n"
        + " photoid varchar(10),\n"
        + " isdirty integer default 0,\n"
        + " isnew integer default 0\n"
        + ");";
		
		private static string CREATE_ALBUM_PHOTO_MAPPING_TABLE = 
		    "create table setphoto (\n"
		    + " setid varchar(25),\n"
		    + " photoid varchar(10)\n"
		    + ");";
		
		private static string CREATE_PHOTO_TAG_TABLE =
		    "create table tag (\n"
		    + " photoid varchar(25),\n"
		    + " tag varchar(56)\n"
		    + ");";
		
		private static string CREATE_POOL_TABLE =
		    "create table pool (\n"
		    + " groupid varchar(25) primary key,\n"
		    + " title varchar(256)\n"
		    + ");";
		
		private static string CREATE_POOL_PHOTO_TABLE =
		    "create table poolphoto (\n"
		    + "groupid varchar(25),\n"
		    + "photoid varchar(25),\n"
		    + "isadded integer default 0,\n"
		    + "isdeleted integer default 0\n"
		    + ");";
		
		private static string CREATE_BLOG_TABLE = 
		    "create table blog (\n"
		    + " blogid varchar(25),\n"
		    + " blogtitle varchar(256)\n"
		    + ");";
		
		private static string CREATE_BLOG_PHOTO_TABLE =
		    "create table blogphoto (\n"
		    + " blogid varchar(25),\n" //composite of (blogid, photoid) is primary key.
		    + " photoid varchar(25),\n"
		    + " title varchar(256),\n"
		    + " desc text\n"
		    + ");";
		
	  private static string CREATE_DOWNLOAD_TABLE =
	      "create table download (\n"
	      + " photoid varchar(25) primary key,\n"
	      + " foldername varchar(256)\n"
	      + ");";
    
    private static string CREATE_UPLOAD_TABLE =
        "create table upload (\n"
        + " filename varchar(256) primary key,\n"
        + " title varchar(256),\n"
        + " desc text,\n"
        + " license integer default -1,\n"
        + " ispublic integer default 0,\n"
        + " isfriend integer default 0,\n"
        + " isfamily integer default 0,\n"
        + " tags text\n"
        + ");";
    
    public class Entry {
      public string entry1;
      public string entry2;
      
      public Entry(string entry1, string entry2) {
        this.entry1 = entry1;
        this.entry2 = entry2;
      }
    }
    
    private System.Collections.Generic.Dictionary<string, Gdk.Pixbuf> _thumbnailbuffer;
    private System.Collections.Generic.Dictionary<string, Gdk.Pixbuf> _smallimagebuffer;
    
		private PersistentInformation()
		{
      Utils.EnsureDirectoryExists(HOME);
      Utils.EnsureDirectoryExists(THUMBNAIL_DIR);
      Utils.EnsureDirectoryExists(SMALL_IMAGE_DIR);
      
      // Attempt creation of tables. If the table exists, the command
      // execution will throw an exception. Dirty way, but had to be done.
      // The cleaner alternative through sql command using "if not exists"
      // is only available in the latest versions of sqlite; which is 
      // currently not ported over to mono.
      if (CreateTable(CREATE_PHOTO_TABLE)) {
        AddIndex("create index iphoto1 on photo(id);");
        AddIndex("create index iphoto2 on photo(isdirty);");
        AddIndex("create index iphoto3 on photo(isdeleted);");
      }
      if (CreateTable(CREATE_ORIGINAL_PHOTO_TABLE)) {
        AddIndex("create index ioriginalphoto1 on originalphoto(id);");
      }
      if (CreateTable(CREATE_COMMENT_TABLE)) {
        AddIndex("create index icomment1 on comment(photoid);");
        AddIndex("create index icomment2 on comment(commentid);");
      }
      if (CreateTable(CREATE_ALBUM_TABLE)) {
        AddIndex("create index ialbum1 on album(setid);");
        AddIndex("create index ialbum2 on album(photoid);");
        AddIndex("create index ialbum3 on album(isdirty);");
      }
      if (CreateTable(CREATE_ALBUM_PHOTO_MAPPING_TABLE)) {
        AddIndex("create index isetphoto1 on setphoto(setid);");
        AddIndex("create index isetphoto2 on setphoto(photoid);");
      }
      if (CreateTable(CREATE_PHOTO_TAG_TABLE)) {
        AddIndex("create index itag1 on tag(photoid);");
        AddIndex("create index itag2 on tag(tag);");
      }
      if (CreateTable(CREATE_POOL_TABLE)) {
        AddIndex("create index ipool1 on pool(groupid);");
      }
      if (CreateTable(CREATE_POOL_PHOTO_TABLE)) {
        AddIndex("create index ipoolphoto1 on poolphoto(groupid);");
        AddIndex("create index ipoolphoto2 on poolphoto(photoid);");
      }
      if (CreateTable(CREATE_BLOG_TABLE)) {
        AddIndex("create index iblog1 on blog(blogid);");
      }
      if (CreateTable(CREATE_BLOG_PHOTO_TABLE)) {
        AddIndex("create index iblogphoto1 on blogphoto(blogid);");
        AddIndex("create index iblogphoto2 on blogphoto(photoid);");
      }
      CreateTable(CREATE_DOWNLOAD_TABLE);
      CreateTable(CREATE_UPLOAD_TABLE);
      
      client = new GConf.Client();
      _photolock = new object();
      _commentlock = new object();
      _albumlock = new object();
      _setphotolock = new object();
      _taglock = new object();
      _poollock = new object();
      _poolphotolock = new object();
      _bloglock = new object();
      _blogphotolock = new object();
      _downloadlock = new object();
      _uploadlock = new object();
      _writelock = new object();
      
      _thumbnailbuffer = new System.Collections.Generic.Dictionary<string, Gdk.Pixbuf>();
      _smallimagebuffer = new System.Collections.Generic.Dictionary<string, Gdk.Pixbuf>();
		}
		
		public static PersistentInformation GetInstance() {
		  if (info == null) {
		    info = new PersistentInformation();
		  }
		  return info;
		}

		public bool CreateTable(string query) {
		  try {
		    IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
        dbcon.Open();
        IDbCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = query;
        dbcmd.ExecuteNonQuery();
        dbcmd.Dispose();
        dbcon.Close();
        return true;
      } catch (SqliteSyntaxException ex) {
        if (!ex.Message.Contains("already exists")) throw ex;
        else return false;
      }
		}
  
    public void AddIndex(string query) {
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = query;
      dbcmd.ExecuteNonQuery();
      dbcmd.Dispose();
      dbcon.Close();
    }
    
    /*
     * Helper functions.
     */
		private bool RunIsTrueQuery(string query) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = query;
      IDataReader reader = dbcmd.ExecuteReader();
      int istrue = 0;
      if (reader.Read()) {
        istrue = reader.GetInt32(0); 
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return (istrue == 1);
		}
		
		private bool RunExistsQuery(string query) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = query;
      IDataReader reader = dbcmd.ExecuteReader();
      bool exists = reader.Read();
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return exists;
		}
		
		private void RunNonQuery(string query) {
		  lock (_writelock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = query;
		  dbcmd.ExecuteNonQuery();
		  dbcmd.Dispose();
		  dbcon.Close();
		  }
		}
		
		/*
		 * Photo retrieval and setting methods.
		 */
		private string GetThumbnailPath(string photoid) {
		  return String.Format("{0}/{1}.png", THUMBNAIL_DIR, photoid);
		}
		
		// Retrive the thumbnail of the photo.
		public Gdk.Pixbuf GetThumbnail(string photoid) {
		  Gdk.Pixbuf buf;
		  try {
		    string safephotoid = photoid.Replace("/", "_");
		    if (_thumbnailbuffer.ContainsKey(safephotoid)) {
		      return _thumbnailbuffer[safephotoid];
		    }
		    // Load the photo from the thumbnails directory.
		    if (System.IO.File.Exists(GetThumbnailPath(safephotoid))) {
  	      buf = new Gdk.Pixbuf(GetThumbnailPath(safephotoid));
  	    } else {
  	      // Photo isn't present in thumbnails directory, so load it from
  	      // file directly. This photo must be an new upload photo.
          string filename = photoid;
  	      Gdk.Pixbuf original = new Gdk.Pixbuf(filename);
  	      buf = original.ScaleSimple(75, 75, Gdk.InterpType.Bilinear);
  	      original.Dispose();
  	      SetThumbnail(photoid, buf);
  	    }
  	    if (buf != null && !_thumbnailbuffer.ContainsKey(safephotoid)) {
  	      _thumbnailbuffer.Add(safephotoid, buf);
  	    }
  	    return buf;
  		} catch(Exception) {
  		  return null;
  		}
		}
		
		public void SetThumbnail(string photoid, Gdk.Pixbuf buf) {
		  if (buf == null) return;
		  photoid = photoid.Replace("/", "_");
		  buf.Save(GetThumbnailPath(photoid), "png");
		}
		
		public void DeleteThumbnail(string photoid) {
		  photoid = photoid.Replace("/", "_");
		  FileInfo info = new FileInfo(GetThumbnailPath(photoid));
		  if (info.Exists) info.Delete();
		  if (_thumbnailbuffer.ContainsKey(photoid))
		      _thumbnailbuffer.Remove(photoid);
		}
		
		private string GetSmallImagePath(string photoid) {
		  return String.Format("{0}/{1}.png", SMALL_IMAGE_DIR, photoid);
		}
		
		// Retrive the small size image to be shown when editing of photos.
		public Gdk.Pixbuf GetSmallImage(string photoid) {
		  Gdk.Pixbuf buf;
		  try {
		    string safephotoid = photoid.Replace("/", "_");
		    if (_smallimagebuffer.ContainsKey(safephotoid)) {
		      return _smallimagebuffer[safephotoid];
		    }
		    if (System.IO.File.Exists(GetSmallImagePath(safephotoid))) {
  	      buf = new Gdk.Pixbuf(GetSmallImagePath(safephotoid));
  	    } else {
          string filename = photoid; // an upload photo.
  	      Gdk.Pixbuf original = new Gdk.Pixbuf(filename);
  	      buf = original.ScaleSimple(240, 180, Gdk.InterpType.Bilinear);
  	      original.Dispose();
  	      SetSmallImage(photoid, buf);
  	    }
  	    if (buf != null && !_smallimagebuffer.ContainsKey(safephotoid)) {
  	      _smallimagebuffer.Add(safephotoid, buf);
  	    }
  	    return buf;
  		} catch(Exception) {
  		  return null;
  		}
		}
		
		public void SetSmallImage(string photoid, Gdk.Pixbuf buf) {
		  if (buf == null) return;
		  photoid = photoid.Replace("/", "_");
		  buf.Save(GetSmallImagePath(photoid), "png");
		}
		
		public void DeleteSmallImage(string photoid) {
		  photoid = photoid.Replace("/", "_");
		  FileInfo finfo = new FileInfo(GetSmallImagePath(photoid));
		  if (finfo.Exists) finfo.Delete();
		  if (_smallimagebuffer.ContainsKey(photoid))
		      _smallimagebuffer.Remove(photoid);
		}
		
		public Photo GetPhoto(string photoid) {
		  Photo photo = null;
			lock (_photolock) {
		  string sqlQuery = String.Format(
		      "select * from photo where id='{0}';", photoid);
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = sqlQuery;
		  IDataReader reader = dbcmd.ExecuteReader();
		  if(reader.Read()) {
		    string id = reader.GetString(0);
		    string title = reader.GetString(1);
		    string desc = reader.GetString(2);
		    int license = reader.GetInt32(3);
		    int isPublic = reader.GetInt32(4);
		    int isFriend = reader.GetInt32(5);
		    int isFamily = reader.GetInt32(6);
		    string lastupdated = reader.GetString(7);
		    // Retrieve the thumbnail from thumbnail home directory.
		    
		    photo = new Photo(id, title, desc, license, isPublic,
		                      isFriend, isFamily, lastupdated);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  }
		  if (photo == null) {
		    return null;
		  }
		  photo.Tags = GetTags(photoid);
		  return photo;
		}
		
		public Photo GetOriginalPhoto(string photoid) {
		  Photo photo = null;
		  lock (_photolock) {
		  string sqlQuery = String.Format(
		      "select * from originalphoto where id='{0}';", photoid);
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = sqlQuery;
		  IDataReader reader = dbcmd.ExecuteReader();
		  if(reader.Read()) {
		    string id = reader.GetString(0);
		    string title = reader.GetString(1);
		    string desc = reader.GetString(2);
		    int license = reader.GetInt32(3);
		    int isPublic = reader.GetInt32(4);
		    int isFriend = reader.GetInt32(5);
		    int isFamily = reader.GetInt32(6);
		    string lastupdated = reader.GetString(7);
		    photo = new Photo(id, title, desc, license, isPublic,
		                      isFriend, isFamily, lastupdated);
		    string tagstring = reader.GetString(8);
		    photo.Tags = Utils.ParseTagsFromString(tagstring);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  }
      return photo;
		}
		
		public int GetCountPhotos() {
		  lock(_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select count(id) from photo where isdeleted=0;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  int count = 0;
		  if (reader.Read()) {
		    count = reader.GetInt32(0);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return count;
		  }
		}
		
		// This method only returns those photos whose isdeleted bit is set to 0.
		public ArrayList GetAllPhotos() {
		  lock(_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from photo where isdeleted=0 order by dateposted desc;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList photos = new ArrayList();
		  while(reader.Read()) {
		    string id = reader.GetString(0);
		    string title = reader.GetString(1);
		    string desc = reader.GetString(2);
		    int license = reader.GetInt32(3);
		    int isPublic = reader.GetInt32(4);
		    int isFriend = reader.GetInt32(5);
		    int isFamily = reader.GetInt32(6);
		    string lastupdated = reader.GetString(7);
		    // Retrieve the thumbnail from thumbnail home directory.
		    
		    Photo photo = new Photo(id, title, desc, license, isPublic,
		                      isFriend, isFamily, lastupdated);
		    photo.Tags = GetTags(id);
		    photos.Add(photo);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photos;
		  }
		}
		
		// This method returns _all_ the photos present in table.
		public ArrayList GetAllPhotoIds() {
		  lock(_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select id from photo;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList photoids = new ArrayList();
		  while(reader.Read()) {
		    photoids.Add(reader.GetString(0));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photoids;
		  }
		}
		
		public string GetDatePosted(string photoid) {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select dateposted from photo where id='{0}';", photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  string dateposted = "0";
		  if (reader.Read()) {
		    dateposted = reader.GetString(0);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return dateposted;
		  }
		}
		
		private bool HasPhoto(string photoid, string tablename) {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select id from {0} where id='{1}';", tablename, photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  bool hasphoto = reader.Read();
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return hasphoto;
		  }
		}
		
		public bool HasPhoto(string photoid) {
		  return HasPhoto(photoid, "photo");
		}
		
		public bool HasOriginalPhoto(string photoid) {
		  return HasPhoto(photoid, "originalphoto");
		}
		
		public bool HasLatestPhoto(string photoid, string lastupdate) {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select lastupdate from photo where id='{0}';", photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  bool haslatestphoto = reader.Read();
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  if (haslatestphoto) {
		    string storedlastupdate = reader.GetString(0);
		    if (storedlastupdate.Equals(lastupdate)) {
		      return true;
		    }
		  }
		  return false;
		  }
		}
		
		public void SetPhotoDirty(string photoid, bool isdirty) {
      if (!HasPhoto(photoid)) throw new Exception();
		  lock (_photolock) {
      int dirtvalue = 0;
      if (isdirty) dirtvalue = 1;
      RunNonQuery(String.Format(
          "update photo set isdirty={0} where id='{1}';", dirtvalue, photoid));
      }
		}
		
		public bool IsPhotoDirty(string photoid) {
		  lock (_photolock) {
		  return RunIsTrueQuery(String.Format(
		      "select isdirty from photo where id = '{0}';", photoid));
		  }
		}
		
		public ArrayList GetDirtyPhotos() {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from photo where isdirty=1;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList photos = new ArrayList();
		  while(reader.Read()) {
		    string id = reader.GetString(0);
		    string title = reader.GetString(1);
		    string desc = reader.GetString(2);
		    int license = reader.GetInt32(3);
		    int isPublic = reader.GetInt32(4);
		    int isFriend = reader.GetInt32(5);
		    int isFamily = reader.GetInt32(6);
		    string lastupdated = reader.GetString(7);

		    Photo photo = new Photo(id, title, desc, license, isPublic,
		                      isFriend, isFamily, lastupdated);
		    photo.Tags = GetTags(id);
		    photos.Add(photo);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photos;
		  }
		}
		
		public void UpdateMetaInfoPhoto(Photo p) {
		  lock (_photolock) {
		  string safeTitle = p.Title.Replace("'", "''");
		  string safeDesc = p.Description.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "update photo set title='{0}', desc='{1}', license={2}, ispublic={3}"
		      + ", isfriend={4}, isfamily={5} where id='{6}';", safeTitle, safeDesc,
		      p.License, p.IsPublic, p.IsFriend, p.IsFamily, p.Id));
		  }
		}
		
		public void InsertPhoto(Photo p) {
		  lock (_photolock) {
		  string safeTitle = p.Title.Replace("'", "''"); // try with \'
		  string safeDesc = p.Description.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "insert into photo (id, title, desc, license, ispublic, "
		      + "isfriend, isfamily, lastupdate, dateposted) values('{0}','{1}','{2}',{3}"
		      + ",{4},{5},{6},'{7}','{8}');",
		       p.Id, safeTitle, safeDesc, p.License, p.IsPublic, p.IsFriend,
		       p.IsFamily, p.LastUpdate, p.DatePosted));
		  }
		  foreach (string tag in p.Tags) {
		    InsertTag(p.Id, tag);
		  }
		}
		
		public void InsertOriginalPhoto(Photo p) {
			lock (_photolock) {
		  string safeTitle = p.Title.Replace("'", "''"); // try with \'
		  string safeDesc = p.Description.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "insert into originalphoto (id, title, desc, license, ispublic, "
		      + "isfriend, isfamily, lastupdate, tags) values('{0}','{1}','{2}',{3}"
		      + ",{4},{5},{6},'{7}','{8}');",
		       p.Id, safeTitle, safeDesc, p.License, p.IsPublic, p.IsFriend,
		       p.IsFamily, p.LastUpdate, p.TagString));
		  }
		}
		
		// This method also deletes the tags associated with the photo.
		// This method doesn't delete the photo from album. The reason is
		// that insertion and deletion of photo is independent of its
		// inclusion in certain sets.
		public void DeletePhoto(string photoid) {
		  // Delete the photo from database.
		  lock (_photolock) {
		  RunNonQuery(String.Format(
		      "delete from photo where id = '{0}';", photoid));
		  }
		  // Now delete the tags.
		  DeleteAllTags(photoid);
		}
		
		public void DeleteAllOriginalPhotos() {
		  lock (_photolock) {
		  RunNonQuery("delete from originalphoto;");
		  }
		}
		
		// This method is mainly meant to be used for updating of photos
		// done by FlickrCommunicator; during the periodic online checks.
		// The method updates the latest copy of the photo in the database,
		// except in the case when photo has been updated both at flickr server
		// and in the application; in which case, it temporarily stores the
		// server copy in-memory, and shows it up as a conflict.
		public void UpdatePhoto(Photo src) {
		  if (!HasPhoto(src.Id)) {
		    InsertPhoto(src);
		    return;
		  }
		  // We have the photo, call it dst.
		  Photo dst = GetPhoto(src.Id);
		  if (!dst.LastUpdate.Equals(src.LastUpdate)) {
		    if (IsPhotoDirty(src.Id)) {
		      DeskFlickrUI.GetInstance().AddServerPhoto(src);
		    } else {
		      DeletePhoto(src.Id);
		      InsertPhoto(src);
		    }
		  }
		}
		
		// This method would set the deleted bit of photo.
		public void SetPhotoForDeletion(string photoid) {
		  lock (_photolock) {
		  RunNonQuery(String.Format(
		      "update photo set isdeleted=1 where id='{0}';", photoid));
		  }
		}
		
		public ArrayList GetPhotoIdsDeleted() {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select id from photo where isdeleted=1;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList photoids = new ArrayList();
		  while(reader.Read()) {
		    photoids.Add(reader.GetString(0));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photoids;
		  }
		}
		
		/*
		 * Comments retrieval and setting methods.
		 */
		public bool HasComment(string photoid) {
		  lock (_commentlock) {
		  return RunExistsQuery(String.Format(
		      "select photoid from comment where photoid='{0}';", photoid));
		  }
		}
		
		public bool HasComment(string photoid, string commentid) {
		  lock (_commentlock) {
		  return RunExistsQuery(String.Format(
		      "select photoid from comment where photoid='{0}' and commentid='{1}';",
		      photoid, commentid));
		  }
		}
		
		public void InsertComment(string photoid, string commentid,
		                          string commenthtml, string username) {
		  lock (_commentlock) {
      string safecomment = commenthtml.Replace("'", "''");
      RunNonQuery(String.Format(
          "insert into comment (photoid, commentid, commenthtml, username)"
          + " values ('{0}','{1}','{2}','{3}');",
          photoid, commentid, safecomment, username));
		  }
		}
		
		public void InsertNewComment(string photoid, string commenthtml) {
		  Random r = new Random();
		  string commentid = "new:" + r.Next(1000).ToString();
		  while (HasComment(photoid, commentid)) {
		    commentid = "new:" + r.Next(1000).ToString();
		  }
		  InsertComment(photoid, commentid, commenthtml, UserName);
		  SetCommentDirty(photoid, commentid, true);
		}
		
		public void UpdateComment(string photoid, string commentid, string commenthtml) {
		  lock (_commentlock) {
		  string safecomment = commenthtml.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "update comment set commenthtml='{0}', isdirty=1"
		      + " where photoid='{1}' and commentid='{2}';",
		      safecomment, photoid, commentid));
		  }
		}
		
		public void SetCommentDirty(string photoid, string commentid, bool isdirty) {
		  lock (_commentlock) {
		  int dirty = 0;
		  if (isdirty) dirty = 1;
		  RunNonQuery(String.Format(
		      "update comment set isdirty={0} where photoid='{1}' and commentid='{2}';",
		      dirty, photoid, commentid));
		  }
		}
		
		public void MarkCommentForDeletion(string photoid, string commentid) {
		  lock (_commentlock) {
		  RunNonQuery(String.Format(
		      "update comment set isdeleted=1 where photoid='{0}' and commentid='{1}';",
		      photoid, commentid));
		  }
		}
		
		public void DeleteComment(string photoid, string commentid) {
		  lock (_commentlock) {
		  RunNonQuery(String.Format(
		      "delete from comment where photoid='{0}' and commentid='{1}';",
		      photoid, commentid));
		  }
		}
		
		public void DeleteCleanComments(string photoid) {
		  lock (_commentlock) {
		  RunNonQuery(String.Format(
		      "delete from comment where photoid='{0}' and isdirty=0;",
		      photoid));
		  }
		}
		
		// Returns (photoid, commenthtml) pair entries.
		public ArrayList GetNewComments() {
		  lock (_commentlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText =
		      "select photoid, commenthtml from comment where commentid like '%new:%';";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList comments = new ArrayList();
		  while (reader.Read()) {
		    string photoid = reader.GetString(0);
		    string commenthtml = reader.GetString(1);
		    PersistentInformation.Entry entry = new Entry(photoid, commenthtml);
		    comments.Add(entry);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return comments;
		  }
		}
		
		public ArrayList GetDirtyComments() {
		  lock (_commentlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText =
		      "select photoid, commentid, commenthtml, username from comment where isdirty=1;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList comments = new ArrayList();
		  while (reader.Read()) {
		    string photoid = reader.GetString(0);
		    string commentid = reader.GetString(1);
		    string commenthtml = reader.GetString(2);
		    string username = reader.GetString(3);
		    Comment comment = new Comment(photoid, commentid, commenthtml, username);
		    comments.Add(comment);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return comments;
		  }
		}

		public ArrayList GetDeletedComments() {
		  lock (_commentlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText =
		      "select photoid, commentid, commenthtml, username from comment where isdeleted=1;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList comments = new ArrayList();
		  while (reader.Read()) {
		    string photoid = reader.GetString(0);
		    string commentid = reader.GetString(1);
		    string commenthtml = reader.GetString(2);
		    string username = reader.GetString(3);
		    Comment comment = new Comment(photoid, commentid, commenthtml, username);
		    comments.Add(comment);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return comments;
		  }
		}
		
		public ArrayList GetCommentsForPhoto(string photoid) {
		  lock (_commentlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select commentid, commenthtml, username from comment where photoid='{0}';",
		      photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList comments = new ArrayList();
		  while (reader.Read()) {
		    string commentid = reader.GetString(0);
		    string commenthtml = reader.GetString(1);
		    string username = reader.GetString(2);
		    Comment comment = new Comment(commentid, commenthtml, username);
		    comments.Add(comment);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return comments;
		  }
		}
		
		/*
		 * Album retrieval and setting methods.
		 */
		public void InsertAlbum(Album a) {
		  lock (_albumlock) {
		  string safetitle = a.Title.Replace("'", "''");
		  string safedesc = a.Desc.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "insert into album (setid, title, desc, photoid) "
		      + "values ('{0}','{1}','{2}',{3});",
		      a.SetId, safetitle, safedesc, a.PrimaryPhotoid));
		  }
		}
		
		public bool HasAlbum(string setid) {
		  lock (_albumlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select setid from album where setid='{0}';", setid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  bool hasalbum = reader.Read();
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
      return hasalbum;
      }
		}
		
		public Album GetAlbum(string setid) {
		  lock (_albumlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select * from album where setid = '{0}';", setid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  Album album;
		  if (reader.Read()) {
        setid = reader.GetString(0);
        string title = reader.GetString(1);
        string desc = reader.GetString(2);
        string photoid = reader.GetString(3);
        album = new Album(setid, title, desc, photoid);
		  } else {
		    album = null;
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return album;
		  }
		}
		
    public void SetAlbumDirty(string setid, bool isdirty) {
      if (!HasAlbum(setid)) throw new Exception();
      lock (_albumlock) {
      int dirtvalue = 0;
      if (isdirty) dirtvalue = 1;
      RunNonQuery(String.Format(
          "update album set isdirty={0} where setid='{1}';", dirtvalue, setid));
      }
    }
    
    public void SetAlbumDirtyIfNotNew(string setid) {
      if (!HasAlbum(setid)) throw new Exception("Album not present: " + setid);
      lock (_albumlock) {
      RunNonQuery(String.Format(
          "update album set isdirty=1 where setid='{0}' and isnew=0;", setid));
      }
    }
    
    public bool IsAlbumDirty(string setid) {
      if (!HasAlbum(setid)) return false;
      lock (_albumlock) {
      return RunIsTrueQuery(String.Format(
          "select isdirty from album where setid = '{0}';", setid));
      }
    }
		
		public void SetPrimaryPhotoForAlbum(string setid, string photoid) {
      if (!HasAlbum(setid)) throw new Exception();
      lock (_albumlock) {
      RunNonQuery(String.Format(
          "update album set photoid={0} where setid='{1}';", photoid, setid));
      }
		}
		
		// This method doesn't remove the photos associated with the album.
		// It deletes only the entry in album table.
		public void DeleteAlbum(string setid) {
		  lock (_albumlock) {
		  RunNonQuery(String.Format(
		      "delete from album where setid = '{0}';", setid));
		  }
		}
		
		public void UpdateAlbum(Album src) {
		  if (!HasAlbum(src.SetId)) {
		    InsertAlbum(src);
		    return;
		  }
		  if (!IsAlbumDirty(src.SetId)) {
		    DeleteAlbum(src.SetId);
		    InsertAlbum(src);
		  }
		}
		
		public ArrayList GetAlbums() {
		  lock (_albumlock) {
		  ArrayList albums = new ArrayList();
		  foreach (string setid in OrderedSetsList.Split(',')) {
		    Album a = GetAlbum(setid);
		    if (a != null) albums.Add(a);
		  }
		  // Now check for those albums which are not present in the ordered list.
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from album;";
		  IDataReader reader = dbcmd.ExecuteReader();
      while(reader.Read()) {
        string setid = reader.GetString(0);
        string title = reader.GetString(1);
        string desc = reader.GetString(2);
        string photoid = reader.GetString(3);
        Album album = new Album(setid, title, desc, photoid);
        bool ispresent = false;
        foreach (Album a in albums) {
          if (album.IsEqual(a)) ispresent = true;
        }
        if (!ispresent) albums.Add(album);
      }
       // clean up
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return albums;
      }
		}
		
		public ArrayList GetDirtyAlbums(bool isdirty) {
		  lock (_albumlock) {
      ArrayList albums = new ArrayList();
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      int dirty = 0;
      if (isdirty) dirty = 1;
		  dbcmd.CommandText = String.Format(
		      "select * from album where isdirty={0};", dirty);
		  IDataReader reader = dbcmd.ExecuteReader();
      while(reader.Read()) {
        string setid = reader.GetString(0);
        string title = reader.GetString(1);
        string desc = reader.GetString(2);
        string photoid = reader.GetString(3);
        Album album = new Album(setid, title, desc, photoid);
        albums.Add(album);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return albums;
      }
		}
		
				
		public void SetNewAlbum(string setid) {
			lock (_albumlock) {
		  RunNonQuery(String.Format(
          "update album set isnew=1 where setid='{0}';", setid)); 
		  }
		}
		
		public bool IsAlbumNew(string setid) {
		  lock (_albumlock) {
		  return RunIsTrueQuery(String.Format(
		      "select isnew from album where setid='{0}';", setid));
		  }
		}
		
		public ArrayList GetNewAlbums() {
		  lock (_albumlock) {
      ArrayList albums = new ArrayList();
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format("select * from album where isnew=1;");
		  IDataReader reader = dbcmd.ExecuteReader();
      while(reader.Read()) {
        string setid = reader.GetString(0);
        string title = reader.GetString(1);
        string desc = reader.GetString(2);
        string photoid = reader.GetString(3);
        Album album = new Album(setid, title, desc, photoid);
        albums.Add(album);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return albums;
      }
		}
		
		/*
		 * Photos assigned to sets and retrieval mechanisms.
		 */
		public bool HasAlbumPhoto(string photoid, string setid) {
		  lock (_setphotolock) {
		  return RunExistsQuery(String.Format(
		      "select setid from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid));
      }
		}
		
		public void AddPhotoToAlbum(string photoid, string setid) {
      if (HasAlbumPhoto(photoid, setid)) return;
		  // Entry not present in the table, insert it.
		  lock (_setphotolock) {
		  RunNonQuery(String.Format(
		      "insert into setphoto (setid, photoid) values('{0}','{1}');",
		      setid, photoid));
		  }
		}
		
		public void DeletePhotoFromAlbum(string photoid, string setid) {
		  lock (_setphotolock) {
		  RunNonQuery(String.Format(
		      "delete from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid));
		  }
		}
		
		public void DeletePhotoFromAllAlbums(string photoid) {
		  lock (_setphotolock) {
		  RunNonQuery(String.Format(
		      "delete from setphoto where photoid='{0}';", photoid));
		  }
		}

		public void DeleteAllPhotosFromAlbum(string setid) {
		  lock (_setphotolock) {
		  RunNonQuery(String.Format("delete from setphoto where setid='{0}';", setid));
		  }
		}
		
		public ArrayList GetPhotoIdsForAlbum(string setid) {
		  lock (_setphotolock) {
		  ArrayList photoids = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select setphoto.photoid from setphoto, photo where setphoto.setid='{0}'"
		      + " and photo.id=setphoto.photoid order by photo.dateposted desc;", setid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  while (reader.Read()) {
		    photoids.Add(reader.GetString(0));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photoids;
		  }
		}
		
		public int GetNumPhotosForAlbum(string setid) {
		  return GetPhotoIdsForAlbum(setid).Count;
		}
		
		public ArrayList GetPhotosForAlbum(string setid) {
		  ArrayList photos = new ArrayList();
		  foreach (string photoid in GetPhotoIdsForAlbum(setid)) {
		    Photo photo = this.GetPhoto(photoid);
		    if (photo != null)
		      photos.Add(photo);
		  }
		  return photos;
		}
		 
		/*
		 * Tags retrieval and setting mechanisms.
		 */
		public ArrayList GetAllTags() {
		  lock (_taglock) {
		  ArrayList entries = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select tag, count(photoid) from tag group by tag;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  while(reader.Read()) {
		    string tag = reader.GetString(0);
		    int numpics = reader.GetInt32(1);
		    entries.Add(new Entry(tag, numpics.ToString()));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return entries;
		  }
		}
		
		public int GetCountPhotosForTag(string tag) {
		  lock (_taglock) {
      int num = 0;
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select count(photoid) from tag where tag='{0}';", tag);
		  IDataReader reader = dbcmd.ExecuteReader();
		  if (reader.Read()) {
		    num = reader.GetInt32(0);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return num;
		  }
		}
		
		public ArrayList GetPhotoIdsForTag(string tag) {
		  lock (_taglock) {
		  ArrayList photoids = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select tag.photoid from tag, photo where tag.tag='{0}'"
		      + " and tag.photoid=photo.id order by photo.dateposted desc;", tag);
		  IDataReader reader = dbcmd.ExecuteReader();
		  while(reader.Read()) {
		    photoids.Add(reader.GetString(0));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return photoids;
		  }
		}
		
		public ArrayList GetPhotosForTag(string tag) {
		  ArrayList photos = new ArrayList();
		  foreach (string id in GetPhotoIdsForTag(tag)) {
		    Photo p = GetPhoto(id);
		    if (p != null) photos.Add(p);
		  }
		  return photos;
		}
		
		public Photo GetSinglePhotoForTag(string tag) {
		  lock (_taglock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select photoid from tag where tag='{0}'"
		      + " order by random() limit 1;", tag);
		  IDataReader reader = dbcmd.ExecuteReader();
		  string photoid = "";
		  while(reader.Read()) {
		    photoid = reader.GetString(0);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return GetPhoto(photoid);
		  }
		}
		
		public ArrayList GetTags(string photoid) {
		  lock (_taglock) {
		  ArrayList tags = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select distinct tag from tag where photoid='{0}' order by tag;", photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  while(reader.Read()) {
		    tags.Add(reader.GetString(0));
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return tags;
		  }
		}
		
		public bool HasTag(string photoid, string tag) {
		  lock (_taglock) {
		  return RunExistsQuery(String.Format(
		      "select * from tag where photoid='{0}' and tag='{1}';", photoid, tag));
      }
		}
		
		public void InsertTag(string photoid, string tag) {
		  if (HasTag(photoid, tag)) return;
		  lock (_taglock) {
		  RunNonQuery(String.Format(
		      "insert into tag (photoid, tag) values('{0}', '{1}');", photoid, tag));
		  }
		}
		
		public void DeleteAllTags(string photoid) {
		  lock (_taglock) {
		  RunNonQuery(String.Format(
		      "delete from tag where photoid = '{0}';", photoid));
		  }
		}
		
		public void DeleteTag(string photoid, string tag) {
		  lock (_taglock) {
		  RunNonQuery(String.Format(
		      "delete from tag where photoid = '{0}' and tag = '{1}';", photoid, tag));
		  }
		}
		
		/*
		 * Pool retrieval and setting methods.
		 */
		public void InsertPool(string groupid, string title) {
		  lock (_poollock) {
		  RunNonQuery(String.Format(
		      "insert into pool (groupid, title) values('{0}', '{1}');",
		      groupid, title));
		  }
		}
		
		public void DeleteAllPools() {
		  lock (_poollock) {
		  RunNonQuery("delete from pool;");
		  }
		}
		
		public string GetPoolTitle(string groupid) {
		  lock (_poollock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = String.Format(
          "select title from pool where groupid='{0}';", groupid);
      IDataReader reader = dbcmd.ExecuteReader();
      string title = "";
      if (reader.Read()) {
        title = reader.GetString(0);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return title;
		  }
		}
		
		// Returns arraylist of groupid, title entries.
		public ArrayList GetAllPools() {
		  lock (_poollock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = "select * from pool;";
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList pools = new ArrayList();
      while (reader.Read()) {
        string groupid = reader.GetString(0);
        string title = reader.GetString(1);
        pools.Add(new Entry(groupid, title));
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return pools;
		  }
		}
		
		/*
		 * Pool photos retrieval and setting methods.
		 */
		public bool HasPoolPhoto(string photoid, string groupid) {
		  lock (_poolphotolock) {
		  return RunExistsQuery(String.Format(
		      "select groupid from poolphoto where groupid='{0}' and photoid='{1}';",
		      groupid, photoid));
      }
		}
		
	  public void InsertPhotoToPool(string photoid, string groupid) {
	    lock (_poolphotolock) {
	    RunNonQuery(String.Format(
	        "insert into poolphoto (groupid, photoid) values ('{0}','{1}');",
	        groupid, photoid));
	    }
	  }
	  
	  public void DeletePhotoFromPool(string photoid, string groupid) {
	    lock (_poolphotolock) {
	    RunNonQuery(String.Format(
	        "delete from poolphoto where groupid='{0}' and photoid='{1}';", 
	        groupid, photoid));
	    }
	  }
	  
	  public Photo GetSinglePhotoForPool(string groupid) {
	    lock (_poolphotolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = String.Format(
          "select photoid from poolphoto where groupid='{0}' and isdeleted=0"
          + " order by random() limit 1;", groupid);
      IDataReader reader = dbcmd.ExecuteReader();
      string photoid = "";
      if (reader.Read()) {
        photoid = reader.GetString(0);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return GetPhoto(photoid);
	    }
	  }
	  
	  public ArrayList GetPhotoidsForPool(string groupid) {
	    lock (_poolphotolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = String.Format(
          "select photoid from poolphoto where groupid='{0}' and isdeleted=0;",
          groupid);
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList photoids = new ArrayList();
      while (reader.Read()) {
        string photoid = reader.GetString(0);
        photoids.Add(photoid);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return photoids;
	    }
	  }
	  
	  public ArrayList GetPhotosForPool(string groupid) {
	    ArrayList photos = new ArrayList();
	    foreach (string photoid in GetPhotoidsForPool(groupid)) {
	      Photo p = GetPhoto(photoid);
	      if (p != null) photos.Add(p);
	    }
	    return photos;
	  }
	  
	  public bool IsPhotoAddedToPool(string photoid, string groupid) {
	    lock (_poolphotolock) {
	    return RunIsTrueQuery(String.Format(
	        "select isadded from poolphoto where groupid='{0}' and photoid='{1}';",
	        groupid, photoid));
	    }
	  }
	  
	  public void MarkPhotoAddedToPool(string photoid, string groupid, bool isadded) {
	    lock (_poolphotolock) {
	    int added = 0;
	    if (isadded) added = 1;
	    RunNonQuery(String.Format(
	        "update poolphoto set isadded={0} where groupid='{1}' and photoid='{2}';",
	        added, groupid, photoid));
	    }
	  }
	  
	  public ArrayList GetPhotosAddedToPools() {
	    lock (_poolphotolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = "select groupid,photoid from poolphoto where isadded=1;";
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList photos = new ArrayList();
      while (reader.Read()) {
        string groupid = reader.GetString(0);
        string photoid = reader.GetString(1);
        photos.Add(new Entry(groupid, photoid));
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return photos;
	    }
	  }
	  
	  public bool IsPhotoDeletedFromPool(string photoid, string groupid) {
	    lock (_poolphotolock) {
	    return RunIsTrueQuery(String.Format(
	        "select isdeleted from poolphoto where groupid='{0}' and photoid='{1}';",
	        groupid, photoid));
	    }
	  }
	  
	  public void MarkPhotoDeletedFromPool(string photoid, string groupid, bool isdeleted) {
	    lock (_poolphotolock) {
	    int deleted = 0;
	    if (isdeleted) deleted = 1;
	    RunNonQuery(String.Format(
	        "update poolphoto set isdeleted={0} where groupid='{1}' and photoid='{2}';",
	        deleted, groupid, photoid));
	    }
	  }
	  
	  public ArrayList GetPhotosDeletedFromPools() {
	    lock (_poolphotolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = "select groupid,photoid from poolphoto where isdeleted=1;";
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList photos = new ArrayList();
      while (reader.Read()) {
        string groupid = reader.GetString(0);
        string photoid = reader.GetString(1);
        photos.Add(new Entry(groupid, photoid));
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return photos;
	    }
	  }
	  
	  /*
	   * Blog insertion and retrieval methods.
	   */
	  public bool HasBlog(string blogid) {
	    lock (_bloglock) {
	    return RunExistsQuery(String.Format(
	        "select blogid from blog where blogid='{0}';", blogid));
	    }
	  }
	  
	  public void InsertBlog(string blogid, string blogtitle) {
	    lock (_bloglock) {
	    RunNonQuery(String.Format(
	        "insert into blog (blogid, blogtitle) values ('{0}','{1}');",
	        blogid, blogtitle));
	    }
	  }
	  
	  public void DeleteAllBlogs() {
	    lock (_bloglock) {
	    RunNonQuery("delete from blog;");
	    }
	  }
	  
	  public ArrayList GetAllBlogs() {
	    lock (_bloglock) {
	    IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
	    dbcon.Open();
	    IDbCommand dbcmd = dbcon.CreateCommand();
	    dbcmd.CommandText = "select blogid, blogtitle from blog";
	    IDataReader reader = dbcmd.ExecuteReader();
	    ArrayList blogs = new ArrayList();
	    while (reader.Read()) {
	      string blogid = reader.GetString(0);
	      string blogtitle = reader.GetString(1);
	      PersistentInformation.Entry entry = new Entry(blogid, blogtitle);
	      blogs.Add(entry);
	    }
	    reader.Close();
	    dbcmd.Dispose();
	    dbcon.Close();
	    return blogs;
	    }
	  }

	  /*
	   * Blog Photo insertion and retrieval methods.
	   */
	  public bool HasBlogPhoto(string blogid, string photoid) {
	    lock (_blogphotolock) {
	    return RunExistsQuery(String.Format(
	        "select blogid from blogphoto where blogid='{0}' and photoid='{1}';", 
	        blogid, photoid));
	    }
	  }
	  
	  public bool IsPhotoBlogged(string photoid) {
	    lock (_blogphotolock) {
	    return RunExistsQuery(String.Format(
	        "select photoid from blogphoto where photoid='{0}' limit 1;", photoid));
	    }
	  }
	  
	  public void InsertEntryToBlog(BlogEntry blogentry) {
	    if (HasBlogPhoto(blogentry.Blogid, blogentry.Photoid)) return;
	    lock (_blogphotolock) {
	    string safetitle = blogentry.Title.Replace("'", "''");
	    string safedesc = blogentry.Desc.Replace("'", "''");
	    RunNonQuery(String.Format(
	        "insert into blogphoto (blogid, photoid, title, desc)"
	        + " values ('{0}','{1}','{2}','{3}');",
	        blogentry.Blogid, blogentry.Photoid, safetitle, safedesc));
	    }
	  }
	  
	  public void UpdateEntryToBlog(BlogEntry blogentry) {
	    lock (_blogphotolock) {
	    string safetitle = blogentry.Title.Replace("'", "''");
	    string safedesc = blogentry.Desc.Replace("'", "''");
	    RunNonQuery(String.Format(
	        "update blogphoto set title='{0}', desc='{1}'"
	        + " where blogid='{2}' and photoid='{3}';",
	        safetitle, safedesc, blogentry.Blogid, blogentry.Photoid));
	    }
	  }
	  
	  public void DeleteEntryFromBlog(string blogid, string photoid) {
	    lock (_blogphotolock) {
	    RunNonQuery(String.Format(
	        "delete from blogphoto where blogid='{0}' and photoid='{1}';",
	        blogid, photoid));
	    }
	  }
	  
	  public void DeleteAllEntriesFromBlog(string blogid) {
	    lock (_blogphotolock) {
	    RunNonQuery(String.Format(
	        "delete from blogphoto where blogid='{0}';", blogid));
	    }
	  }
    
    public BlogEntry GetEntryForBlog(string blogid, string photoid) {
      lock (_blogphotolock) {
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = String.Format(
          "select * from blogphoto where blogid='{0}' and photoid='{1}';", 
          blogid, photoid);
      IDataReader reader = dbcmd.ExecuteReader();
      BlogEntry blogentry = null;
      if (reader.Read()) {
        reader.GetString(0); // blog id
        reader.GetString(1); // photo id
        string title = reader.GetString(2);
        string desc = reader.GetString(3);
        blogentry = new BlogEntry(blogid, photoid, title, desc);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return blogentry;
      }
    }
    
    public ArrayList GetEntriesForBlog(string blogid) {
      lock (_blogphotolock) {
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = String.Format(
          "select * from blogphoto where blogid='{0}';", blogid);
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList blogentries = new ArrayList();
      while (reader.Read()) {
        reader.GetString(0); // blog id
        string photoid = reader.GetString(1);
        string title = reader.GetString(2);
        string desc = reader.GetString(3);
        BlogEntry blogentry = new BlogEntry(blogid, photoid, title, desc);
        blogentries.Add(blogentry);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return blogentries;
      }
    }
    
    public ArrayList GetAllBlogEntries() {
      lock (_blogphotolock) {
      IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
      IDbCommand dbcmd = dbcon.CreateCommand();
      dbcmd.CommandText = "select * from blogphoto;";
      IDataReader reader = dbcmd.ExecuteReader();
      ArrayList blogentries = new ArrayList();
      while (reader.Read()) {
        string blogid = reader.GetString(0); // blog id
        string photoid = reader.GetString(1);
        string title = reader.GetString(2);
        string desc = reader.GetString(3);
        BlogEntry blogentry = new BlogEntry(blogid, photoid, title, desc);
        blogentries.Add(blogentry);
      }
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return blogentries;
      }
    }
    
		/*
		 * Download table methods.
		 */
		public void InsertEntryToDownload(string photoid, string foldername) {
		  lock (_downloadlock) {
		  string query = String.Format(
		      "insert into download (photoid, foldername) values ('{0}', '{1}');",
		      photoid, foldername);
		  RunNonQuery(query);
		  }
		}
		
		public bool IsDownloadEntryExists(string photoid) {
		  lock (_downloadlock) {
		  return RunExistsQuery(String.Format(
		      "select photoid from download where photoid='{0}';", photoid));
		  }
		}
		
		public void DeleteEntryFromDownload(string photoid) {
		  lock (_downloadlock) {
		  string query = String.Format(
		      "delete from download where photoid='{0}';", photoid);
		  RunNonQuery(query);
		  }
		}
		
		public ArrayList GetEntriesToDownload() {
		  lock (_downloadlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from download;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList downloadentries = new ArrayList();
		  while (reader.Read()) {
		    string photoid = reader.GetString(0);
		    string foldername = reader.GetString(1);
		    Entry entry = new Entry(photoid, foldername);
		    downloadentries.Add(entry);
		  }
		  reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
		  return downloadentries;
		  }
		}
		
		public string GetFolderNameForPhotoId(string photoid) {
		  lock (_downloadlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select foldername from download where photoid='{0}';", photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  string foldername = "";
		  if (reader.Read()) {
		    foldername = reader.GetString(0);
		  }
		  reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
		  return foldername;
		  }
		}
		
		/*
		 * Upload table methods.
		 */
		// This method takes care of checking if the file is already present
		// in the database.
		public void InsertEntryToUpload(string filename) {
		  if (!Utils.isImageFile(filename)) return;
		  string safefilename = filename.Replace("'", "''");
		  if (IsUploadEntryExists(safefilename)) return;
		  System.IO.FileInfo finfo = new System.IO.FileInfo(filename);
		  if (!finfo.Exists) {
		    Console.WriteLine(filename + " doesn't exist");
		    return;
		  }
		  
		  string title = finfo.Name.Replace("'", "''");
		  string desc = "Uploaded through <a href=''http://code.google.com/p/dfo''>"
		                + "Desktop Flickr Organizer</a>.";
		  string tags = "dfoupload";
		  lock (_uploadlock) { 
		  string query = String.Format(
		      "insert into upload (filename, title, desc, tags)" 
		      + " values ('{0}','{1}','{2}','{3}');",
		      safefilename, title, desc, tags);
		  RunNonQuery(query);
		  }
		}
		
		public bool IsUploadEntryExists(string filename) {
		  lock (_uploadlock) {
		  return RunExistsQuery(String.Format(
		      "select filename from upload where filename='{0}';", filename));
		  }
		}
				
		public void UpdateInfoForUploadPhoto(Photo p) {
		  lock (_uploadlock) {
		  string safeFilename = p.Id.Replace("'", "''");
		  string safeTitle = p.Title.Replace("'", "''");
		  string safeDesc = p.Description.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "update upload set title='{0}', desc='{1}', license={2}, ispublic={3}"
		      + ", isfriend={4}, isfamily={5}, tags='{6}' where filename='{7}';", 
		      safeTitle, safeDesc, p.License, p.IsPublic, p.IsFriend,
		      p.IsFamily, p.TagString, safeFilename));
		  }
		}
		
		public void DeleteEntryFromUpload(string filename) {
		  lock (_uploadlock) {
		  string query = String.Format(
		      "delete from upload where filename='{0}';", filename);
		  RunNonQuery(query);
		  }
		}
		
		public ArrayList GetPhotosToUpload() {
		  lock (_uploadlock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
		  dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from upload;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  ArrayList photos = new ArrayList();
		  while (reader.Read()) {
		    string filename = reader.GetString(0);
		    string title = reader.GetString(1);
		    string description = reader.GetString(2);
		    int license = reader.GetInt32(3);
		    int isPublic = reader.GetInt32(4);
		    int isFriend = reader.GetInt32(5);
		    int isFamily = reader.GetInt32(6);
		    Photo photo = new Photo(filename, title, description, license, isPublic,
		                            isFriend, isFamily, "new");
		    string tagstring = reader.GetString(7);
		    photo.Tags = Utils.ParseTagsFromString(tagstring);
		    photos.Add(photo);
		  }
		  reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
		  return photos;
		  }
		}
		
		/*
		 * Methods to be used by editor.
		 */

		public void UpdateTagsForPhoto(Photo p) {
		  DeleteAllTags(p.Id);
		  foreach (string tag in p.Tags) {
		    InsertTag(p.Id, tag);
		  }
		}
		
		public void SaveAlbum(Album a) {
		  bool isnew = IsAlbumNew(a.SetId);
		  DeleteAlbum(a.SetId);
		  InsertAlbum(a);
		  if (isnew) SetNewAlbum(a.SetId);
		  if (!HasAlbumPhoto(a.SetId, a.PrimaryPhotoid)) {
		    AddPhotoToAlbum(a.PrimaryPhotoid, a.SetId);
		  }
		  SetAlbumDirtyIfNotNew(a.SetId);
		}
		
		/*
		 * Methods talking to gconf
		 */
		public string Token
		{
		  get {
		    string token;
		    try {
		      token = (string) client.Get(SECRET_TOKEN);
		    } catch (GConf.NoSuchKeyException) {
		      token = "";
		    }
		    return token;
		  }
		  set {
		    client.Set(SECRET_TOKEN, value);
		  }
		}
		
		public string DownloadFoldername
		{
		  get {
		    string foldername;
		    try {
		      foldername = (string) client.Get(DOWNLOAD_FOLDER);
		    } catch (GConf.NoSuchKeyException) {
		      foldername = "";
		    }
		    return foldername;
		  }
		  set {
		    client.Set(DOWNLOAD_FOLDER, value);
		  }
		}
		
		public string UploadFilename
		{
		  get {
		    string filename;
		    try {
		      filename = (string) client.Get(UPLOAD_FILE);
		    } catch (GConf.NoSuchKeyException) {
		      filename = "";
		    }
		    return filename;
		  }
		  set {
		    client.Set(UPLOAD_FILE, value);
		  }
		}
		
		public string OrderedSetsList {
		  get {
		    string list;
		    try {
		      list = (string) client.Get(ORDERED_SETS_LIST);
		    } catch (GConf.NoSuchKeyException) {
		      list = "";
		    }
		    return list;
		  }
		  set {
		    client.Set(ORDERED_SETS_LIST, value);
		  }
		}
		
		public string UserId {
		  get {
		    string userid;
		    try {
		      userid = (string) client.Get(USER_NAME);
		    } catch (GConf.NoSuchKeyException) {
		      userid = "";
		    }
		    return userid;
		  }
		  set {
		    client.Set(USER_NSID, value);
		  }
		}

		public string UserName {
		  get {
		    string username;
		    try {
		      username = (string) client.Get(USER_NAME);
		    } catch (GConf.NoSuchKeyException) {
		      username = "me";
		    }
		    return username;
		  }
		  set {
		    client.Set(USER_NAME, value);
		  }
		}
		
		public int WindowWidth {
		  get {
		    int width;
		    try {
		      width = (int) client.Get(WIDTH);
		    } catch (GConf.NoSuchKeyException) {
		      width = 0;
		    }
		    return width;
		  }
		  set {
		    client.Set(WIDTH, value);
		  }
		}
		
		public int WindowHeight {
		  get {
		    int height;
		    try {
		      height = (int) client.Get(HEIGHT);
		    } catch (GConf.NoSuchKeyException) {
		      height = 0;
		    }
		    return height;
		  }
		  set {
		    client.Set(HEIGHT, value);
		  }
		}
		
		public int VerticalPosition {
		  get {
		    int vposition;
		    try {
		      vposition = (int) client.Get(VPOSITION);
		    } catch (GConf.NoSuchKeyException) {
		      vposition = 0;
		    }
		    return vposition;
		  }
		  set {
		    client.Set(VPOSITION, value);
		  }
		}
		
		public int HorizontalPosition {
		  get {
		    int hposition;
		    try {
		      hposition = (int) client.Get(HPOSITION);
		    } catch (GConf.NoSuchKeyException) {
		      hposition = 0;
		    }
		    return hposition;
		  }
		  set {
		    client.Set(HPOSITION, value);
		  }
		}
	}