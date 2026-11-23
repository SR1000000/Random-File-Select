using System.Collections.Immutable;

namespace Random_File_Select.Tests;

public class FilesViewControllerTests
{
    [Fact]
    public void BuildRows_sharesRetainedFileReference()
    {
        var retained = new RetainedFile("a.txt", DateTime.UnixEpoch, DateTime.UnixEpoch, 1);
        var folder = new FolderNode(
            "dir",
            @"C:\dir",
            [],
            ImmutableArray.Create(retained),
            SubtreeFileCount: 1);

        FileRow[] rows = FilesViewController.BuildRows(folder, includeSubfolders: false);

        Assert.Single(rows);
        Assert.Same(retained, rows[0].File);
        Assert.Equal(@"C:\dir", rows[0].FolderPath);
        Assert.Equal(Path.Combine(@"C:\dir", "a.txt"), rows[0].FullPath);
    }

    [Fact]
    public void BuildRows_includeSubfolders_collectsDescendantFiles()
    {
        var childFile = new RetainedFile("nested.txt", default, default, 1);
        var child = new FolderNode("sub", @"C:\parent\sub", [], ImmutableArray.Create(childFile), SubtreeFileCount: 1);
        var parent = new FolderNode("parent", @"C:\parent", [child], ImmutableArray<RetainedFile>.Empty, SubtreeFileCount: 1);

        FileRow[] rows = FilesViewController.BuildRows(parent, includeSubfolders: true);

        Assert.Single(rows);
        Assert.Equal("nested.txt", rows[0].File.FileName);
        Assert.Equal(Path.Combine(@"C:\parent\sub", "nested.txt"), rows[0].FullPath);
    }

    [Fact]
    public void BuildRows_includeSubfolders_ordersOwnFilesBeforeChildFiles()
    {
        var own = new RetainedFile("own.txt", default, default, 1);
        var childFile = new RetainedFile("child.txt", default, default, 1);
        var grandFile = new RetainedFile("grand.txt", default, default, 1);
        var grand = new FolderNode("g", @"C:\p\c\g", [], ImmutableArray.Create(grandFile), SubtreeFileCount: 1);
        var child = new FolderNode("c", @"C:\p\c", [grand], ImmutableArray.Create(childFile), SubtreeFileCount: 2);
        var parent = new FolderNode("p", @"C:\p", [child], ImmutableArray.Create(own), SubtreeFileCount: 3);

        FileRow[] rows = FilesViewController.BuildRows(parent, includeSubfolders: true);

        Assert.Equal(["own.txt", "child.txt", "grand.txt"], rows.Select(r => r.File.FileName));
    }
}
