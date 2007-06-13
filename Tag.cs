
using System;

  public class Tag : IComparable
	{
	  private string name;
	  private int numpics;
		
		public Tag(string name, int numpics)
		{
		  this.name = name;
		  this.numpics = numpics;
		}
		
		public string Name {
		  get {
		    return name;
		  }
		}
		
		public int NumberOfPics {
		  get {
		    return numpics;
		  }
		}
		
		int IComparable.CompareTo(object x) {
		  Tag tx = (Tag) x;
		  return name.CompareTo(tx.Name);
		}
	}
