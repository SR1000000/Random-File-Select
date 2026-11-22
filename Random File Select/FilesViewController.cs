using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Random_File_Select;

/// <summary>Owns the files ListView: building and showing items, opening files, and weighted random selection.</summary>
public sealed class FilesViewController
{
    /// <summary>Maximum rows kept in the selection log; the oldest row is dropped once exceeded.</summary>
    private const int MaxLogEntries = 1000;

    private readonly ListView _listView;
    private readonly ListView _logListView;
    private readonly TrackBar _lastAccessWeight;
    private readonly CheckBox _instantOpen;
    private readonly CheckBox _excludeDup;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly Form _owner;

    /// <summary>Full path to the number of log rows referencing it (case-insensitive); kept in sync by LogSelection and ClearLog.</summary>
    private readonly Dictionary<string, int> _loggedPathCounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Binds to the files ListView, selection log ListView, Last Accessed trackbar, Instantly Open checkbox, Exclude Duplicates checkbox, status label, and owner form.</summary>
    public FilesViewController(
        ListView listView,
        ListView logListView,
        TrackBar lastAccessWeight,
        CheckBox instantOpen,
        CheckBox excludeDup,
        ToolStripStatusLabel statusLabel,
        Form owner)
    {
        _listView = listView;
        _logListView = logListView;
        _lastAccessWeight = lastAccessWeight;
        _instantOpen = instantOpen;
        _excludeDup = excludeDup;
        _statusLabel = statusLabel;
        _owner = owner;
    }

    /// <summary>Builds ListViewItems for a folder's files (and its subfolders' files when requested), each tagged with a FileListEntry; safe to call off the UI thread.</summary>
    public static ListViewItem[] BuildItems(FolderNode folder, bool includeSubfolders)
    {
        int capacity = includeSubfolders ? folder.SubtreeFileCount : folder.Files.Length;
        var list = new List<ListViewItem>(capacity);
        AppendFolderItems(folder, includeSubfolders, list);
        return list.ToArray();
    }

    /// <summary>Replaces the ListView contents with the given items inside BeginUpdate/EndUpdate.</summary>
    public void SetItems(ListViewItem[] items)
    {
        _listView.BeginUpdate();
        try
        {
            _listView.Items.Clear();
            if (items.Length > 0)
                _listView.Items.AddRange(items);
        }
        finally
        {
            _listView.EndUpdate();
        }
    }

    /// <summary>Returns the FileListEntry tag of the files ListView item at index (all items are tagged by BuildItems).</summary>
    private FileListEntry EntryAt(int index) => (FileListEntry)_listView.Items[index].Tag!;

    /// <summary>Picks an item by tournament selection on Last Accessed, skipping files already in the log when Exclude Duplicates is checked, then selects it (and opens when Instantly Open is checked);
    /// returns false when the list is empty, or after showing a dialog when every file is already logged.</summary>
    public bool SelectRandom()
    {
        int count = _listView.Items.Count;
        if (count == 0)
            return false;

        Func<int, DateTime> timestampAt = i => EntryAt(i).File.LastAccessTime;
        int value = _lastAccessWeight.Value, min = _lastAccessWeight.Minimum, max = _lastAccessWeight.Maximum;

        int index = !_excludeDup.Checked || _loggedPathCounts.Count == 0
            ? RandomSelector.PickTournamentIndex(count, timestampAt, value, min, max, Random.Shared)
            : RandomSelector.PickTournamentIndex(
                count, timestampAt, i => !_loggedPathCounts.ContainsKey(EntryAt(i).FullPath), value, min, max, Random.Shared);

        if (index < 0)
        {
            MessageBox.Show(
                _owner,
                "Every file in this list is already in the log. Clear the log or uncheck the exclusion option to pick again.",
                "No Files Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        return SelectItemAtIndex(index, randomChoice: true);
    }

    /// <summary>Steps to the adjacent list item (wrapping) from the current selection, then selects it (and opens when Instantly Open is checked); returns false when the list is empty or nothing is selected.</summary>
    public bool SelectRelative(int delta)
    {
        int count = _listView.Items.Count;
        if (count == 0 || _listView.SelectedIndices.Count == 0)
            return false;

        int index = WrapFileIndex(_listView.SelectedIndices[0], delta, count);
        return SelectItemAtIndex(index, randomChoice: false);
    }

    /// <summary>Selects, focuses, and reveals the item at index; sets status text for random vs sequential choice; opens when Instantly Open is checked.</summary>
    private bool SelectItemAtIndex(int index, bool randomChoice)
    {
        if ((uint)index >= (uint)_listView.Items.Count)
            return false;

        ListViewItem item = _listView.Items[index];
        item.Selected = true;
        item.Focused = true;
        item.EnsureVisible();
        _listView.Focus();

        SetFileStatus(item, randomChoice);
        LogSelection(item);

        if (_instantOpen.Checked && OpenItem(item))
            _statusLabel.Text += "  File opened.";

        return true;
    }

    /// <summary>Steps index by delta within [0, count), wrapping at both ends.</summary>
    private static int WrapFileIndex(int index, int delta, int count)
        => ((index + delta) % count + count) % count;

    /// <summary>Appends the item's directory, filename, and current Last Accessed time to the bottom of the log, tagged with the same FileListEntry so it can be opened later; drops the oldest row once the log exceeds MaxLogEntries; increments the path's logged count and decrements the evicted row's.</summary>
    private void LogSelection(ListViewItem item)
    {
        if (item.Tag is not FileListEntry entry)
            return;

        var logItem = new ListViewItem(new string[]
        {
            Path.GetDirectoryName(entry.FullPath) ?? string.Empty,
            entry.File.FileName,
            entry.File.LastAccessTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        })
        {
            Tag = entry,
            ToolTipText = entry.FullPath
        };

        _logListView.BeginUpdate();
        try
        {
            _logListView.Items.Add(logItem);
            _loggedPathCounts[entry.FullPath] = _loggedPathCounts.GetValueOrDefault(entry.FullPath) + 1;
            if (_logListView.Items.Count > MaxLogEntries)
            {
                string evictedPath = ((FileListEntry)_logListView.Items[0].Tag!).FullPath;
                _logListView.Items.RemoveAt(0);
                if (--_loggedPathCounts[evictedPath] == 0)
                    _loggedPathCounts.Remove(evictedPath);
            }
        }
        finally
        {
            _logListView.EndUpdate();
        }
        logItem.EnsureVisible();
    }

    /// <summary>Removes all rows from the selection log, resets the logged-path counts, and sets the status strip to "Log cleared."</summary>
    public void ClearLog()
    {
        _logListView.BeginUpdate();
        try
        {
            _logListView.Items.Clear();
            _loggedPathCounts.Clear();
        }
        finally
        {
            _logListView.EndUpdate();
        }

        _statusLabel.Text = "Log cleared.";
    }

    /// <summary>Opens the file for the first selected log item; sets status text and appends "File opened." when launch succeeds. Never modifies the log.</summary>
    public void OpenLogSelected()
    {
        if (_logListView.SelectedItems.Count == 0)
            return;

        ListViewItem item = _logListView.SelectedItems[0];
        _statusLabel.Text = string.Create(CultureInfo.InvariantCulture, $"Selected \"{item.SubItems[1].Text}\" from log.");

        if (OpenItem(item))
            _statusLabel.Text += "  File opened.";
    }

    /// <summary>Sets the status strip to random or manual selection for the given list item.</summary>
    private void SetFileStatus(ListViewItem item, bool randomChoice)
    {
        _statusLabel.Text = randomChoice
            ? string.Create(CultureInfo.InvariantCulture, $"Randomly chose \"{item.Text}\".")
            : string.Create(CultureInfo.InvariantCulture, $"Selected \"{item.Text}\".");
    }

    /// <summary>Opens the first selected item when present; sets manual-selection status and appends "File opened." when launch succeeds.</summary>
    public void OpenSelected()
    {
        if (_listView.SelectedItems.Count == 0)
            return;

        ListViewItem item = _listView.SelectedItems[0];
        SetFileStatus(item, randomChoice: false);
        LogSelection(item);

        if (OpenItem(item))
            _statusLabel.Text += "  File opened.";
    }

    /// <summary>Opens the item's file with the default shell handler and, on success, sets its stored Last Accessed time to now; reports errors except a user-cancelled UAC prompt; returns whether the default handler was started.</summary>
    public bool OpenItem(ListViewItem item)
    {
        if (item.Tag is not FileListEntry entry)
            return false;

        string filePath = entry.FullPath;
        if (string.IsNullOrEmpty(filePath))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
            entry.File.LastAccessTime = DateTime.Now;
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // The operation was canceled by the user (e.g. UAC elevation dismissed).
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _owner,
                $"Unable to open \"{filePath}\": {ex.Message}",
                "Error Opening File",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>Appends list items for folder files and, when requested, descendant folders.</summary>
    private static void AppendFolderItems(FolderNode folder, bool includeSubfolders, List<ListViewItem> items)
    {
        foreach (RetainedFile file in folder.Files)
        {
            string fullPath = Path.Combine(folder.FullPath, file.FileName);
            items.Add(new ListViewItem(file.FileName)
            {
                Tag = new FileListEntry(fullPath, file),
                ToolTipText = fullPath
            });
        }

        if (includeSubfolders)
        {
            foreach (FolderNode child in folder.Children)
                AppendFolderItems(child, true, items);
        }
    }
}