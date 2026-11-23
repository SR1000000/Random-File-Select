# Random File Select

You have a collection of files that you want to visit randomly, because surprise is the spice of life.  Your collection has a backlog of files that haven't been visited lately, that you want to clear by visiting them randomly in a reasonable timeframe.  This program addresses both of these needs, allowing you to clear out your backlog while still visiting randomly, and works on any filetype extensions you choose. 

## Usage



This program is built to be portable only.  If you place it in a folder which requires admin privileges to write to (Program Files, etc.), settings will not be saved.

## Notes
Built on .NET 10 + Windows Forms, in Visual Studio 2026.  .NET 10 mainly used over .NET 4.8.1 for system-supported Dark mode support. 
(.NET 10 either requires users to install .NET 10 or the self-contained executable (not requiring .NET 10) is 114MB vs 288KB)(A consideration for downporting to .NET 4.8.1 which comes with every Windows 11 installation)

Junctions (Folders with ReparsePoints) with Link Targets are ignored (primarily for the possibility of infinite recursion or double-countingduring directory scan).  That includes virtual drives and symlinked folders, though cloud-synced folders don't have link targets and should be supported.

System files and folders are ignored (Mostly because Recycle Bin and other system folders interfere with directory scan, might be worth revisiting if selection within System folders is necessary).

The Log box (FileDetails_listView) has a limit of 1000 entries.  After the limit is reached, new entries push out the oldest entries.

Randomness weighted by Last Accessed Time done through Best-of-k tournament-style sampling (where best = oldest).  More efficient than Rank-based Exponential (no sorting needed) while approximating it decently.  K = 25 at the highest setting (either end of the trackbar), which resolves old file backlogs relatively quickly.

Last Accessed Time as a Windows property is very fragile: opening File Properties pings it, mousing over a video or zip file too long pings it, it can be turned off and is off by default in some Windows editions, updating timestamps can be delayed up to 1 hour.  Despite all this, it is natively supported in Windows and mostly works, so I use it here.

Layout inspired by Random File Picker by Marcin Szeniak.