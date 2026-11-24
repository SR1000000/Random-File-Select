using System.Collections.Immutable;
using System.Globalization;

namespace Random_File_Select.Tests;

public class FoldersTreeTests : IDisposable
{
    private readonly string _temp;
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public FoldersTreeTests()
    {
        // Formatter output follows the current culture; pin it so the expected strings are locale-independent.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        _temp = Path.Combine(Path.GetTempPath(), "RandomFileSelectTree_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temp);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        if (Directory.Exists(_temp))
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(_temp, "*", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(path, FileAttributes.Normal); } catch (IOException) { }
            }

            Directory.Delete(_temp, recursive: true);
        }
    }

    // ---------- helpers ----------

    private string Dir(params string[] parts)
    {
        string path = Path.Combine([_temp, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Touch(string directory, string name, DateTime? lastWrite = null)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "x");
        if (lastWrite.HasValue)
            File.SetLastWriteTime(path, lastWrite.Value);
        return path;
    }

    private static FileFilter Txt() => new("txt", null, null, null, null);

    private static FolderScanResult Scan(FileFilter filter, params string[] roots) =>
        FolderTreeBuilder.Scan(roots, filter, CancellationToken.None);

    private static FolderScanResult ScanSub(FileFilter filter, params string[] roots) =>
        FolderTreeBuilder.Scan(roots, filter, CancellationToken.None, includeSubfolders: true);

    private static FolderScanResult Result(
        int matching = 1, int processed = 1, int roots = 1,
        int inaccessible = 0, int link = 0, int missing = 0, double seconds = 1.0) =>
        new([], matching, processed, roots, new SkippedCounts(inaccessible, link, missing),
            TimeSpan.FromSeconds(seconds));

    // ---------- FileFilter ----------

    [Theory]
    [InlineData("txt", "a.txt", true)]
    [InlineData(".TXT", "a.txt", true)]
    [InlineData("mkv mp4", "a.MP4", true)]
    [InlineData("txt", "a.doc", false)]
    [InlineData("txt", "noext", false)]
    [InlineData("", "a.doc", true)]
    [InlineData("   ", "a.doc", true)]
    [InlineData("  .  ", "a.doc", true)]
    public void Extensions_parseAndMatch(string text, string name, bool expected)
    {
        var filter = new FileFilter(text, null, null, null, null);
        Assert.Equal(expected, filter.Matches(name, DateTime.Now));
    }

    [Fact]
    public void HasExtensionRestriction_reflectsText()
    {
        Assert.False(new FileFilter(null, null, null, null, null).HasExtensionRestriction);
        Assert.False(new FileFilter("  ", null, null, null, null).HasExtensionRestriction);
        Assert.True(new FileFilter("txt", null, null, null, null).HasExtensionRestriction);
    }

    [Fact]
    public void Include_anyTermMatches_caseInsensitive_tabsAndSpaces()
    {
        var filter = new FileFilter(null, "foo \t  BAR", null, null, null);
        Assert.True(filter.Matches("my-foo.txt", DateTime.Now));
        Assert.True(filter.Matches("bar.txt", DateTime.Now));
        Assert.False(filter.Matches("baz.txt", DateTime.Now));
    }

    [Fact]
    public void Exclude_overridesInclude_andBlankTermsIgnored()
    {
        var filter = new FileFilter(null, "foo", "  draft  ", null, null);
        Assert.True(filter.Matches("foo.txt", DateTime.Now));
        Assert.False(filter.Matches("foo-draft.txt", DateTime.Now));
        Assert.True(new FileFilter(null, "  ", "   ", null, null).Matches("anything", DateTime.Now));
    }

    [Fact]
    public void Dates_nullBoundsImposeNoRestriction()
    {
        var filter = new FileFilter(null, null, null, null, null);
        Assert.True(filter.Matches("a", DateTime.MinValue.AddDays(1)));
        Assert.True(filter.Matches("a", DateTime.MaxValue.AddDays(-2)));
    }

    [Fact]
    public void Dates_inclusiveCalendarDayBoundaries()
    {
        var min = new DateTime(2024, 5, 10, 23, 59, 0);
        var max = new DateTime(2024, 5, 12, 0, 1, 0);
        var filter = new FileFilter(null, null, null, min, max);

        Assert.False(filter.Matches("a", new DateTime(2024, 5, 9, 23, 59, 59)));
        Assert.True(filter.Matches("a", new DateTime(2024, 5, 10, 0, 0, 0)));
        Assert.True(filter.Matches("a", new DateTime(2024, 5, 12, 23, 59, 59)));
        Assert.False(filter.Matches("a", new DateTime(2024, 5, 13, 0, 0, 0)));
    }

    [Fact]
    public void Dates_minOnlyAndMaxOnlyBoundOneSide()
    {
        var day = new DateTime(2024, 5, 10);
        var minOnly = new FileFilter(null, null, null, day, null);
        var maxOnly = new FileFilter(null, null, null, null, day);

        Assert.True(minOnly.Matches("a", day.AddYears(5)));
        Assert.False(minOnly.Matches("a", day.AddSeconds(-1)));
        Assert.True(maxOnly.Matches("a", day.AddYears(-5)));
        Assert.False(maxOnly.Matches("a", day.AddDays(1)));
    }

    [Fact]
    public void Dates_minAfterMax_clampsToSingleDay()
    {
        var filter = new FileFilter(null, null, null, new DateTime(2024, 6, 20), new DateTime(2024, 6, 10));

        Assert.True(filter.Matches("a", new DateTime(2024, 6, 10, 13, 0, 0)));
        Assert.False(filter.Matches("a", new DateTime(2024, 6, 9, 23, 0, 0)));
        Assert.False(filter.Matches("a", new DateTime(2024, 6, 11, 0, 0, 0)));
    }

    [Fact]
    public void FromSettings_combinesAllParts()
    {
        FileFilter filter = FileFilter.FromSettings(new FilterSettings
        {
            ExtensionFilterText = "txt",
            Include = "keep",
            Exclude = "tmp"
        });

        Assert.True(filter.Matches("keep.txt", DateTime.Now));
        Assert.False(filter.Matches("keep.doc", DateTime.Now));
        Assert.False(filter.Matches("keep-tmp.txt", DateTime.Now));
        Assert.False(filter.Matches("other.txt", DateTime.Now));
    }

    // ---------- nested roots ----------

    [Fact]
    public void PartitionRoots_dedupesExact_splitsCoveringAndNested_keepsOrder()
    {
        (IReadOnlyList<string> ordered, IReadOnlyList<string> covering, HashSet<string> nested) =
            FolderTreeBuilder.PartitionRoots(
                [@"D:\Music\Rock", @"D:\Video", @"D:\Music", @"D:\Music2", @"D:\video\sub", @"D:\VIDEO", @"D:\Music\Pop\"]);

        Assert.Equal(
            [@"D:\Music\Rock", @"D:\Video", @"D:\Music", @"D:\Music2", @"D:\video\sub", @"D:\Music\Pop"], ordered);
        Assert.Equal([@"D:\Video", @"D:\Music", @"D:\Music2"], covering);
        Assert.Equal(3, nested.Count);
        Assert.Contains(@"D:\Music\Rock", nested);
        Assert.Contains(@"D:\video\sub", nested);
        Assert.Contains(@"D:\Music\Pop", nested);
    }

    [Theory]
    [InlineData(@"C:\Music", @"C:\", true)]
    [InlineData(@"C:\", @"C:\", false)]
    [InlineData(@"\\server\share\sub", @"\\server\share\", true)]
    [InlineData(@"D:\Music2", @"D:\Music", false)]
    [InlineData(@"D:\music\a", @"D:\Music", true)]
    public void IsUnder_handlesDriveUncAndPrefixes(string child, string parent, bool expected)
    {
        Assert.Equal(expected, FolderTreeBuilder.IsUnder(child, parent));
    }

    [Fact]
    public void Scan_nestedRoot_scannedOnce_countsNotInflated()
    {
        string root = Dir("music");
        string nested = Dir("music", "rock");
        Touch(nested, "a.txt");

        FolderScanResult result = Scan(Txt(), root, nested);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(1, result.MatchingFolderCount);
        Assert.Equal(2, result.ProcessedCount);
        FolderNode nestedTop = result.Roots[1];
        Assert.Equal(1, nestedTop.SubtreeFileCount);
        FolderNode parentChild = Assert.Single(result.Roots[0].Children);
        Assert.NotSame(nestedTop, parentChild);
        Assert.Same(nestedTop.Children, parentChild.Children);
        Assert.True(nestedTop.Files == parentChild.Files);
    }

    [Fact]
    public void Scan_childRootListedBeforeParent_followsInputOrder()
    {
        string root = Dir("music");
        string nested = Dir("music", "rock");
        Touch(nested, "a.txt");

        FolderScanResult result = Scan(Txt(), nested, root);

        Assert.Equal([nested, root], result.Roots.Select(r => r.FullPath));
    }

    [Fact]
    public void Scan_grandchildRoots_allShown_countsPhysical()
    {
        string a = Dir("a");
        string b = Dir("a", "b");
        string c = Dir("a", "b", "c");
        Touch(a, "1.txt");
        Touch(b, "2.txt");
        Touch(c, "3.txt");

        FolderScanResult result = Scan(Txt(), a, b, c);

        Assert.Equal(3, result.Roots.Count);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(3, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_emptyNestedRoot_stillShown()
    {
        string a = Dir("a");
        Touch(a, "1.txt");
        string empty = Dir("a", "empty");

        FolderScanResult result = Scan(Txt(), a, empty);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(0, result.Roots[1].SubtreeFileCount);
    }

    [Fact]
    public void Scan_nestedRootWithoutOwnFiles_childrenRelabelledPerView()
    {
        string a = Dir("a");
        string b = Dir("a", "b");
        string c = Dir("a", "b", "c");
        Touch(c, "x.txt");

        FolderScanResult result = Scan(Txt(), a, b);

        FolderNode top = result.Roots[0];
        FolderNode nestedTop = result.Roots[1];
        Assert.Equal(b, nestedTop.DisplayName);
        Assert.Equal("c", Assert.Single(nestedTop.Children).DisplayName);
        Assert.Equal(Path.Combine("b", "c"), Assert.Single(top.Children).DisplayName);
        Assert.Equal(1, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_trailingSeparatorRoot_notRescanned()
    {
        string a = Dir("a");
        string b = Dir("a", "b");
        Touch(b, "x.txt");

        FolderScanResult result = Scan(Txt(), a, b + Path.DirectorySeparatorChar);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(2, result.ProcessedCount);
    }

    [Fact]
    public void Scan_nestedRootUnderSystemFolder_fallbackScans()
    {
        string a = Dir("a");
        string sys = Dir("a", "Sys");
        string n = Dir("a", "Sys", "N");
        Touch(n, "x.txt");
        File.SetAttributes(sys, FileAttributes.System | FileAttributes.Directory);

        FolderScanResult result = Scan(Txt(), a, n);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(1, result.Roots[1].SubtreeFileCount);
        Assert.Equal(0, result.Roots[0].SubtreeFileCount);
    }

    [Fact]
    public void Scan_fallbackNestedGrandchildren_notScannedTwice()
    {
        string a = Dir("a");
        string sys = Dir("a", "Sys");
        string b = Dir("a", "Sys", "B");
        string c = Dir("a", "Sys", "B", "C");
        Touch(b, "1.txt");
        Touch(c, "2.txt");
        File.SetAttributes(sys, FileAttributes.System | FileAttributes.Directory);

        FolderScanResult result = Scan(Txt(), a, b, c);

        Assert.Equal(3, result.Roots.Count);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(2, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_nestedRootUnderLink_fallbackScans()
    {
        string a = Dir("a");
        string target = Dir("target");
        string n = Dir("target", "N");
        Touch(n, "x.txt");
        string link = Path.Combine(a, "link");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }

        FolderScanResult result = Scan(Txt(), a, Path.Combine(link, "N"));

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(1, result.Skipped.Link);
    }

    [Fact]
    public void Scan_nestedRootThatIsLink_notRetried()
    {
        string a = Dir("a");
        string target = Dir("target");
        Touch(target, "x.txt");
        string link = Path.Combine(a, "L");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }

        FolderScanResult result = Scan(Txt(), a, link);

        Assert.Single(result.Roots);
        Assert.Equal(1, result.Skipped.Link);
    }

    [Fact]
    public void Scan_missingCoveringRoot_bothSkipped()
    {
        string missing = Path.Combine(_temp, "missing");

        FolderScanResult result = Scan(Txt(), missing, Path.Combine(missing, "sub"));

        Assert.Empty(result.Roots);
        Assert.Equal(2, result.Skipped.Missing);
    }

    // ---------- tree shape ----------

    [Fact]
    public void Scan_strictInclusion_ancestorWithoutMatchesHidden_descendantAttachesToRoot()
    {
        string root = Dir("root");
        Touch(root, "top.txt");
        string deep = Dir("root", "a", "b");
        Touch(deep, "deep.txt");

        FolderScanResult result = Scan(Txt(), root);

        FolderNode rootNode = Assert.Single(result.Roots);
        Assert.Equal(root, rootNode.DisplayName);
        FolderNode child = Assert.Single(rootNode.Children);
        Assert.Equal(deep, child.FullPath);
        Assert.Equal(Path.Combine("a", "b"), child.DisplayName);
        Assert.Equal(2, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_shownDescendantAttachesToNearestShownAncestor()
    {
        string root = Dir("root");
        string a = Dir("root", "a");
        Touch(a, "a.txt");
        string c = Dir("root", "a", "b", "c");
        Touch(c, "c.txt");

        FolderScanResult result = Scan(Txt(), root);

        FolderNode rootNode = result.Roots[0];
        FolderNode aNode = Assert.Single(rootNode.Children);
        Assert.Equal("a", aNode.DisplayName);
        FolderNode cNode = Assert.Single(aNode.Children);
        Assert.Equal(Path.Combine("b", "c"), cNode.DisplayName);
        Assert.Equal(2, rootNode.SubtreeFileCount);
    }

    [Fact]
    public void Scan_includeSubfolders_branchingIntermediateKept_singleBranchFlattened()
    {
        string root = Dir("root");
        Touch(Dir("root", "branch", "x"), "x.txt");
        Touch(Dir("root", "branch", "y"), "y.txt");
        Touch(Dir("root", "single", "only"), "o.txt");
        Dir("root", "single", "empty");

        FolderNode on = Assert.Single(ScanSub(Txt(), root).Roots);
        Assert.Equal(["branch", Path.Combine("single", "only")], on.Children.Select(c => c.DisplayName));
        FolderNode branch = on.Children[0];
        Assert.Equal(0, branch.OwnFileCount);
        Assert.Equal(2, branch.SubtreeFileCount);
        Assert.Equal(["x", "y"], branch.Children.Select(c => c.DisplayName));
        Assert.Equal(3, ScanSub(Txt(), root).MatchingFolderCount);

        FolderNode off = Assert.Single(Scan(Txt(), root).Roots);
        Assert.Equal(
            [Path.Combine("branch", "x"), Path.Combine("branch", "y"), Path.Combine("single", "only")],
            off.Children.Select(c => c.DisplayName));
    }

    [Fact]
    public void Scan_includeSubfolders_chainAboveBranchCollapsesToRelativePath()
    {
        string root = Dir("root");
        Touch(Dir("root", "a", "b", "x"), "x.txt");
        Touch(Dir("root", "a", "b", "y"), "y.txt");

        FolderNode b = Assert.Single(Assert.Single(ScanSub(Txt(), root).Roots).Children);
        Assert.Equal(Path.Combine("a", "b"), b.DisplayName);
        Assert.Equal(["x", "y"], b.Children.Select(c => c.DisplayName));
    }

    [Fact]
    public void Scan_includeSubfolders_branchWithFlattenedChild()
    {
        string root = Dir("root");
        Touch(Dir("root", "branch", "left", "deep"), "d.txt");
        Touch(Dir("root", "branch", "right"), "r.txt");

        FolderNode branch = Assert.Single(Assert.Single(ScanSub(Txt(), root).Roots).Children);
        Assert.Equal("branch", branch.DisplayName);
        Assert.Equal([Path.Combine("left", "deep"), "right"], branch.Children.Select(c => c.DisplayName));
    }

    [Fact]
    public void Scan_includeSubfolders_nestedBranchingRoot_scannedOnce()
    {
        string root = Dir("root");
        string branch = Dir("root", "branch");
        Touch(Dir("root", "branch", "x"), "x.txt");
        Touch(Dir("root", "branch", "y"), "y.txt");

        FolderScanResult result = ScanSub(Txt(), root, branch);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(2, result.MatchingFolderCount);
        Assert.Equal(4, result.ProcessedCount);
        Assert.Equal(branch, result.Roots[1].DisplayName);
        Assert.Equal("branch", Assert.Single(result.Roots[0].Children).DisplayName);
    }

    [Fact]
    public void Scan_rootWithoutMatches_stillShownButNotCounted()
    {
        string empty = Dir("empty");
        string full = Dir("full");
        Touch(full, "a.txt");

        FolderScanResult result = Scan(Txt(), empty, full);

        Assert.Equal(2, result.Roots.Count);
        Assert.Equal(1, result.MatchingFolderCount);
        Assert.Equal(2, result.RootsScanned);
        Assert.Contains("1 folder with", FolderTreeStatusFormatter.FormatCompleted(result));
    }

    [Fact]
    public void Scan_skippedRoot_notInTree()
    {
        string good = Dir("good");
        string missing = Path.Combine(_temp, "missing");

        FolderScanResult result = Scan(Txt(), missing, good);

        Assert.Single(result.Roots);
        Assert.Equal(1, result.RootsScanned);
        Assert.Equal(1, result.Skipped.Missing);
        Assert.Equal(1, result.Skipped.Total);
        Assert.Equal(2, result.ProcessedCount);
    }

    [Fact]
    public void Scan_siblingsAreOrderedDeterministically()
    {
        string root = Dir("root");
        foreach (string name in new[] { "b", "A", "ä", "C" })
            Touch(Dir("root", name), "f.txt");

        FolderScanResult result = Scan(Txt(), root);

        Assert.Equal(["A", "b", "C", "ä"], result.Roots[0].Children.Select(c => c.DisplayName));
    }

    // ---------- retained files & counts ----------

    [Fact]
    public void Scan_retainsMatchingFilesWithMetadata_andCountsAreConsistent()
    {
        string root = Dir("root");
        var stamp = new DateTime(2023, 3, 4, 5, 6, 7);
        Touch(root, "b.txt", stamp);
        Touch(root, "a.txt", stamp);
        Touch(root, "skip.doc");
        string sub = Dir("root", "sub");
        Touch(sub, "c.txt");
        Dir("root", "nomatch");
        Touch(Path.Combine(root, "nomatch"), "x.doc");

        FolderScanResult result = Scan(Txt(), root);

        FolderNode node = result.Roots[0];
        Assert.Equal(["a.txt", "b.txt"], node.Files.Select(f => f.FileName));
        Assert.All(node.Files, f =>
        {
            Assert.Equal(stamp, f.LastWriteTime);
            Assert.Equal(1, f.Length);
        });
        Assert.Equal(2, node.OwnFileCount);
        Assert.Equal(3, node.SubtreeFileCount);
        Assert.Equal(node.OwnFileCount + node.Children.Sum(c => c.SubtreeFileCount), node.SubtreeFileCount);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(2, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_filterAppliesToRetainedFiles()
    {
        string root = Dir("root");
        Touch(root, "keep.txt");
        Touch(root, "drop.txt");

        FolderScanResult result = Scan(new FileFilter("txt", null, "drop", null, null), root);

        Assert.Equal(["keep.txt"], result.Roots[0].Files.Select(f => f.FileName));
    }

    // ---------- hidden & system ----------

    [Fact]
    public void Scan_hiddenFileAndFolder_areIncluded()
    {
        string root = Dir("root");
        string hiddenFile = Touch(root, "h.txt");
        File.SetAttributes(hiddenFile, FileAttributes.Hidden);
        string hiddenDir = Dir("root", "hid");
        Touch(hiddenDir, "in.txt");
        File.SetAttributes(hiddenDir, FileAttributes.Hidden | FileAttributes.Directory);

        FolderScanResult result = Scan(Txt(), root);

        Assert.Equal(["h.txt"], result.Roots[0].Files.Select(f => f.FileName));
        Assert.Single(result.Roots[0].Children);
    }

    [Fact]
    public void Scan_systemFileAndFolders_areIgnoredAndNotCounted()
    {
        string root = Dir("root");
        string systemFile = Touch(root, "s.txt");
        File.SetAttributes(systemFile, FileAttributes.System);
        string systemDir = Dir("root", "sys");
        Touch(systemDir, "in.txt");
        File.SetAttributes(systemDir, FileAttributes.System | FileAttributes.Directory);
        string hiddenSystemDir = Dir("root", "hs");
        Touch(hiddenSystemDir, "in.txt");
        File.SetAttributes(hiddenSystemDir, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory);

        FolderScanResult result = Scan(Txt(), root);

        Assert.Empty(result.Roots[0].Files);
        Assert.Empty(result.Roots[0].Children);
        Assert.Equal(0, result.Skipped.Total);
        Assert.Equal(1, result.ProcessedCount);
    }

    [Fact]
    public void Scan_hiddenRoot_isScannedNormally()
    {
        string root = Dir("hroot");
        Touch(root, "a.txt");
        File.SetAttributes(root, FileAttributes.Hidden | FileAttributes.Directory);

        FolderScanResult result = Scan(Txt(), root);

        Assert.Equal(1, result.MatchingFolderCount);
    }

    // ---------- cancellation, links, I/O ----------

    [Fact]
    public void Scan_preCancelledToken_throws()
    {
        string root = Dir("root");
        Touch(root, "a.txt");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => FolderTreeBuilder.Scan([root], Txt(), cts.Token));
    }

    [Fact]
    public void Scan_cancelledMidScan_throws()
    {
        string root = Dir("root");
        for (int i = 0; i < 5; i++)
            Touch(Dir("root", "d" + i), "a.txt");
        using var cts = new CancellationTokenSource();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            FolderTreeBuilder.Scan([root], Txt(), cts.Token, (count, _) =>
            {
                if (count >= 2)
                    cts.Cancel();
            }));
    }

    [Fact]
    public void Scan_progressReportsCountAndPath()
    {
        string root = Dir("root");
        Dir("root", "a");
        var seen = new List<(int Count, string Path)>();

        FolderTreeBuilder.Scan([root], Txt(), CancellationToken.None, (c, p) => seen.Add((c, p)));

        Assert.Equal([1, 2], seen.Select(s => s.Count));
        Assert.Equal(root, seen[0].Path);
    }

    [Fact]
    public void Scan_symbolicLinkDirectory_isSkippedNotTraversed()
    {
        string root = Dir("root");
        string target = Dir("target");
        Touch(target, "linked.txt");
        string link = Path.Combine(root, "link");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return; // Host cannot create links without privilege.
        }

        FolderScanResult result = Scan(Txt(), root);

        Assert.Equal(1, result.Skipped.Link);
        Assert.Empty(result.Roots[0].Children);
        Assert.Equal(0, result.MatchingFolderCount);
    }

    [Fact]
    public void Scan_symbolicLinkRoot_isSkippedAndNotShown()
    {
        string target = Dir("target");
        Touch(target, "linked.txt");
        string link = Path.Combine(_temp, "linkroot");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }

        FolderScanResult result = Scan(Txt(), link);

        Assert.Empty(result.Roots);
        Assert.Equal(1, result.Skipped.Link);
    }

    [Fact]
    public void Scan_skippedReasonsSumToTotal()
    {
        FolderScanResult result = Scan(Txt(), Path.Combine(_temp, "nope1"), Path.Combine(_temp, "nope2"));

        Assert.Equal(result.Skipped.Inaccessible + result.Skipped.Link + result.Skipped.Missing, result.Skipped.Total);
        Assert.Equal(2, result.Skipped.Total);
        Assert.Equal(2, result.ProcessedCount);
    }

    // ---------- unicode & special names ----------

    public static TheoryData<string> SpecialNames =>
    [
        "with space and (parens) [brackets] {braces}",
        "hash#percent%amp&apos'comma,semi;plus+eq=at@bang!tilde~caret^",
        "naïve café résumé",
        "e\u0301 combining acute",
        "Привет мир",
        "Ελληνικά φάκελος",
        "مرحبا بالعالم",
        "日本語フォルダ",
        "中文文件夹",
        "한국어 폴더",
        "😀 emoji 📁 \U0001F9EA",
        "𠜎 supplementary CJK",
        "ß ŉ ǅ İ ı",
        "dollar$ and `backtick`",
    ];

    [Theory]
    [MemberData(nameof(SpecialNames))]
    public void Scan_specialAndUnicodeNames_surviveExactly(string name)
    {
        string root = Dir("root");
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        string fileName = name + ".txt";
        Touch(folder, fileName);

        FolderScanResult result = Scan(Txt(), root);

        FolderNode child = Assert.Single(result.Roots[0].Children);
        Assert.Equal(name, child.DisplayName);
        Assert.Equal(folder, child.FullPath);
        Assert.True(Directory.Exists(child.FullPath));
        Assert.Equal(fileName, Assert.Single(child.Files).FileName);
    }

    // ---------- formatters ----------

    [Theory]
    [InlineData(0.8, "0.8 s")]
    [InlineData(59.9, "59.9 s")]
    [InlineData(60, "1 min 0 s")]
    [InlineData(134, "2 min 14 s")]
    public void FormatElapsed_boundaries(double seconds, string expected)
    {
        Assert.Equal(expected, FolderTreeStatusFormatter.FormatElapsed(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void FormatCompleted_withMatches()
    {
        string text = FolderTreeStatusFormatter.FormatCompleted(
            Result(matching: 1284, inaccessible: 2, link: 1, seconds: 2.4));

        Assert.Equal("Loaded 1,284 folders with matching files (3 skipped) in 2.4 s", text);
    }

    [Fact]
    public void FormatCompleted_singularFolder_zeroSkippedShown()
    {
        string text = FolderTreeStatusFormatter.FormatCompleted(Result(matching: 1, seconds: 0.8));

        Assert.Equal("Loaded 1 folder with matching files (0 skipped) in 0.8 s", text);
    }

    [Fact]
    public void FormatCompleted_noMergedText()
    {
        string text = FolderTreeStatusFormatter.FormatCompleted(Result(matching: 5, missing: 3));

        Assert.DoesNotContain("merged", text);
    }

    [Fact]
    public void FormatCompleted_noMatches()
    {
        string text = FolderTreeStatusFormatter.FormatCompleted(
            Result(matching: 0, roots: 2, missing: 1, seconds: 0.5));

        Assert.Equal("No folders contain matching files (2 roots scanned, 1 skipped) in 0.5 s", text);
    }

    [Fact]
    public void FormatCancelledAndFaulted()
    {
        Assert.Equal("Load cancelled after 2 min 14 s",
            FolderTreeStatusFormatter.FormatCancelled(TimeSpan.FromSeconds(134)));
        Assert.Equal("Load failed: line one line two",
            FolderTreeStatusFormatter.FormatFaulted(new InvalidOperationException("line one\r\nline   two")));
    }

    [Fact]
    public void FormatTooltip_containsBreakdown()
    {
        string tip = FolderTreeStatusFormatter.FormatTooltip(
            Result(processed: 1500, roots: 2, inaccessible: 1, link: 2, missing: 3, seconds: 2.4));

        Assert.Contains("Folders processed: 1,500", tip);
        Assert.Contains("Skipped: 6", tip);
        Assert.Contains("Inaccessible: 1", tip);
        Assert.Contains("Link or junction: 2", tip);
        Assert.Contains("Missing: 3", tip);
        Assert.Contains("Roots scanned: 2", tip);
        Assert.DoesNotContain("merged", tip);
        Assert.Contains("Elapsed: 2.4 s", tip);
    }
}
