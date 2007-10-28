
using System;
using System.Collections;
using System.Text.RegularExpressions;

	public class Utils
	{
		public static string GetDelimitedString(ArrayList list, string delimiter) {
		  return String.Join(delimiter, (string[]) list.ToArray(typeof(string)));
		}
		
		public static ArrayList GetIntersection(ArrayList src, ArrayList dst) {
		  ArrayList intersectedlist = new ArrayList();
		  foreach (object s in src) {
		    if (dst.Contains(s)) intersectedlist.Add(s);
		  }
		  return intersectedlist;
		}
		
		// Just for string arrays.
		public static bool IsStringArrayEqual(ArrayList a1, ArrayList a2) {
		  if (a1.Count != a2.Count) return false;
		  foreach (string str in a1) {
		    if (!a2.Contains(str)) return false;
		  }
		  return true;
		}
		
		public static ArrayList ParseTagsFromString(string tagstring) {
		  if (tagstring.Contains("\"")) return null;
		  
		  string[] tokens = tagstring.Split(' ');
		  ArrayList tags = new ArrayList();
		  foreach (string token in tokens) {
		    if (!token.Trim().Equals("")) {
		      tags.Add(token.Trim());
		    }
		  }
		  return tags;
		}
		
    public static void EnsureDirectoryExists(string dir) {
      System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir);
		  if (!dirInfo.Exists) {
		    dirInfo.Create();
		    Console.WriteLine("Created directory: " + dir);
		  }
    }
    
    public static bool FileNameExists(string filename) {
      System.IO.FileInfo fileinfo = new System.IO.FileInfo(filename);
      return fileinfo.Exists;
    }
    
    public static void IfExistsDeleteFile(string filename) {
      System.IO.FileInfo fileinfo = new System.IO.FileInfo(filename);
      if (fileinfo.Exists) {
        fileinfo.Delete();
      }
    }
    
    public static string EscapeForPango(string str) {
      /*
			if (str == null)
				return "";
		  Regex r = new Regex("<[^>]+>");
		  str = r.Replace(str, "");
			str = str.Replace ("&", "&amp;");
			str = str.Replace ("<", "&lt;");
			return str;
			*/
			return GLib.Markup.EscapeText(str);
    }
    
    public static bool isImageFile(string filepath) {
      System.IO.FileInfo finfo = new System.IO.FileInfo(filepath);
      if (!finfo.Exists) return false;
      System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase;
      if (finfo.Extension.IndexOf("jpg", comp) > -1) return true;
      if (finfo.Extension.IndexOf("png", comp) > -1) return true;
      return false;
		}
	}
