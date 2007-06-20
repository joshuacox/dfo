
using System;
using System.Collections;

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
	}