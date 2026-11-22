This is a work in progress.

Built on .NET 10 + Windows Forms.  (.NET 10 either requires users to install .NET 10 or the self-contained executable (not requiring .NET 10) is 104MB vs 288KB)(A consideration for downporting to .NET 4.8.1 which comes with every Windows 11 installation)

Junctions (Folders with ReparsePoints) are ignored (primarily for the possibility of infinite recursion during directory scan).  That includes virtual drives and symlinked folders, though cloud-synced folders should be supported.

System files and folders are ignored (Mostly because Recycle Bin and other system folders interfere with directory scan, might be worth revisiting if selection within System folders is necessary).

This program is built to be portable only.  If you place it in a folder which requires admin privileges to write to (Program Files, etc.), settings will not be saved.

The Log box (FileDetails_listView) has a limit of 1000 entries.  After the limit is reached, new entries push out the oldest entries.

Last Accessed Time as a Windows property is very fragile: opening File Properties pings it, mousing over a video or zip file too long pings it, it can be turned off and is off by default in some Windows editions, updating timestamps can be delayed up to 1 hour.  Despite all this, it is natively supported in Windows and mostly works, so I use it here.

Layout inspired by Random File Picker by Marcin Szeniak.