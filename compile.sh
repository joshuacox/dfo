#!/bin/bash
gmcs -pkg:glade-sharp-2.0 -pkg:gconf-sharp-2.0 -r:lib/FlickrNet.dll -r:System.Data -r:Mono.Data.SqliteClient.dll -resource:glade/organizer.glade Main.cs *.cs
