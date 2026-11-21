namespace Random_File_Select;

/// <summary>Coordinates the modal load dialog, the folders TreeView, the files ListView population, and the status strip feedback.</summary>
public sealed class FoldersTreeController
{
    private readonly TreeView _tree;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripProgressBar _progressBar;
    private readonly Form _owner;
    private readonly ListView _filesListView;
    private readonly CheckBox _subFoldersCheckBox;
    private bool _isActivating;

    /// <summary>True after a non-PlusMinus left double-click until the next message-loop turn; used to cancel BeforeExpand/Collapse.</summary>
    private bool _suppressExpandCollapse;

    /// <summary>Binds the controller to the TreeView, status label, progress bar, owner form, files ListView, and subfolders checkbox, and wires double-click expand/collapse suppression on the tree.</summary>
    public FoldersTreeController(
        TreeView tree,
        ToolStripStatusLabel statusLabel,
        ToolStripProgressBar progressBar,
        Form owner,
        ListView filesListView,
        CheckBox subFoldersCheckBox)
    {
        _tree = tree;
        _statusLabel = statusLabel;
        _progressBar = progressBar;
        _owner = owner;
        _filesListView = filesListView;
        _subFoldersCheckBox = subFoldersCheckBox;

        _tree.MouseDown += OnTreeMouseDown;
        _tree.BeforeExpand += OnTreeBeforeExpandOrCollapse;
        _tree.BeforeCollapse += OnTreeBeforeExpandOrCollapse;
    }

    /// <summary>Flags non-PlusMinus left double-clicks to cancel the upcoming expand/collapse and schedules the flag reset on the next message-loop turn.</summary>
    private void OnTreeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Clicks >= 2
            && _tree.HitTest(e.Location).Location != TreeViewHitTestLocations.PlusMinus)
        {
            _suppressExpandCollapse = true;
            _tree.BeginInvoke(new Action(() => _suppressExpandCollapse = false));
        }
    }

    /// <summary>Cancels TreeView expand/collapse events triggered by a flagged double-click.</summary>
    private void OnTreeBeforeExpandOrCollapse(object? sender, TreeViewCancelEventArgs e)
        => e.Cancel |= _suppressExpandCollapse;

    /// <summary>Scans the roots behind a modal progress dialog, then updates the tree (on success) and the status strip (always).</summary>
    public void Load(IReadOnlyList<string> roots, FileFilter filter)
    {
        // No re-entrancy guard needed: the modal dialog disables the owner form, so a second load cannot start.
        using var dialog = new FolderLoadProgressDialog(
            (token, progress) => FolderTreeBuilder.Scan(roots, filter, token, progress));
        dialog.ShowDialog(_owner);

        FolderLoadOutcome outcome = dialog.Outcome
            ?? new FolderLoadOutcome(FolderLoadStatus.Cancelled, null, null, TimeSpan.Zero);

        switch (outcome.Status)
        {
            case FolderLoadStatus.Completed when outcome.Result is not null:
                Populate(outcome.Result);
                SetStatus(FolderTreeStatusFormatter.FormatCompleted(outcome.Result),
                    FolderTreeStatusFormatter.FormatTooltip(outcome.Result));
                break;

            case FolderLoadStatus.Faulted when outcome.Error is not null:
                SetStatus(FolderTreeStatusFormatter.FormatFaulted(outcome.Error), string.Empty);
                MessageBox.Show(_owner, outcome.Error.Message, "Load folders",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            default:
                SetStatus(FolderTreeStatusFormatter.FormatCancelled(outcome.Elapsed), string.Empty);
                break;
        }
    }

    /// <summary>Locks the owner form and runs a marquee progress bar while asynchronously populating the files ListView and updating the status strip.</summary>
    public async Task OnFolderActivatedAsync(TreeNode node)
    {
        if (_isActivating || node.Tag is not FolderNode folder)
            return;

        _isActivating = true;

        // Disable only the form's top-level controls, except the one hosting the progress bar (the status strip),
        // so the marquee stays enabled and animates. Remember which were enabled so they can be restored.
        Control? statusHost = _progressBar.Owner;
        var disabledControls = new List<Control>();
        foreach (Control control in _owner.Controls)
        {
            if (control == statusHost || !control.Enabled)
                continue;
            control.Enabled = false;
            disabledControls.Add(control);
        }

        _owner.UseWaitCursor = true;
        _statusLabel.Text = "Populating...";
        _statusLabel.ToolTipText = string.Empty;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 30;
        _progressBar.Visible = true;
        _progressBar.Owner?.Refresh(); // paint the bar before the work starts

        try
        {
            bool includeSubfolders = _subFoldersCheckBox.Checked;
            ListViewItem[] items = await Task.Run(() =>
            {
                int capacity = includeSubfolders ? folder.SubtreeFileCount : folder.Files.Length;
                var list = new List<ListViewItem>(capacity);
                AppendFolderItems(folder, includeSubfolders, list);
                return list.ToArray();
            });

            _filesListView.BeginUpdate();
            try
            {
                _filesListView.Items.Clear();
                if (items.Length > 0)
                    _filesListView.Items.AddRange(items);
            }
            finally
            {
                _filesListView.EndUpdate();
            }

            SetStatus(FolderTreeStatusFormatter.FormatFolderActivated(items.Length, folder.DisplayName), folder.FullPath);
        }
        finally
        {
            _progressBar.Visible = false;
            _owner.UseWaitCursor = false;
            foreach (Control control in disabledControls)
                control.Enabled = true;
            _isActivating = false;
        }
    }

    /// <summary>
    /// Traverses folder files and appends ListViewItems with filename in Text and fullpath in Tag and ToolTipText.
    /// Recursively traverses child nodes when includeSubfolders is true.
    /// </summary>
    private static void AppendFolderItems(FolderNode folder, bool includeSubfolders, List<ListViewItem> items)
    {
        foreach (RetainedFile file in folder.Files)
        {
            string fullPath = Path.Combine(folder.FullPath, file.FileName);
            items.Add(new ListViewItem(file.FileName)
            {
                Tag = fullPath,
                ToolTipText = fullPath
            });
        }

        if (includeSubfolders)
        {
            foreach (FolderNode child in folder.Children)
                AppendFolderItems(child, true, items);
        }
    }

    /// <summary>Replaces the tree contents with one top-level node per root, inside balanced BeginUpdate/EndUpdate.</summary>
    private void Populate(FolderScanResult result)
    {
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            foreach (FolderNode root in result.Roots)
                _tree.Nodes.Add(CreateNode(root));
        }
        finally
        {
            _tree.EndUpdate();
        }
    }

    /// <summary>Recursively converts a folder node into a TreeNode carrying the FolderNode in its Tag and its tooltip.</summary>
    private static TreeNode CreateNode(FolderNode folder)
    {
        var node = new TreeNode(folder.DisplayName)
        {
            Tag = folder,
            ToolTipText = FolderTreeStatusFormatter.FormatNodeTooltip(folder)
        };
        foreach (FolderNode child in folder.Children)
            node.Nodes.Add(CreateNode(child));

        return node;
    }

    /// <summary>Sets the status label text and tooltip (empty tooltip clears it).</summary>
    private void SetStatus(string text, string tooltip)
    {
        _statusLabel.Text = text;
        _statusLabel.ToolTipText = tooltip;
    }
}
