using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Random_File_Select;

/// <summary>Compact metadata for one matching file, captured from the directory listing (no file content is read).</summary>
public sealed record RetainedFile(string FileName, DateTime LastWriteTime, DateTime LastAccessTime, long Length);

/// <summary>Immutable shown folder: display label, true full path, shown descendants, and its own matching files.</summary>
public sealed record FolderNode(
    string DisplayName,
    string FullPath,
    IReadOnlyList<FolderNode> Children,
    ImmutableArray<RetainedFile> Files,
    int SubtreeFileCount)
{
    /// <summary>Number of matching files directly inside this folder.</summary>
    public int OwnFileCount => Files.Length;
}

/// <summary>Counts of skipped directories by reason; <see cref="Total"/> is their sum.</summary>
public sealed record SkippedCounts(int Inaccessible, int Link, int Missing)
{
    /// <summary>Total skipped directories across all reasons.</summary>
    public int Total => Inaccessible + Link + Missing;
}

/// <summary>Outcome of a completed scan: root nodes plus exact counts and scan duration.</summary>
public sealed record FolderScanResult(
    IReadOnlyList<FolderNode> Roots,
    int MatchingFolderCount,
    int ProcessedCount,
    int RootsScanned,
    int MergedRootCount,
    SkippedCounts Skipped,
    TimeSpan Elapsed);

/// <summary>
/// Scans checked root folders in a single recursive pass and builds a tree of folders that directly contain matching files.
/// Do not open or read file contents here. Only directory-listing metadata (name, timestamps, length) may be read,
/// so the scan never updates a file's Last Accessed time, which later weighting logic depends on.
/// </summary>
public static class FolderTreeBuilder
{
    /// <summary>Drops roots lying under another root (or duplicating an earlier one), preserving order, and reports how many were merged.</summary>
    public static (IReadOnlyList<string> Roots, int MergedCount) RemoveNestedRoots(IEnumerable<string> roots)
    {
        List<string> all = roots.ToList();
        var kept = new List<string>();

        for (int i = 0; i < all.Count; i++)
        {
            bool drop = false;
            for (int j = 0; j < all.Count && !drop; j++)
            {
                if (i == j)
                    continue;

                if (IsUnder(all[i], all[j]))
                    drop = true;
                else if (j < i && string.Equals(all[i], all[j], StringComparison.OrdinalIgnoreCase))
                    drop = true;
            }

            if (!drop)
                kept.Add(all[i]);
        }

        return (kept, all.Count - kept.Count);
    }

    /// <summary>True when child is strictly inside parent; handles drive/UNC roots that already end with a separator.</summary>
    public static bool IsUnder(string child, string parent)
    {
        string prefix = Path.EndsInDirectorySeparator(parent)
            ? parent
            : parent + Path.DirectorySeparatorChar;

        return child.Length > prefix.Length && child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Scans roots (nested ones removed) with the filter, reporting processed count and current path; throws on cancellation.</summary>
    public static FolderScanResult Scan(
        IEnumerable<string> roots,
        FileFilter filter,
        CancellationToken cancellationToken,
        Action<int, string>? progress = null)
    {
        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<string> keptRoots, int merged) = RemoveNestedRoots(roots);

        var scanner = new Scanner(filter, cancellationToken, progress);
        var rootNodes = new List<FolderNode>();
        foreach (string root in keptRoots)
        {
            FolderNode? node = scanner.ScanRoot(root);
            if (node is not null)
                rootNodes.Add(node);
        }

        stopwatch.Stop();
        return new FolderScanResult(
            rootNodes,
            scanner.MatchingFolderCount,
            scanner.ProcessedCount,
            rootNodes.Count,
            merged,
            new SkippedCounts(scanner.Inaccessible, scanner.Link, scanner.Missing),
            stopwatch.Elapsed);
    }

    /// <summary>Orders names by OrdinalIgnoreCase, then Ordinal, for culture-independent determinism.</summary>
    private static int CompareNames(string a, string b)
    {
        int result = StringComparer.OrdinalIgnoreCase.Compare(a, b);
        return result != 0 ? result : StringComparer.Ordinal.Compare(a, b);
    }

    /// <summary>True for exceptions that mean a directory cannot be inspected and should be skipped.</summary>
    private static bool IsSkippable(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or SecurityException or PathTooLongException
            or ArgumentException or NotSupportedException;

    /// <summary>True for a symbolic link or junction (reparse point with a link target); other reparse points are traversed.</summary>
    private static bool IsLink(DirectoryInfo directory) =>
        (directory.Attributes & FileAttributes.ReparsePoint) != 0 && directory.LinkTarget is not null;

    private sealed record DirectoryScan(List<RetainedFile> Files, List<FolderNode> Nodes);

    private sealed class Scanner(FileFilter filter, CancellationToken token, Action<int, string>? progress)
    {
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

        /// <summary>Scans one root; returns its always-shown node, or null when the root is skipped.</summary>
        public FolderNode? ScanRoot(string path)
        {
            token.ThrowIfCancellationRequested();
            Visit(path);

            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(path);
                if (!directory.Exists)
                {
                    Missing++;
                    return null;
                }

                if (IsLink(directory))
                {
                    Link++;
                    return null;
                }
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                Inaccessible++;
                return null;
            }

            DirectoryScan? scan = ScanDirectory(directory);
            return scan is null ? null : Wrap(directory.FullName, directory.FullName, scan);
        }

        /// <summary>Records a visited directory and reports progress.</summary>
        private void Visit(string path)
        {
            ProcessedCount++;
            progress?.Invoke(ProcessedCount, path);
        }

        /// <summary>Enumerates a directory once, retaining matching files and recursing into subdirectories; null if skipped.</summary>
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
                        files.Add(retained!);
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

            subdirectories.Sort((a, b) => CompareNames(a.Name, b.Name));
            var nodes = new List<FolderNode>();
            foreach (DirectoryInfo sub in subdirectories)
            {
                token.ThrowIfCancellationRequested();
                Visit(sub.FullName);

                try
                {
                    if (IsLink(sub))
                    {
                        Link++;
                        continue;
                    }
                }
                catch (Exception ex) when (IsSkippable(ex))
                {
                    Inaccessible++;
                    continue;
                }

                DirectoryScan? child = ScanDirectory(sub);
                if (child is null)
                    continue;

                if (child.Files.Count > 0)
                    nodes.Add(Wrap(sub.FullName, sub.Name, child));
                else
                    nodes.AddRange(child.Nodes);
            }

            return new DirectoryScan(files, nodes);
        }

        /// <summary>Applies the filter and captures listing metadata; files whose metadata cannot be read are ignored.</summary>
        private bool TryRetain(FileInfo file, out RetainedFile? retained)
        {
            retained = null;
            try
            {
                if (!filter.Matches(file))
                    return false;

                retained = new RetainedFile(file.Name, file.LastWriteTime, file.LastAccessTime, file.Length);
                return true;
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                return false;
            }
        }

        /// <summary>Builds a node from a directory's own files and shown descendants, relabeling and sorting children.</summary>
        private FolderNode Wrap(string fullPath, string displayName, DirectoryScan scan)
        {
            if (scan.Files.Count > 0)
                MatchingFolderCount++;

            scan.Files.Sort((a, b) => CompareNames(a.FileName, b.FileName));

            List<FolderNode> children = scan.Nodes
                .Select(child => child with { DisplayName = Path.GetRelativePath(fullPath, child.FullPath) })
                .ToList();
            children.Sort((a, b) => CompareNames(a.DisplayName, b.DisplayName));

            int subtreeCount = scan.Files.Count + children.Sum(c => c.SubtreeFileCount);
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
            return string.Create(CultureInfo.InvariantCulture, $"{seconds:0.0} s");

        int whole = (int)Math.Round(seconds);
        return $"{whole / 60} min {whole % 60} s";
    }

    /// <summary>Formats the completion message, with or without matching folders.</summary>
    public static string FormatCompleted(FolderScanResult result)
    {
        string elapsed = FormatElapsed(result.Elapsed);
        string skipped = result.Skipped.Total.ToString("N0", CultureInfo.InvariantCulture);

        if (result.MatchingFolderCount == 0)
        {
            string rootWord = result.RootsScanned == 1 ? "root" : "roots";
            return $"No folders contain matching files ({result.RootsScanned:N0} {rootWord} scanned, {skipped} skipped) in {elapsed}";
        }

        string folderWord = result.MatchingFolderCount == 1 ? "folder" : "folders";
        string detail = $"{skipped} skipped";
        if (result.MergedRootCount > 0)
        {
            string mergedWord = result.MergedRootCount == 1 ? "root" : "roots";
            detail += $", {result.MergedRootCount:N0} nested {mergedWord} merged";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Loaded {result.MatchingFolderCount:N0} {folderWord} with matching files ({detail}) in {elapsed}");
    }

    /// <summary>Formats the cancellation message.</summary>
    public static string FormatCancelled(TimeSpan elapsed) => $"Load cancelled after {FormatElapsed(elapsed)}";

    /// <summary>Formats the failure message as a single line.</summary>
    public static string FormatFaulted(Exception error) =>
        $"Load failed: {Regex.Replace(error.Message, @"\s+", " ").Trim()}";

    /// <summary>Builds the multi-line tooltip breakdown for a completed scan.</summary>
    public static string FormatTooltip(FolderScanResult result)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Folders processed: {result.ProcessedCount:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Skipped: {result.Skipped.Total:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Inaccessible: {result.Skipped.Inaccessible:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Link or junction: {result.Skipped.Link:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Missing: {result.Skipped.Missing:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Roots scanned: {result.RootsScanned:N0}"));
        text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Nested roots merged: {result.MergedRootCount:N0}"));
        text.Append($"Elapsed: {FormatElapsed(result.Elapsed)}");
        return text.ToString();
    }

    /// <summary>Formats the tooltip text for a folder node showing its own matching file count and subtree count (if non-zero).</summary>
    public static string FormatNodeTooltip(FolderNode folder)
    {
        int own = folder.OwnFileCount;
        int sub = folder.SubtreeFileCount - own;
        string fileWord = own == 1 ? "file" : "files";

        if (sub > 0)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{own:N0} {fileWord} ({sub:N0} in subfolders)");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{own:N0} {fileWord}");
    }

    /// <summary>Formats the status strip text showing the count of files displayed from the activated folder.</summary>
    public static string FormatFolderActivated(int fileCount, string folderName)
    {
        string fileWord = fileCount == 1 ? "file" : "files";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{fileCount:N0} {fileWord} displayed from '{folderName}'");
    }
}
