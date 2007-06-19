
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

		private Random rand;
		private readonly object _photolock;
		private readonly object _albumlock;
		private readonly object _setphotolock;
		private readonly object _taglock;
		private readonly object _writelock;
		
		private static string GCONF_APP_PATH = "/apps/DesktopFlickrOrganizer";
		private static string SECRET_TOKEN = GCONF_APP_PATH + "/token";
		private static string ORDERED_SETS_LIST = GCONF_APP_PATH + "/sets";
		
		private static string HOME = 
		    System.Environment.GetEnvironmentVariable("HOME") + "/.desktopflickr";
		private static string THUMBNAIL_DIR = HOME + "/thumbnails";
		private static string SMALL_IMAGE_DIR = HOME + "/small_images";
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
        + " isdirty integer default 0\n"
        + ");";
    
    private static string CREATE_ALBUM_TABLE = 
        "create table album (\n"
        + " setid varchar(25) primary key,\n"
        + " title varchar(256),\n"
        + " desc text,\n"
        + " photoid varchar(10),\n"
        + " isdirty integer default 0\n"
        + ");";
		
		private static string CREATE_ALBUM_PHOTO_MAPPING_TABLE = 
		    "create table setphoto (\n"
		    + " setid varchar(25),\n"
		    + " photoid varchar(10),\n"
		    + " isdirty integer default 0,\n"
		    + " isdeleted integer default 0\n"
		    + ");";
		
		private static string CREATE_PHOTO_TAG_TABLE =
		    "create table tag (\n"
		    + " photoid varchar(25),\n"
		    + " tag varchar(56)\n"
		    + ");";

    private void EnsureDirectoryExists(string dir) {
      System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir);
		  if (!dirInfo.Exists) {
		    dirInfo.Create();
		    Console.WriteLine("Created directory: " + dir);
		  }
    }
    
		private PersistentInformation()
		{
      EnsureDirectoryExists(HOME);
      EnsureDirectoryExists(THUMBNAIL_DIR);
      EnsureDirectoryExists(SMALL_IMAGE_DIR);
      
      // Attempt creation of tables. If the table exists, the command
      // execution will throw an exception. Dirty way, but had to be done.
      // The cleaner alternative through sql command using "if not exists"
      // is only available in the latest versions of sqlite; which is 
      // currently not ported over to mono.
      if (CreateTable(CREATE_PHOTO_TABLE)) {
        AddIndex("create index iphoto1 on photo(id);");
        AddIndex("create index iphoto2 on photo(isdirty);");
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
      
      client = new GConf.Client();
      rand = new Random();
      _albumlock = new object();
      _photolock = new object();
      _setphotolock = new object();
      _taglock = new object();
      _writelock = new object();
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
		  try {
		    FileStream fs = new FileStream(GetThumbnailPath(photoid), 
		                                   FileMode.Open, FileAccess.Read);
		    return new Gdk.Pixbuf(fs);
		  } catch(Exception e) {
		    return null;
		  }
		}
		
		public void SetThumbnail(string photoid, Gdk.Pixbuf buf) {
		  buf.Save(GetThumbnailPath(photoid), "png");
		}
		
		private string GetSmallImagePath(string photoid) {
		  return String.Format("{0}/{1}.png", SMALL_IMAGE_DIR, photoid);
		}
		
		// Retrive the small size image to be shown when editing of photos.
		public Gdk.Pixbuf GetSmallImage(string photoid) {
		  try {
		    FileStream fs = new FileStream(GetSmallImagePath(photoid), 
		                                   FileMode.Open, FileAccess.Read);
		    return new Gdk.Pixbuf(fs);
		  } catch(Exception e) {
		    return null;
		  }
		}
		
		public void SetSmallImage(string photoid, Gdk.Pixbuf buf) {
		  buf.Save(GetSmallImagePath(photoid), "png");
		}
		
		public Photo GetPhoto(String photoid) {
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
		
		public ArrayList GetAllPhotos() {
		  lock(_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select * from photo;";
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
		
		public bool HasPhoto(string photoid) {
		  lock (_photolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select id from photo where id='{0}';", photoid);
		  IDataReader reader = dbcmd.ExecuteReader();
		  bool hasphoto = reader.Read();
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  return hasphoto;
		  }
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
		  dbcmd.CommandText = "select id from photo where isdirty=1;";
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
		
		public void InsertPhoto(Photo p) {
		  lock (_photolock) {
		  string safeTitle = p.Title.Replace("'", "''"); // try with \'
		  string safeDesc = p.Description.Replace("'", "''");
		  RunNonQuery(String.Format(
		      "insert into photo (id, title, desc, license, ispublic, "
		      + "isfriend, isfamily, lastupdate) values('{0}','{1}','{2}',{3}"
		      + ",{4},{5},{6},'{7}');",
		       p.Id, safeTitle, safeDesc, p.License, p.IsPublic, p.IsFriend,
		       p.IsFamily, p.LastUpdate));
		  }
		  foreach (string tag in p.Tags) {
		    InsertTag(p.Id, tag);
		  }
		}
		
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
		
		// This method is mainly meant to be used for updating of photos
		// done by FlickrCommunicator; during the periodic online checks.
		public void UpdatePhoto(Photo src) {
		  if (!HasPhoto(src.Id)) {
		    InsertPhoto(src);
		    return;
		  }
		  if (!IsPhotoDirty(src.Id)) {
		    Photo dst = GetPhoto(src.Id);
		    if (!dst.LastUpdate.Equals(src.LastUpdate)) {
		      DeletePhoto(src.Id);
		      InsertPhoto(src);
		    }
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
    
    public bool IsAlbumDirty(string setid) {
      lock (_albumlock) {
      return RunIsTrueQuery(String.Format(
          "select isdirty from album where setid = '{0}';", setid));
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
		
		/*
		 * Photos assigned to sets and retrieval mechanisms.
		 */
		public bool HasAlbumPhoto(string photoid, string setid) {
		  lock (_setphotolock) {
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select * from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid);
      bool hasphoto = dbcmd.ExecuteReader().Read();
      dbcmd.Dispose();
      dbcon.Close();
      return hasphoto;
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
		
		public bool IsAlbumToPhotoDirty(string photoid, string setid) {
		  lock (_setphotolock) {
		  return RunIsTrueQuery(String.Format(
		      "select isdirty from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid));
		  }
		}
		
		public void SetAlbumToPhotoDirty(string photoid, string setid, bool dirty) {
		  lock (_setphotolock) {
		  int isdirty = 0;
		  if (dirty) isdirty = 1;
		  RunNonQuery(String.Format(
		      "update setphoto set isdirty = {0} where setid='{1}' and photoid='{2}';",
		      isdirty, setid, photoid));
		  }
		}
		
		public bool IsAlbumToPhotoMarkedDeleted(string photoid, string setid) {
		  lock (_setphotolock) {
		  return RunIsTrueQuery(String.Format(
		      "select isdeleted from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid));
		  }
		}
		
		public void MarkPhotoForDeletionFromAlbum(string photoid, 
		    string setid, bool deleted) {
		  lock (_setphotolock) {
		  int isdeleted = 0;
		  if (deleted) isdeleted = 1;
		  RunNonQuery(String.Format(
		      "update setphoto set isdirty = {0}, isdeleted = {0}"
		      + " where setid='{1}' and photoid='{2}';", isdeleted, setid, photoid));
		  }
		}
		
		public void DeletePhotoEntryFromAlbum(string photoid, string setid) {
		  lock (_setphotolock) {
		  RunNonQuery(String.Format(
		      "delete from setphoto where setid='{0}' and photoid='{1}';",
		      setid, photoid));
		  }
		}
		
		public void DeleteCleanPhotoEntriesFromAlbum(string setid) {
		  lock (_setphotolock) {
		  RunNonQuery(String.Format(
		      "delete from setphoto where setid='{0}' and isdirty=0;", setid));
		  }
		}
		
		public ArrayList GetPhotoIdsForAlbum(string setid) {
		  lock (_setphotolock) {
		  ArrayList photoids = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select photoid from setphoto where setid='{0}' and isdeleted=0;",
		      setid);
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
		  ArrayList tags = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = "select tag, count(photoid) from tag group by tag;";
		  IDataReader reader = dbcmd.ExecuteReader();
		  while(reader.Read()) {
		    Tag t = new Tag(reader.GetString(0), reader.GetInt32(1));
		    tags.Add(t);
		  }
		  reader.Close();
		  dbcmd.Dispose();
		  dbcon.Close();
		  tags.Sort();
		  return tags;
		  }
		}
		
		public ArrayList GetPhotoIdsForTag(string tag) {
		  lock (_taglock) {
		  ArrayList photoids = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select photoid from tag where tag='{0}';", tag);
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
		  ArrayList photoids = GetPhotoIdsForTag(tag);
		  int index = rand.Next(photoids.Count);
		  if (photoids.Count > 0) {
		    return GetPhoto((string) photoids[index]);
		  } else return null;
		}
		
		public ArrayList GetTags(string photoid) {
		  lock (_taglock) {
		  ArrayList tags = new ArrayList();
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select distinct tag from tag where photoid='{0}';", photoid);
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
		  IDbConnection dbcon = (IDbConnection) new SqliteConnection(DB_PATH);
      dbcon.Open();
		  IDbCommand dbcmd = dbcon.CreateCommand();
		  dbcmd.CommandText = String.Format(
		      "select * from tag where photoid='{0}' and tag='{1}';", photoid, tag);
		  IDataReader reader = dbcmd.ExecuteReader();
      bool hastag = reader.Read();
      reader.Close();
      dbcmd.Dispose();
      dbcon.Close();
      return hastag;
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
		 * License retrieval and viewing methods.
		 */
		public Gdk.Pixbuf GetLicenseThumbnail(string photoid) {
		  return null;
		}
		
		/*
		 * Methods to be used by editor.
		 */
		public void SavePhoto(Photo p) {
		  DeletePhoto(p.Id);
		  InsertPhoto(p);
		  SetPhotoDirty(p.Id, true);
		}
		
		public void SaveAlbum(Album a) {
		  DeleteAlbum(a.SetId);
		  InsertAlbum(a);
		  SetAlbumDirty(a.SetId, true);
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
	}
