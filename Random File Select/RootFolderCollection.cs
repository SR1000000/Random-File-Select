using System.Diagnostics.CodeAnalysis;

namespace Random_File_Select;

/// <summary>Manages an ordered collection of root folder paths with validation, deduplication, and selection states.</summary>
public sealed class RootFolderCollection
{
    private readonly List<RootFolderEntry> _items = [];

    /// <summary>Read-only view of current root folder entries.</summary>
    public IReadOnlyList<RootFolderEntry> Items => _items;

    /// <summary>Enumerates paths of all currently checked entries.</summary>
    public IEnumerable<string> CheckedPaths =>
        _items.Where(entry => entry.IsChecked).Select(entry => entry.Path);

    /// <summary>Validates, normalizes, and appends a unique existing directory path.</summary>
    public bool TryAdd(string path, [NotNullWhen(true)] out RootFolderEntry? added, [NotNullWhen(false)] out string? error)
    {
        added = null;
        if (!TryValidate(path, exceptIndex: null, out string? normalized, out error))
            return false;

        added = new RootFolderEntry { Path = normalized, IsChecked = false };
        _items.Add(added);
        return true;
    }

    /// <summary>Validates, normalizes, and replaces the folder at the specified index, preserving checked state.</summary>
    public bool TryReplace(int index, string newPath, [NotNullWhen(true)] out RootFolderEntry? updated, [NotNullWhen(false)] out string? error)
    {
        updated = null;
        if ((uint)index >= (uint)_items.Count)
        {
            error = "Invalid folder selection.";
            return false;
        }

        if (!TryValidate(newPath, exceptIndex: index, out string? normalized, out error))
            return false;

        updated = new RootFolderEntry { Path = normalized, IsChecked = _items[index].IsChecked };
        _items[index] = updated;
        return true;
    }

    /// <summary>Removes the entry at the specified index.</summary>
    public void RemoveAt(int index) => _items.RemoveAt(index);

    /// <summary>Sets the checked state of the entry at the specified index.</summary>
    public void SetChecked(int index, bool value) => _items[index].IsChecked = value;

    /// <summary>Replaces collection contents with validated, deduplicated entries from persistent storage.</summary>
    public void LoadSaved(IEnumerable<RootFolderEntry>? entries)
    {
        _items.Clear();
        if (entries is null)
            return;

        foreach (RootFolderEntry? entry in entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Path))
                continue;

            string path = NormalizeSavedPath(entry.Path);
            if (IsDuplicate(path, exceptIndex: null))
                continue;

            _items.Add(new RootFolderEntry { Path = path, IsChecked = entry.IsChecked });
        }
    }

    /// <summary>Creates a detached copy of all entries for serialization.</summary>
    public List<RootFolderEntry> Export() =>
        _items.Select(e => new RootFolderEntry { Path = e.Path, IsChecked = e.IsChecked }).ToList();

    /// <summary>Normalizes a path and checks that it is an existing directory and not a duplicate (ignoring exceptIndex), returning the normalized path or an error message.</summary>
    private bool TryValidate(string path, int? exceptIndex, [NotNullWhen(true)] out string? normalized, [NotNullWhen(false)] out string? error)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Please choose a folder.";
            return false;
        }

        if (!TryGetFullPath(path, out string? fullPath))
        {
            error = "That path is not valid.";
            return false;
        }

        if (!Directory.Exists(fullPath))
        {
            error = "That folder does not exist.";
            return false;
        }

        if (IsDuplicate(fullPath, exceptIndex))
        {
            error = "Folder already in list.";
            return false;
        }

        normalized = fullPath;
        error = null;
        return true;
    }

    /// <summary>Returns the full path without a trailing separator, or false when the path syntax is invalid.</summary>
    private static bool TryGetFullPath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        try
        {
            fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            fullPath = null;
            return false;
        }
    }

    /// <summary>Determines whether a normalized path already exists, optionally ignoring an index.</summary>
    private bool IsDuplicate(string normalizedPath, int? exceptIndex) =>
        _items.Where((item, i) => i != exceptIndex
            && string.Equals(item.Path, normalizedPath, StringComparison.OrdinalIgnoreCase)).Any();

    /// <summary>Normalizes a stored path to a full path without verifying directory existence; an unparseable path is kept trimmed.</summary>
    private static string NormalizeSavedPath(string path)
    {
        string trimmed = path.Trim();
        return TryGetFullPath(trimmed, out string? fullPath) ? fullPath : trimmed;
    }
}
