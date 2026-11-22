using System.Collections.Immutable;

namespace Random_File_Select.Tests;

public class FilesViewControllerTests
{
    [Fact]
    public void BuildItems_tagsItemsWithSharedRetainedFileReference()
    {
        var retained = new RetainedFile("a.txt", DateTime.UnixEpoch, DateTime.UnixEpoch, 1);
        var folder = new FolderNode(
            "dir",
            @"C:\dir",
            [],
            ImmutableArray.Create(retained),
            SubtreeFileCount: 1);

        ListViewItem[] items = FilesViewController.BuildItems(folder, includeSubfolders: false);

        Assert.Single(items);
        var entry = Assert.IsType<FileListEntry>(items[0].Tag);
        Assert.Same(retained, entry.File);
        Assert.Equal(Path.Combine(@"C:\dir", "a.txt"), entry.FullPath);
    }

    [Fact]
    public void BuildItems_includeSubfolders_collectsDescendantFiles()
    {
        var childFile = new RetainedFile("nested.txt", default, default, 1);
        var child = new FolderNode("sub", @"C:\parent\sub", [], ImmutableArray.Create(childFile), SubtreeFileCount: 1);
        var parent = new FolderNode("parent", @"C:\parent", [child], ImmutableArray<RetainedFile>.Empty, SubtreeFileCount: 1);

        ListViewItem[] items = FilesViewController.BuildItems(parent, includeSubfolders: true);

        Assert.Single(items);
        Assert.Equal("nested.txt", items[0].Text);
        Assert.Equal(Path.Combine(@"C:\parent\sub", "nested.txt"), ((FileListEntry)items[0].Tag!).FullPath);
    }
}