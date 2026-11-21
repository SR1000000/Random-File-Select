using System.ComponentModel;

namespace Random_File_Select;

/// <summary>Manages extension filter presets and synchronizes them with the extension ComboBox.</summary>
public sealed class FiltersController
{
    private readonly ComboBox _extensionsComboBox;
    private BindingList<string> _extensionGroups = new();

    /// <summary>Initializes the controller and binds the ComboBox to extension groups.</summary>
    public FiltersController(ComboBox extensionsComboBox)
    {
        _extensionsComboBox = extensionsComboBox;
        InitializeExtensions(null);
    }

    /// <summary>Populates extension groups from settings and selects the active preset.</summary>
    public void Load(FilterSettings settings)
    {
        InitializeExtensions(settings.ExtensionGroups.Count > 0 ? settings.ExtensionGroups : null);

        if (settings.ExtensionFilterText is not null)
        {
            string text = settings.ExtensionFilterText;
            int match = _extensionGroups.ToList().FindIndex(
                g => string.Equals(g, text, StringComparison.OrdinalIgnoreCase));
            if (match >= 0)
            {
                _extensionsComboBox.SelectedIndex = match;
            }
            else
            {
                _extensionsComboBox.SelectedIndex = -1;
                _extensionsComboBox.Text = text;
            }

            return;
        }

        if (settings.SelectedExtensionGroupIndex >= 0
            && settings.SelectedExtensionGroupIndex < _extensionsComboBox.Items.Count)
        {
            _extensionsComboBox.SelectedIndex = settings.SelectedExtensionGroupIndex;
        }
    }

    /// <summary>Exports extension groups and selected index into a FilterSettings object.</summary>
    public FilterSettings Export()
    {
        int selectedIndex = _extensionsComboBox.SelectedIndex >= 0
            ? _extensionsComboBox.SelectedIndex
            : -1;

        return new FilterSettings
        {
            ExtensionGroups = _extensionGroups.ToList(),
            SelectedExtensionGroupIndex = selectedIndex,
            ExtensionFilterText = _extensionsComboBox.Text
        };
    }

    /// <summary>Appends the current ComboBox text to extension groups if unique, or selects the existing match.</summary>
    public void AddExtensionGroup()
    {
        string text = _extensionsComboBox.Text.Trim();

        for (int i = 0; i < _extensionGroups.Count; i++)
        {
            if (string.Equals(_extensionGroups[i], text, StringComparison.OrdinalIgnoreCase))
            {
                _extensionsComboBox.SelectedIndex = i;
                return;
            }
        }

        _extensionGroups.Add(text);
        _extensionsComboBox.SelectedIndex = _extensionGroups.Count - 1;
    }

    /// <summary>Removes the selected extension group and adjusts the selection index.</summary>
    public void RemoveExtensionGroup()
    {
        int index = _extensionsComboBox.SelectedIndex;
        if (index < 0)
            return;

        _extensionGroups.RemoveAt(index);

        if (_extensionGroups.Count > 0)
            _extensionsComboBox.SelectedIndex = Math.Min(index, _extensionGroups.Count - 1);
        else
            _extensionsComboBox.SelectedIndex = -1;
    }

    /// <summary>Initializes the binding list with saved or default extension groups and binds to the ComboBox.</summary>
    private void InitializeExtensions(List<string>? savedGroups)
    {
        if (savedGroups is null || savedGroups.Count == 0)
        {
            _extensionGroups = new BindingList<string> { "mkv mp4 avi mov" };
        }
        else
        {
            _extensionGroups = new BindingList<string>(savedGroups);
        }

        _extensionsComboBox.DataSource = _extensionGroups;
        _extensionsComboBox.SelectedIndex = _extensionGroups.Count > 0 ? 0 : -1;
    }
}

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
        try
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (RootFolderEntry entry in _collection.Items)
                _list.Items.Add(entry, entry.IsChecked);
        }
        finally
        {
            _list.EndUpdate();
            _suppressItemCheck = false;
        }

        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
        else
            _list.SelectedIndex = -1;

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Exports current root folder entries for serialization.</summary>
    public List<RootFolderEntry> Export() => _collection.Export();

    /// <summary>Displays folder browser dialog and appends the selected folder if valid and non-duplicate.</summary>
    public void Add()
    {
        if (_addDialog.ShowDialog(_owner) != DialogResult.OK)
            return;

        string selectedPath = _addDialog.SelectedPath;
        if (!_collection.TryAdd(selectedPath, out RootFolderEntry? added, out string? error))
        {
            MessageBox.Show(_owner, error, "Add root folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _list.Items.Add(added!, isChecked: false);
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

        string selectedPath = _editDialog.SelectedPath;
        if (!_collection.TryReplace(index, selectedPath, out RootFolderEntry? updated, out string? error))
        {
            MessageBox.Show(_owner, error, "Edit root folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _list.Items[index] = updated!;
        _list.SetItemChecked(index, updated!.IsChecked);
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

        if (e.Index < 0 || e.Index >= _collection.Items.Count)
            return;

        bool isChecked = e.NewValue == CheckState.Checked;
        _collection.SetChecked(e.Index, isChecked);
    }

    /// <summary>Raises the <see cref="SelectionChanged"/> event when list selection changes.</summary>
    private void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
