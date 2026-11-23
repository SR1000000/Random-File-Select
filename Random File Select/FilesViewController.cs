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

    /// <summary>Rows backing the virtual files ListView.</summary>
    private FileRow[] _rows = [];

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
        _listView.RetrieveVirtualItem += OnRetrieveVirtualItem;
    }

    /// <summary>Builds the flat row array for a folder's files (and its subfolders' files when requested); safe to call off the UI thread.</summary>
    public static FileRow[] BuildRows(FolderNode folder, bool includeSubfolders)
    {
        var rows = new FileRow[includeSubfolders ? folder.SubtreeFileCount : folder.Files.Length];
        int index = 0;
        AppendFolderRows(folder, includeSubfolders, rows, ref index);
        return rows;
    }

    /// <summary>Replaces the virtual list contents with the given rows, clearing selection and scrolling to the top.</summary>
    public void SetRows(FileRow[] rows)
    {
        _listView.BeginUpdate();
        try
        {
            _listView.SelectedIndices.Clear();
            _rows = rows;
            _listView.VirtualListSize = rows.Length;
            if (rows.Length > 0)
                _listView.EnsureVisible(0);
        }
        finally
        {
            _listView.EndUpdate();
        }
    }

    /// <summary>Supplies the virtual item for the requested index.</summary>
    private void OnRetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
    {
        if ((uint)e.ItemIndex >= (uint)_rows.Length)
        {
            e.Item = new ListViewItem(string.Empty);
            return;
        }

        FileRow row = _rows[e.ItemIndex];
        e.Item = new ListViewItem(row.File.FileName) { ToolTipText = row.FullPath };
    }

    /// <summary>Picks an item by tournament selection on Last Accessed, skipping files already in the log when Exclude Duplicates is checked, then selects it (and opens when Instantly Open is checked);
    /// returns false when the list is empty, or after showing a dialog when every file is already logged.</summary>
    public bool SelectRandom()
    {
        int count = _rows.Length;
        if (count == 0)
            return false;

        Func<int, DateTime> timestampAt = i => _rows[i].File.LastAccessTime;
        int value = _lastAccessWeight.Value, min = _lastAccessWeight.Minimum, max = _lastAccessWeight.Maximum;

        int index = !_excludeDup.Checked || _loggedPathCounts.Count == 0
            ? RandomSelector.PickTournamentIndex(count, timestampAt, value, min, max, Random.Shared)
            : RandomSelector.PickTournamentIndex(
                count, timestampAt, i => !_loggedPathCounts.ContainsKey(_rows[i].FullPath), value, min, max, Random.Shared);

        if (index < 0)
        {
            MessageBox.Show(
                _owner,
                "Every file in this list is already in the log. Clear the log or uncheck the exclusion option to pick again.",
                "No Files Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.None);
            return false;
        }

        return SelectItemAtIndex(index, randomChoice: true);
    }

    /// <summary>Steps to the adjacent list item (wrapping) from the current selection, then selects it (and opens when Instantly Open is checked); returns false when the list is empty or nothing is selected.</summary>
    public bool SelectRelative(int delta)
    {
        int count = _rows.Length;
        if (count == 0 || _listView.SelectedIndices.Count == 0)
            return false;

        int index = WrapFileIndex(_listView.SelectedIndices[0], delta, count);
        return SelectItemAtIndex(index, randomChoice: false);
    }

    /// <summary>Selects, focuses, and reveals the item at index; sets status text for random vs sequential choice; opens when Instantly Open is checked.</summary>
    private bool SelectItemAtIndex(int index, bool randomChoice)
    {
        if ((uint)index >= (uint)_rows.Length)
            return false;

        FileRow row = _rows[index];
        _listView.SelectedIndices.Clear();
        _listView.SelectedIndices.Add(index);
        _listView.FocusedItem = _listView.Items[index];
        _listView.EnsureVisible(index);
        _listView.Focus();

        SetFileStatus(row, randomChoice);
        LogSelection(row);

        if (_instantOpen.Checked && OpenFile(row))
            _statusLabel.Text += "  File opened.";

        return true;
    }

    /// <summary>Steps index by delta within [0, count), wrapping at both ends.</summary>
    private static int WrapFileIndex(int index, int delta, int count)
        => ((index + delta) % count + count) % count;

    /// <summary>Appends the item's directory, filename, and current Last Accessed time to the bottom of the log, tagged with the same FileRow so it can be opened later; drops the oldest row once the log exceeds MaxLogEntries; increments the path's logged count and decrements the evicted row's.</summary>
    private void LogSelection(FileRow row)
    {
        string fullPath = row.FullPath;

        var logItem = new ListViewItem(new string[]
        {
            row.FolderPath,
            row.File.FileName,
            row.File.LastAccessTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        })
        {
            Tag = row,
            ToolTipText = fullPath
        };

        _logListView.Items.Add(logItem);
        _loggedPathCounts[fullPath] = _loggedPathCounts.GetValueOrDefault(fullPath) + 1;
        if (_logListView.Items.Count > MaxLogEntries)
        {
            string evictedPath = ((FileRow)_logListView.Items[0].Tag!).FullPath;
            _logListView.Items.RemoveAt(0);
            if (--_loggedPathCounts[evictedPath] == 0)
                _loggedPathCounts.Remove(evictedPath);
        }

        logItem.EnsureVisible();
        _logListView.Update();
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

        if (OpenFile((FileRow)item.Tag!))
            _statusLabel.Text += "  File opened.";
    }

    /// <summary>Sets the status strip to random or manual selection for the given row.</summary>
    private void SetFileStatus(FileRow row, bool randomChoice)
    {
        _statusLabel.Text = randomChoice
            ? string.Create(CultureInfo.InvariantCulture, $"Randomly chose \"{row.File.FileName}\".")
            : string.Create(CultureInfo.InvariantCulture, $"Selected \"{row.File.FileName}\".");
    }

    /// <summary>Opens the first selected row when present; sets manual-selection status and appends "File opened." when launch succeeds.</summary>
    public void OpenSelected()
    {
        if (_listView.SelectedIndices.Count == 0)
            return;

        FileRow row = _rows[_listView.SelectedIndices[0]];
        SetFileStatus(row, randomChoice: false);
        LogSelection(row);

        if (OpenFile(row))
            _statusLabel.Text += "  File opened.";
    }

    /// <summary>Opens the row's file with the default shell handler and, on success, sets its stored Last Accessed time to now; reports errors except a user-cancelled UAC prompt; returns whether the default handler was started.</summary>
    private bool OpenFile(FileRow row)
    {
        string filePath = row.FullPath;
        if (string.IsNullOrEmpty(filePath))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
            row.File.LastAccessTime = DateTime.Now;
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

    /// <summary>Writes rows for folder files and, when requested, descendant folders (own files first, depth-first).</summary>
    private static void AppendFolderRows(FolderNode folder, bool includeSubfolders, FileRow[] rows, ref int index)
    {
        foreach (RetainedFile file in folder.Files)
            rows[index++] = new FileRow(folder.FullPath, file);

        if (includeSubfolders)
        {
            foreach (FolderNode child in folder.Children)
                AppendFolderRows(child, true, rows, ref index);
        }
    }
}