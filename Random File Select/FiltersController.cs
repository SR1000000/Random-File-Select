using System.ComponentModel;

namespace Random_File_Select;

/// <summary>Owns all filter inputs (extension presets, include/exclude text, modified-date range) and converts them to and from <see cref="FilterSettings"/>.</summary>
public sealed class FiltersController
{
    private readonly ComboBox _extensionsComboBox;
    private readonly TextBox _includeTextBox;
    private readonly TextBox _excludeTextBox;
    private readonly DateTimePicker _minDatePicker;
    private readonly DateTimePicker _maxDatePicker;
    private BindingList<string> _extensionGroups = [];

    /// <summary>Initializes the controller and binds the ComboBox to extension groups.</summary>
    public FiltersController(
        ComboBox extensionsComboBox,
        TextBox includeTextBox,
        TextBox excludeTextBox,
        DateTimePicker minDatePicker,
        DateTimePicker maxDatePicker)
    {
        _extensionsComboBox = extensionsComboBox;
        _includeTextBox = includeTextBox;
        _excludeTextBox = excludeTextBox;
        _minDatePicker = minDatePicker;
        _maxDatePicker = maxDatePicker;
        InitializeExtensions(null);
    }

    /// <summary>Populates all filter controls from settings and selects the active extension preset.</summary>
    public void Load(FilterSettings settings)
    {
        LoadExtensions(settings);

        _includeTextBox.Text = settings.Include;
        _excludeTextBox.Text = settings.Exclude;

        if (settings.DateModifiedMin.HasValue)
        {
            _minDatePicker.Value = settings.DateModifiedMin.Value;
            _minDatePicker.Checked = true;
        }
        else
        {
            _minDatePicker.Checked = false;
        }

        // DateModifiedMax is never restored; Max always starts at today. Uncheck after assigning Value,
        // because assigning Value can re-check the box.
        _maxDatePicker.Value = DateTime.Today;
        _maxDatePicker.Checked = false;
    }

    /// <summary>Captures all filter controls into a FilterSettings object.</summary>
    public FilterSettings Export()
    {
        return new FilterSettings
        {
            Include = _includeTextBox.Text,
            Exclude = _excludeTextBox.Text,
            ExtensionGroups = [.. _extensionGroups],
            SelectedExtensionGroupIndex = _extensionsComboBox.SelectedIndex,
            ExtensionFilterText = _extensionsComboBox.Text,
            DateModifiedMin = _minDatePicker.Checked ? _minDatePicker.Value : null,
            DateModifiedMax = _maxDatePicker.Checked ? _maxDatePicker.Value : null
        };
    }

    /// <summary>Populates extension groups from settings and selects the active preset.</summary>
    private void LoadExtensions(FilterSettings settings)
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
