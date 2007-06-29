#!/bin/sh
dfodir=`dirname $0`
MONO_PATH=$dfodir/lib:$MONO_PATH exec /usr/bin/mono $dfodir/Main.exe