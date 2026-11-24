using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Random_File_Select;

/// <summary>Compact metadata for one matching file, captured from the directory listing and shared by reference between FolderNode and FileRow; LastAccessTime is updated when the app opens the file.</summary>
public sealed class RetainedFile(string fileName, DateTime lastAccessTime)
{
    public string FileName { get; } = fileName;
    public DateTime LastAccessTime { get; set; } = lastAccessTime;
}

/// <summary>A file shown in the files ListView: its containing folder's full path plus the RetainedFile instance shared with that FolderNode.</summary>
public readonly record struct FileRow(string FolderPath, RetainedFile File)
{
    public string FullPath => Path.Combine(FolderPath, File.FileName);
}

/// <summary>Shown folder: display label, true full path, shown descendants, and its own matching files (immutable except each RetainedFile.LastAccessTime, which the app updates).</summary>
public sealed record FolderNode(
    string DisplayName,
    string FullPath,
    IReadOnlyList<FolderNode> Children,
    ImmutableArray<RetainedFile> Files,
    int SubtreeFileCount)
{
    public int OwnFileCount => Files.Length;
}

/// <summary>Counts of skipped directories by reason; <see cref="Total"/> is their sum.</summary>
public sealed record SkippedCounts(int Inaccessible, int Link, int Missing)
{
    public int Total => Inaccessible + Link + Missing;
}

/// <summary>Outcome of a completed scan: root nodes plus exact counts and scan duration.</summary>
public sealed record FolderScanResult(
    IReadOnlyList<FolderNode> Roots,
    int MatchingFolderCount,
    int ProcessedCount,
    int RootsScanned,
    SkippedCounts Skipped,
    TimeSpan Elapsed);

/// <summary>Scans checked roots in one recursive pass into a tree of folders with matching files (plus branching intermediate folders when subfolders are included); never open or read file contents, only listing metadata, so Last Accessed (used for weighting) is never updated.</summary>
public static class FolderTreeBuilder
{
    /// <summary>Trims trailing separators, drops case-insensitive duplicates, and splits roots into input-ordered Ordered, Covering (not under another root), and Nested (under another root).</summary>
    public static (IReadOnlyList<string> Ordered, IReadOnlyList<string> Covering, HashSet<string> Nested) PartitionRoots(IEnumerable<string> roots)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (string raw in roots)
        {
            string path = Path.TrimEndingDirectorySeparator(raw);
            if (seen.Add(path))
                ordered.Add(path);
        }

        var covering = new List<string>();
        var nested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in ordered)
        {
            if (ordered.Any(other => IsUnder(path, other)))
                nested.Add(path);
            else
                covering.Add(path);
        }

        return (ordered, covering, nested);
    }

    /// <summary>True when child is strictly inside parent; handles drive/UNC roots that already end with a separator.</summary>
    public static bool IsUnder(string child, string parent)
    {
        string prefix = Path.EndsInDirectorySeparator(parent)
            ? parent
            : parent + Path.DirectorySeparatorChar;

        return child.Length > prefix.Length && child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Scans covering roots once (caching nested roots reached), scans unreached nested roots directly, and returns every root as its own top-level node in input order; when includeSubfolders is true, folders without own matches that branch into 2+ shown children keep a node; throws on cancellation.</summary>
    public static FolderScanResult Scan(
        IEnumerable<string> roots,
        FileFilter filter,
        CancellationToken cancellationToken,
        Action<int, string>? progress = null,
        bool includeSubfolders = false)
    {
        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<string> ordered, IReadOnlyList<string> covering, HashSet<string> nested) = PartitionRoots(roots);

        var scanner = new Scanner(filter, nested, cancellationToken, progress, includeSubfolders);

        foreach (string root in covering)
            scanner.ScanRoot(root);

        foreach (string root in nested.OrderBy(p => p.Length))
        {
            if (!scanner.WasReached(root))
                scanner.ScanRoot(root);
        }

        var rootNodes = new List<FolderNode>(ordered.Count);
        foreach (string root in ordered)
        {
            if (scanner.TryGetRootNode(root, out FolderNode? node))
                rootNodes.Add(node);
        }

        return new FolderScanResult(
            rootNodes,
            scanner.MatchingFolderCount,
            scanner.ProcessedCount,
            rootNodes.Count,
            new SkippedCounts(scanner.Inaccessible, scanner.Link, scanner.Missing),
            stopwatch.Elapsed);
    }

    /// <summary>Orders names by OrdinalIgnoreCase, then Ordinal, for culture-independent determinism.</summary>
    private static int CompareNames(string a, string b)
    {
        int result = StringComparer.OrdinalIgnoreCase.Compare(a, b);
        return result != 0 ? result : StringComparer.Ordinal.Compare(a, b);
    }

    private static bool IsSkippable(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or SecurityException
            or ArgumentException or NotSupportedException;

    /// <summary>True for a symbolic link or junction (reparse point with a link target); other reparse points are traversed.</summary>
    private static bool IsLink(DirectoryInfo directory) =>
        (directory.Attributes & FileAttributes.ReparsePoint) != 0 && directory.LinkTarget is not null;

    /// <summary>Mutable accumulator for files and child nodes collected during a single directory enumeration.</summary>
    private sealed record DirectoryScan(List<RetainedFile> Files, List<FolderNode> Nodes);

    /// <summary>Stateful worker that walks the directory tree, enforces filter and skip rules, decides which folders get nodes, and accumulates scan counters.</summary>
    private sealed class Scanner(FileFilter filter, HashSet<string> nestedRoots, CancellationToken token, Action<int, string>? progress, bool includeSubfolders)
    {
        /// <summary>Nested roots met while scanning a covering root, whether or not they ended up with a node.</summary>
        private readonly HashSet<string> _reachedNested = new(nestedRoots.Count, StringComparer.OrdinalIgnoreCase);

        /// <summary>Top-level node for every root (covering or nested) that was scanned successfully.</summary>
        private readonly Dictionary<string, FolderNode> _rootNodes = new(StringComparer.OrdinalIgnoreCase);

        public bool WasReached(string path) => _reachedNested.Contains(path);

        public bool TryGetRootNode(string path, [NotNullWhen(true)] out FolderNode? node) =>
            _rootNodes.TryGetValue(path, out node);

        private static readonly EnumerationOptions Options = new()
        {
            AttributesToSkip = FileAttributes.System,
            IgnoreInaccessible = false,
            RecurseSubdirectories = false
        };

        public int ProcessedCount { get; private set; }
        public int MatchingFolderCount { get; private set; }
        public int Inaccessible { get; private set; }
        public int Link { get; private set; }
        public int Missing { get; private set; }

        /// <summary>Scans one root and records its always-shown node in the root map; the root is skipped (and counted) when missing, a link, or inaccessible.</summary>
        public void ScanRoot(string path)
        {
            token.ThrowIfCancellationRequested();
            Visit(path);

            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(path);
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                Inaccessible++;
                return;
            }

            if (!TryEnter(directory))
                return;

            DirectoryScan? scan = ScanDirectory(directory);
            if (scan is not null)
                _rootNodes[path] = Wrap(directory.FullName, directory.FullName, scan);
        }

        /// <summary>Checks that a directory exists and is not a link, counting the skip reason otherwise; returns whether it may be scanned.</summary>
        private bool TryEnter(DirectoryInfo directory)
        {
            try
            {
                if (!directory.Exists)
                {
                    Missing++;
                    return false;
                }

                if (IsLink(directory))
                {
                    Link++;
                    return false;
                }

                return true;
            }
            catch (DirectoryNotFoundException)
            {
                Missing++;
                return false;
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                Inaccessible++;
                return false;
            }
        }

        /// <summary>Increments the processed counter and reports progress.</summary>
        private void Visit(string path)
        {
            ProcessedCount++;
            progress?.Invoke(ProcessedCount, path);
        }

        /// <summary>Enumerates a directory once, retaining matching files and recursing into subdirectories and flattening folders that need no node; null if skipped.</summary>
        private DirectoryScan? ScanDirectory(DirectoryInfo directory)
        {
            var files = new List<RetainedFile>();
            var subdirectories = new List<DirectoryInfo>();

            try
            {
                // Do not open or read file contents: only listing metadata, so Last Accessed is never updated.
                foreach (FileSystemInfo entry in directory.EnumerateFileSystemInfos("*", Options))
                {
                    token.ThrowIfCancellationRequested();

                    if (entry is DirectoryInfo sub)
                        subdirectories.Add(sub);
                    else if (entry is FileInfo file && TryRetain(file, out RetainedFile? retained))
                        files.Add(retained);
                }
            }
            catch (DirectoryNotFoundException)
            {
                Missing++;
                return null;
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                Inaccessible++;
                return null;
            }

            files.Sort((a, b) => CompareNames(a.FileName, b.FileName));
            if (files.Count > 0)
                MatchingFolderCount++;

            subdirectories.Sort((a, b) => CompareNames(a.Name, b.Name));
            var nodes = new List<FolderNode>();
            foreach (DirectoryInfo sub in subdirectories)
            {
                token.ThrowIfCancellationRequested();
                Visit(sub.FullName);

                // Mark a nested root as reached before validating it, so a skipped or linked nested root
                // is not scanned (and counted) a second time by Scan's fallback loop.
                bool isNestedRoot = _reachedNested.Count < nestedRoots.Count && nestedRoots.Contains(sub.FullName);
                if (isNestedRoot)
                    _reachedNested.Add(sub.FullName);

                if (!TryEnter(sub))
                    continue;

                DirectoryScan? child = ScanDirectory(sub);
                if (child is null)
                    continue;

                FolderNode? nestedNode = null;
                if (isNestedRoot)
                {
                    nestedNode = Wrap(sub.FullName, sub.FullName, child);
                    _rootNodes[sub.FullName] = nestedNode;
                }

                if (ShouldCreateNode(child))
                    nodes.Add(nestedNode ?? Wrap(sub.FullName, sub.Name, child));
                else
                    nodes.AddRange(child.Nodes);
            }

            return new DirectoryScan(files, nodes);
        }

        /// <summary>Returns true when a scanned subdirectory has matching files of its own, or branches into multiple shown children while subfolders are included.</summary>
        private bool ShouldCreateNode(DirectoryScan scan) =>
            scan.Files.Count > 0 || (includeSubfolders && scan.Nodes.Count > 1);

        /// <summary>Applies the filter and captures listing metadata; files whose metadata cannot be read are ignored.</summary>
        private bool TryRetain(FileInfo file, [NotNullWhen(true)] out RetainedFile? retained)
        {
            retained = null;
            try
            {
                if (!filter.Matches(file))
                    return false;

                retained = new RetainedFile(file.Name, file.LastAccessTime);
                return true;
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                return false;
            }
        }

        /// <summary>Builds a node from a directory's own (already sorted) files and shown descendants, relabeling children relative to it only when their label differs, then sorting them; the node may be cached as a top-level nested root.</summary>
        private static FolderNode Wrap(string fullPath, string displayName, DirectoryScan scan)
        {
            var children = new List<FolderNode>(scan.Nodes.Count);
            int subtreeCount = scan.Files.Count;
            foreach (FolderNode child in scan.Nodes)
            {
                string relative = Path.GetRelativePath(fullPath, child.FullPath);
                children.Add(child.DisplayName == relative ? child : child with { DisplayName = relative });
                subtreeCount += child.SubtreeFileCount;
            }

            children.Sort((a, b) => CompareNames(a.DisplayName, b.DisplayName));
            return new FolderNode(displayName, fullPath, children, [.. scan.Files], subtreeCount);
        }
    }
}

/// <summary>Pure formatting of elapsed time, status strip text, and tooltip breakdowns for load outcomes.</summary>
public static class FolderTreeStatusFormatter
{
    /// <summary>Formats a duration as "0.8 s" below one minute, otherwise "2 min 14 s".</summary>
    public static string FormatElapsed(TimeSpan elapsed)
    {
        double seconds = elapsed.TotalSeconds;
        if (Math.Round(seconds, 1) < 60)
            return $"{seconds:0.0} s";

        int whole = (int)Math.Round(seconds);
        return $"{whole / 60} min {whole % 60} s";
    }

    /// <summary>Formats the completion message, with or without matching folders.</summary>
    public static string FormatCompleted(FolderScanResult result)
    {
        string elapsed = FormatElapsed(result.Elapsed);
        string skipped = result.Skipped.Total.ToString("N0");

        if (result.MatchingFolderCount == 0)
        {
            return $"No folders contain matching files ({result.RootsScanned:N0} {Plural(result.RootsScanned, "root")} scanned, {skipped} skipped) in {elapsed}";
        }

        return $"Loaded {result.MatchingFolderCount:N0} {Plural(result.MatchingFolderCount, "folder")} with matching files ({skipped} skipped) in {elapsed}";
    }

    public static string FormatCancelled(TimeSpan elapsed) => $"Load cancelled after {FormatElapsed(elapsed)}";

    public static string FormatFaulted(Exception error) =>
        $"Load failed: {string.Join(' ', error.Message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}";

    /// <summary>Builds the multi-line tooltip breakdown for a completed scan.</summary>
    public static string FormatTooltip(FolderScanResult result) => string.Join(Environment.NewLine,
        $"Folders processed: {result.ProcessedCount:N0}",
        $"Skipped: {result.Skipped.Total:N0}",
        $"  Inaccessible: {result.Skipped.Inaccessible:N0}",
        $"  Link or junction: {result.Skipped.Link:N0}",
        $"  Missing: {result.Skipped.Missing:N0}",
        $"Roots scanned: {result.RootsScanned:N0}",
        $"Elapsed: {FormatElapsed(result.Elapsed)}");

    public static string FormatNodeTooltip(FolderNode folder)
    {
        int own = folder.OwnFileCount;
        int sub = folder.SubtreeFileCount - own;
        string suffix = sub > 0 ? $" ({sub:N0} in subfolders)" : string.Empty;
        return $"{own:N0} {Plural(own, "file")}{suffix}";
    }

    public static string FormatFolderActivated(int fileCount, string folderName) =>
        $"{fileCount:N0} {Plural(fileCount, "file")} displayed from '{folderName}'";

    /// <summary>Appends "s" to the word unless the count is exactly one.</summary>
    private static string Plural(int count, string word) => count == 1 ? word : word + "s";
}