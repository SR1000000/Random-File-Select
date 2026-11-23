namespace Random_File_Select;

/// <summary>Coordinates the modal load dialog, the folders TreeView, the files ListView population, and the status strip feedback.</summary>
public sealed class FoldersTreeController
{
    private readonly TreeView _tree;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly Form _owner;
    private readonly FilesViewController _filesView;
    private readonly CheckBox _subFoldersCheckBox;
    private readonly List<TreeNode> _matchingNodes = [];
    /// <summary>True after a non-PlusMinus left double-click until the next message-loop turn; used to cancel BeforeExpand/Collapse.</summary>
    private bool _suppressExpandCollapse;

    /// <summary>Binds the controller to the TreeView, status label, owner form, files view controller, and subfolders checkbox, and wires double-click expand/collapse suppression on the tree.</summary>
    public FoldersTreeController(
        TreeView tree,
        ToolStripStatusLabel statusLabel,
        Form owner,
        FilesViewController filesView,
        CheckBox subFoldersCheckBox)
    {
        _tree = tree;
        _statusLabel = statusLabel;
        _owner = owner;
        _filesView = filesView;
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

    /// <summary>Repopulates the files ListView from the tree's selected folder; returns false when there is no selection or activation is skipped.</summary>
    public bool ReloadFilesFromSelectedFolder()
    {
        TreeNode? selected = _tree.SelectedNode;
        if (selected is null)
            return false;

        return OnFolderActivated(selected);
    }

    /// <summary>Returns whether activating this folder would show at least one file for the given subfolder option.</summary>
    internal static bool ShouldPopulateListView(FolderNode folder, bool includeSubfolders)
        => includeSubfolders ? folder.SubtreeFileCount > 0 : folder.OwnFileCount > 0;

    /// <summary>Populates the virtual files ListView from the folder node and updates the status strip; returns false when skipped, otherwise true.</summary>
    public bool OnFolderActivated(TreeNode node)
    {
        if (node.Tag is not FolderNode folder)
            return false;

        bool includeSubfolders = _subFoldersCheckBox.Checked;
        if (!ShouldPopulateListView(folder, includeSubfolders))
            return false;

        FileRow[] rows = FilesViewController.BuildRows(folder, includeSubfolders);
        _filesView.SetRows(rows);
        SetStatus(FolderTreeStatusFormatter.FormatFolderActivated(rows.Length, folder.DisplayName), folder.FullPath);
        return true;
    }

    /// <summary>Uniformly picks a cached node with matching files, then selects and activates it; returns true if the files ListView was populated.</summary>
    public bool SelectRandomFolder()
    {
        if (_matchingNodes.Count == 0)
            return false;

        TreeNode node = _matchingNodes[Random.Shared.Next(_matchingNodes.Count)];
        return ActivateNode(node);
    }

    /// <summary>Moves to the next or previous matching-folder node (wrapping) from the current tree selection, then activates it; returns false when navigation or populate is skipped.</summary>
    public bool SelectRelativeFolder(int delta)
    {
        int count = _matchingNodes.Count;
        if (count == 0)
            return false;

        TreeNode? selected = _tree.SelectedNode;
        if (selected is null)
            return false;

        int index = _matchingNodes.IndexOf(selected);
        if (index < 0)
            return false;

        int nextIndex = WrapMatchingIndex(index, delta, count);
        return ActivateNode(_matchingNodes[nextIndex]);
    }

    /// <summary>Selects and reveals a folder node, then activates it; returns whether the files ListView was populated.</summary>
    private bool ActivateNode(TreeNode node)
    {
        _tree.SelectedNode = node;
        node.EnsureVisible();
        return OnFolderActivated(node);
    }

    /// <summary>Steps index by delta within [0, count), wrapping at both ends.</summary>
    private static int WrapMatchingIndex(int index, int delta, int count)
        => ((index + delta) % count + count) % count;

    /// <summary>Replaces the tree contents with one top-level node per root, refreshing the matching-folder cache, inside balanced BeginUpdate/EndUpdate.</summary>
    private void Populate(FolderScanResult result)
    {
        _matchingNodes.Clear();
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

    /// <summary>Recursively converts a folder node into a TreeNode carrying the FolderNode in its Tag and its tooltip, caching nodes that directly contain matching files.</summary>
    private TreeNode CreateNode(FolderNode folder)
    {
        var node = new TreeNode(folder.DisplayName)
        {
            Tag = folder,
            ToolTipText = FolderTreeStatusFormatter.FormatNodeTooltip(folder)
        };

        if (folder.OwnFileCount > 0)
            _matchingNodes.Add(node);

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