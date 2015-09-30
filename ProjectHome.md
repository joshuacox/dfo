NOTE: This project is no longer under active development, please contact the administrator if you're interested in taking it over.


## About ##
The application allows online/offline mode management of your photos. You can upload, search and download photos, through the easy to use interface.

[![](http://farm2.static.flickr.com/1340/621022325_f0a6662489_d.jpg)](http://www.flickr.com/photos/tuxmann/sets/72157600480365381)

## Why should you be using it? ##
Web based organizer's are moderately slow, updates take time, you can only use it when you're online. When you have thousands of photos to organize, the time adds up. DFO on the other hand, stores all the updates offline, and with a simple and intuitive graphical interface, allows you to be way more productive. You'll never dread having to organize sets, edit titles, descriptions and tags for the hundreds or thousands of photos you've accumulated. And you can do all this editing, when you're just trying to kill your time in train, or that long plane trip.

## What all does it provide? ##
  1. Uploading and downloading of photos. You can download selected photos or the entire sets.
  1. Edit information attached to photos; delete photos from stream.
  1. Add/Remove tags associated with photos.
  1. Create new sets, edit set information, add/remove photos from sets, delete sets.
  1. New in v0.4 Add/Remove photos from Group Pools.
  1. **New in v0.7** Add/Delete/Edit comments. Text search comments and their author names.
  1. **New in v0.7** Post photos to blogs.
  1. **New in v0.7** Easy Drag-n-drop photos from nautilus for uploading.
  1. **New in v0.7** Image preview in file chooser dialog, shown when uploading photos.
  1. **New in v0.7** Edit title, description, privacy and tags of photos set for uploading.
  1. **New in v0.7** Allow reverting of edits done to photo.

## Important note regarding v0.7 ##
For all the existing users of DFO, to view the comments of your photos, you'd need to delete sqlite.db file located in $HOMEDIR/.desktopflickr, and do a sync after that. DFO will repopulate photo information, and also retrieve the corresponding comments. This doesn't apply to the new users of DFO.

## Important note regarding v0.8 ##

Fixes released in this version:
  1. Instead of storing all the thumbnails and small images in a single directory, they're now sharded in subdirectories, hence avoiding the 'too-many-files-in-one-directory' issue.
  1. The conflict resolution is only done when there're noticeable changes to the images, hence fixing [issue 13](https://code.google.com/p/dfo/issues/detail?id=13).
  1. File extension is now extracted from information provided by flickr server, instead of defaulting to JPG.
  1. DFO crash upon entering apostrophe s ('s) in set name.

If you're upgrading from v0.7, **please delete .desktopflickr directory** located in $HOME\_DIR. This would prompt DFO to re-download the thumbnails and small images to a new directory structure.

## Installation ##
Currently, DFO is only available for Gnome. To install it on Ubuntu Feisty Fawn, follow these instructions:
### Command Line ###
  1. sudo apt-get install mono libmono-sqlite2.0-cil libgconf2.0-cil gtk-sharp2
  1. Download Desktop Flickr Organizer files from the 'Downloads' section, and execute 'sh run.sh'. That's it!

### Graphical Interface ###
  1. Fire up Synaptics.
  1. Install Mono CLI (.NET) runtime. The current latest version is v1.2.3.1
  1. Install Mono Sqlite library version 2.0. The package version is v1.2.3.1
  1. Install CLI binding for GConf 2.16. The package version is v2.16.0
  1. Install CLI binding for Gtk# 2.10. The current package version is v2.10.0
  1. Download Desktop Flickr Organizer files from the 'Downloads' section, and execute 'sh run.sh'.

Other distros would also work the same way, once you have installed the Mono libraries.

Note: DFO has been included in debian packages, so to install it in debian, all you need to do is to run 'sudo apt-get install dfo'.

## Known Issues ##
  1. DFO doesn't show the photos uploaded, while I can see them on the flickr web interface.
The reason they don't show up is instantaneously is because of flickr apis' responses slowness. They take easily upto half an hour to catch up with updates. Especially new uploaded photos can even take more than that, to show up in the 'retrieve stream' api. The edits done to photos, may also show similar glitches, but give it enough time, and it would show you the expected results. If you can see it on flickr web interface, DFO _will_ catch up with the changes.

(c) Manish Rai Jain, 2007