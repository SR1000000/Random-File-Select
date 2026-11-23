using System.ComponentModel;

namespace Random_File_Select;

/// <summary>Manages extension filter presets and synchronizes them with the extension ComboBox.</summary>
public sealed class FiltersController
{
    private readonly ComboBox _extensionsComboBox;
    private BindingList<string> _extensionGroups = [];

    /// <summary>Initializes the controller and binds the ComboBox to extension groups.</summary>
    public FiltersController(ComboBox extensionsComboBox)
    {
        _extensionsComboBox = extensionsComboBox;
        InitializeExtensions(null);
    }

    /// <summary>Populates extension groups from settings and selects the active preset.</summary>
    public void Load(FilterSettings settings)
    {
        InitializeExtensions(settings.ExtensionGroups);

        if (settings.ExtensionFilterText is not null)
        {
            string text = settings.ExtensionFilterText;
            int match = FindGroupIndex(text);
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
        return new FilterSettings
        {
            ExtensionGroups = [.. _extensionGroups],
            SelectedExtensionGroupIndex = _extensionsComboBox.SelectedIndex,
            ExtensionFilterText = _extensionsComboBox.Text
        };
    }

    /// <summary>Appends the current ComboBox text to extension groups if non-blank and unique, or selects the existing match.</summary>
    public void AddExtensionGroup()
    {
        string text = _extensionsComboBox.Text.Trim();
        if (text.Length == 0)
            return;

        int existing = FindGroupIndex(text);
        if (existing >= 0)
        {
            _extensionsComboBox.SelectedIndex = existing;
            return;
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
        _extensionsComboBox.SelectedIndex = Math.Min(index, _extensionGroups.Count - 1);
    }

    /// <summary>Case-insensitive index of the group with the given text, or -1.</summary>
    private int FindGroupIndex(string text)
    {
        for (int i = 0; i < _extensionGroups.Count; i++)
        {
            if (string.Equals(_extensionGroups[i], text, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>Initializes the binding list with a copy of the saved groups, or the default preset when none exist, and binds it to the ComboBox.</summary>
    private void InitializeExtensions(List<string>? savedGroups)
    {
        _extensionGroups = savedGroups is { Count: > 0 }
            ? new BindingList<string>([.. savedGroups])
            : ["mkv mp4 avi mov"];

        _extensionsComboBox.DataSource = _extensionGroups;
        _extensionsComboBox.SelectedIndex = 0;
    }
}
