namespace Random_File_Select;

/// <summary>Manages an ordered collection of root folder paths with validation, deduplication, and selection states.</summary>
public sealed class RootFolderCollection
{
    private readonly List<RootFolderEntry> _items;

    /// <summary>Initializes a new collection, optionally seeded with initial entries.</summary>
    public RootFolderCollection(IEnumerable<RootFolderEntry>? initialEntries = null)
    {
        _items = initialEntries is null
            ? new List<RootFolderEntry>()
            : new List<RootFolderEntry>(initialEntries);
    }

    /// <summary>Read-only view of current root folder entries.</summary>
    public IReadOnlyList<RootFolderEntry> Items => _items;

    /// <summary>Enumerates paths of all currently checked entries.</summary>
    public IEnumerable<string> CheckedPaths =>
        _items.Where(entry => entry.IsChecked).Select(entry => entry.Path);

    /// <summary>Validates, normalizes, and appends a unique existing directory path.</summary>
    public bool TryAdd(string path, out RootFolderEntry? added, out string? error)
    {
        added = null;
        if (!TryNormalize(path, out string? normalized, out error) || normalized is null)
            return false;

        if (IsDuplicate(normalized, exceptIndex: null))
        {
            error = "Folder already in list.";
            return false;
        }

        added = new RootFolderEntry { Path = normalized, IsChecked = false };
        _items.Add(added);
        return true;
    }

    /// <summary>Validates, normalizes, and replaces the folder at the specified index, preserving checked state.</summary>
    public bool TryReplace(int index, string newPath, out RootFolderEntry? updated, out string? error)
    {
        updated = null;
        if (!IsValidIndex(index))
        {
            error = "Invalid folder selection.";
            return false;
        }

        if (!TryNormalize(newPath, out string? normalized, out error) || normalized is null)
            return false;

        if (IsDuplicate(normalized, exceptIndex: index))
        {
            error = "Folder already in list.";
            return false;
        }

        bool isChecked = _items[index].IsChecked;
        updated = new RootFolderEntry { Path = normalized, IsChecked = isChecked };
        _items[index] = updated;
        return true;
    }

    /// <summary>Removes the entry at the specified index.</summary>
    public void RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            throw new ArgumentOutOfRangeException(nameof(index));

        _items.RemoveAt(index);
    }

    /// <summary>Sets the checked state of the entry at the specified index.</summary>
    public void SetChecked(int index, bool value)
    {
        if (!IsValidIndex(index))
            throw new ArgumentOutOfRangeException(nameof(index));

        _items[index].IsChecked = value;
    }

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

            string path = TryNormalizeSavedPath(entry.Path);
            if (IsDuplicate(path, exceptIndex: null))
                continue;

            _items.Add(new RootFolderEntry { Path = path, IsChecked = entry.IsChecked });
        }
    }

    /// <summary>Creates a detached copy of all entries for serialization.</summary>
    public List<RootFolderEntry> Export() =>
        _items.Select(e => new RootFolderEntry { Path = e.Path, IsChecked = e.IsChecked }).ToList();

    /// <summary>Determines whether the index is within the collection bounds.</summary>
    private bool IsValidIndex(int index) => (uint)index < (uint)_items.Count;

    /// <summary>Validates path syntax and directory existence, returning a trimmed full path or error message.</summary>
    private static bool TryNormalize(string path, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Please choose a folder.";
            return false;
        }

        try
        {
            normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = "That path is not valid.";
            return false;
        }

        if (!Directory.Exists(normalized))
        {
            error = "That folder does not exist.";
            return false;
        }

        return true;
    }

    /// <summary>Determines whether a normalized path already exists, optionally ignoring an index.</summary>
    private bool IsDuplicate(string normalizedPath, int? exceptIndex)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (exceptIndex == i)
                continue;

            if (string.Equals(_items[i].Path, normalizedPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Normalizes a stored path to a full path without verifying directory existence.</summary>
    private static string TryNormalizeSavedPath(string path)
    {
        string trimmed = path.Trim();
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return trimmed;
        }
    }
}
