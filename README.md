This is a work in progress.

Built on .NET 10 + Windows Forms

Junctions (Folders with ReparsePoints) are ignored.  That includes virtual drives and symlinked folders.

System files and folders are ignored.

This program is built to be portable only.  Do not place it in a folder which requires admin privileges to write to (Program Files, etc.) or settings will not be saved.

Last Access is so sensitive, opening File Properties pings it, mousing over a video or zip file too long pings it.

Layout inspired by Random File Picker by Marcin Szeniak