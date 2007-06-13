// project created on 6/8/2007 at 10:21 PM

using System;

public class GladeApp
{ 
/*
 * gmcs -pkg:glade-sharp-2.0 -pkg:gconf-sharp-2.0 -r:lib/FlickrNet.dll 
 * -resource:glade/organizer.glade Main.cs *.cs
 */
	public static void Main (string[] args)
	{
		DeskFlickrUI.GetInstance().CreateGUI();
	}
}