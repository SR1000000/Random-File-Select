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
}