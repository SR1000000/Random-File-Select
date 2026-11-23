namespace Random_File_Select;

/// <summary>Coordinates root folder list interactions, browser dialogs, and selection state synchronization.</summary>
public sealed class RootFoldersController
{
    private readonly CheckedListBox _list;
    private readonly FolderBrowserDialog _addDialog;
    private readonly FolderBrowserDialog _editDialog;
    private readonly IWin32Window _owner;
    private readonly RootFolderCollection _collection = new();
    private bool _suppressItemCheck;

    /// <summary>Initializes the controller with UI controls and wires event handlers.</summary>
    public RootFoldersController(
        CheckedListBox list,
        FolderBrowserDialog addDialog,
        FolderBrowserDialog editDialog,
        IWin32Window owner)
    {
        _list = list;
        _addDialog = addDialog;
        _editDialog = editDialog;
        _owner = owner;

        _list.ItemCheck += OnItemCheck;
        _list.SelectedIndexChanged += OnSelectedIndexChanged;
    }

    /// <summary>Fresh read-only snapshot of currently checked root paths; the backing collection is not exposed.</summary>
    public IReadOnlyList<string> CheckedPaths => _collection.CheckedPaths.ToList();

    /// <summary>Indicates whether an item in the list is currently selected.</summary>
    public bool HasSelection => _list.SelectedIndex >= 0;

    /// <summary>Occurs when the selected item in the root folders list changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Loads entries into the backing collection and populates the CheckedListBox.</summary>
    public void Load(IEnumerable<RootFolderEntry>? entries)
    {
        _collection.LoadSaved(entries);

        _suppressItemCheck = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (RootFolderEntry entry in _collection.Items)
                _list.Items.Add(entry, entry.IsChecked);
        }
        finally
        {
            _list.EndUpdate();
            _suppressItemCheck = false;
        }

        // Clear leaves SelectedIndex at -1, so selecting item 0 raises SelectionChanged exactly once;
        // an empty list has no index change to forward, so raise it directly.
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
        else
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Exports current root folder entries for serialization.</summary>
    public List<RootFolderEntry> Export() => _collection.Export();

    /// <summary>Displays folder browser dialog and appends the selected folder if valid and non-duplicate.</summary>
    public void Add()
    {
        if (_addDialog.ShowDialog(_owner) != DialogResult.OK)
            return;

        if (!_collection.TryAdd(_addDialog.SelectedPath, out RootFolderEntry? added, out string? error))
        {
            MessageBox.Show(_owner, error, "Add root folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _list.Items.Add(added, isChecked: false);
    }

    /// <summary>Displays folder browser dialog to update the selected folder path if valid.</summary>
    public void EditSelected()
    {
        int index = _list.SelectedIndex;
        if (index < 0)
            return;

        _editDialog.SelectedPath = _collection.Items[index].Path;
        if (_editDialog.ShowDialog(_owner) != DialogResult.OK)
            return;

        if (!_collection.TryReplace(index, _editDialog.SelectedPath, out RootFolderEntry? updated, out string? error))
        {
            MessageBox.Show(_owner, error, "Edit root folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Replacing the item may reset its check mark; restore it without writing back through OnItemCheck.
        _suppressItemCheck = true;
        try
        {
            _list.Items[index] = updated;
            _list.SetItemChecked(index, updated.IsChecked);
        }
        finally
        {
            _suppressItemCheck = false;
        }

        _list.SelectedIndex = index;
    }

    /// <summary>Removes the selected root folder from the collection and list.</summary>
    public void RemoveSelected()
    {
        int index = _list.SelectedIndex;
        if (index < 0)
            return;

        _collection.RemoveAt(index);
        _list.Items.RemoveAt(index);

        if (_list.Items.Count > 0)
            _list.SelectedIndex = Math.Min(index, _list.Items.Count - 1);
    }

    /// <summary>Propagates CheckedListBox item check state changes to the backing collection.</summary>
    private void OnItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressItemCheck)
            return;

        _collection.SetChecked(e.Index, e.NewValue == CheckState.Checked);
    }

    /// <summary>Raises the <see cref="SelectionChanged"/> event when list selection changes.</summary>
    private void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
