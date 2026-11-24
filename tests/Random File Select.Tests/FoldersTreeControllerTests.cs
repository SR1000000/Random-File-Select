using System.Collections.Immutable;

namespace Random_File_Select.Tests;

public class FoldersTreeControllerTests
{
    [Fact]
    public void ShouldPopulateListView_subfoldersOff_requiresOwnFiles()
    {
        var childFile = new RetainedFile("a.txt", default);
        var child = new FolderNode("child", @"C:\child", [], ImmutableArray.Create(childFile), SubtreeFileCount: 1);
        var parent = new FolderNode("parent", @"C:\parent", [child], ImmutableArray<RetainedFile>.Empty, SubtreeFileCount: 1);

        Assert.False(FoldersTreeController.ShouldPopulateListView(parent, includeSubfolders: false));
        Assert.True(FoldersTreeController.ShouldPopulateListView(child, includeSubfolders: false));
    }

    [Fact]
    public void ShouldPopulateListView_subfoldersOn_usesSubtreeCount()
    {
        var childFile = new RetainedFile("a.txt", default);
        var child = new FolderNode("child", @"C:\child", [], ImmutableArray.Create(childFile), SubtreeFileCount: 1);
        var parent = new FolderNode("parent", @"C:\parent", [child], ImmutableArray<RetainedFile>.Empty, SubtreeFileCount: 1);
        var empty = new FolderNode("empty", @"C:\empty", [], ImmutableArray<RetainedFile>.Empty, SubtreeFileCount: 0);

        Assert.True(FoldersTreeController.ShouldPopulateListView(parent, includeSubfolders: true));
        Assert.False(FoldersTreeController.ShouldPopulateListView(empty, includeSubfolders: true));
    }
}