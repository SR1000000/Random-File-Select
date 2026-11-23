namespace Random_File_Select
{
    public partial class Form1 : Form
    {
        private readonly SettingsStore _settingsStore = new(SettingsStore.DefaultFilePath);
        private AppSettings _settings = new();
        private readonly FiltersController filtersController;
        private readonly RootFoldersController rootFoldersController;
        private readonly FilesViewController filesViewController;
        private readonly FoldersTreeController foldersTreeController;

        public Form1() : this(new AppSettings())
        {
        }

        public Form1(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            filtersController = new FiltersController(Filters_comboBox_Extensions);
            rootFoldersController = new RootFoldersController(
                RootFolders_checkedListBox,
                RootFolders_folderBrowserDialog_Add,
                RootFolders_folderBrowserDialog_Edit,
                this);

            filesViewController = new FilesViewController(
                Files_listView,
                FileDetails_listView,
                Settings_trackBar_LastAccessWeight,
                checkBox_InstantOpen,
                FileDetails_checkBox_ExcludeDup,
                BottomStatusStrip_StatusLabel,
                this);

            foldersTreeController = new FoldersTreeController(
                Folders_treeView,
                BottomStatusStrip_StatusLabel,
                this,
                filesViewController,
                Settings_checkBox_Subfolders);

            rootFoldersController.SelectionChanged += (_, _) => UpdateRootFolderButtons();
            UpdateRootFolderButtons();

            BottomStatusStrip_StatusLabel.Text = "Application Started";
        }

        /// <summary>Updates enabled state of root folder Edit and Remove buttons based on list selection.</summary>
        private void UpdateRootFolderButtons()
        {
            bool hasSelection = rootFoldersController.HasSelection;
            RootFolders_button_Edit.Enabled = hasSelection;
            RootFolders_button_Remove.Enabled = hasSelection;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CaptureSettings();
            try
            {
                _settingsStore.Save(_settings);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ApplySettings(_settings);

            WindowPlacementSettings? placement = _settings.Layout.WindowPlacement;
            if (placement is not null)
            {
                StartPosition = FormStartPosition.Manual;
                NativeWindowPlacement.Set(Handle, placement);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            var layout = _settings.Layout;
            SetSplitterSafe(splitContainerMain, layout.SplitMainDistance);
            SetSplitterSafe(splitContainerLower, layout.SplitLowerDistance);
            SetSplitterSafe(splitContainerMiddle, layout.SplitMiddleDistance);
        }

        /// <summary>Populates UI controls, column widths, and controllers from the specified settings.</summary>
        private void ApplySettings(AppSettings settings)
        {
            rootFoldersController.Load(settings.RootFolders);

            BehaviorSettings behavior = settings.Behavior;
            checkBox_InstantLoadDir.Checked = behavior.LoadRootFoldersOnStart;
            checkBox_InstantFile.Checked = behavior.ChooseRandomFileAfterDir;
            checkBox_InstantOpen.Checked = behavior.InstantlyOpenChosenFiles;
            Settings_checkBox_Subfolders.Checked = behavior.IncludeSubfolders;
            Settings_checkBox_DarkMode.Checked = behavior.DarkMode;
            FileDetails_checkBox_ExcludeDup.Checked = behavior.ExcludeDup;
            Settings_trackBar_LastAccessWeight.Value = Math.Clamp(
                behavior.LastAccessWeight,
                Settings_trackBar_LastAccessWeight.Minimum,
                Settings_trackBar_LastAccessWeight.Maximum);

            ApplyColumnWidth(FileDetails_Header_Directory, settings.Layout.FileDetailsDirectoryColumnWidth);
            ApplyColumnWidth(FileDetails_Header_Filename, settings.Layout.FileDetailsFilenameColumnWidth);
            ApplyColumnWidth(FileDetails_Header_LastAccess, settings.Layout.FileDetailsLastAccessColumnWidth);

            ApplyFilterSettings(settings.Filters);
        }

        /// <summary>Applies filter settings to filter controls.</summary>
        private void ApplyFilterSettings(FilterSettings filters)
        {
            filtersController.Load(filters);
            Filters_textBox_Include.Text = filters.Include;
            Filters_textBox_Exclude.Text = filters.Exclude;
            if (filters.DateModifiedMin.HasValue)
                Filters_dateTimePicker_Min.Value = filters.DateModifiedMin.Value;

            // DateModifiedMax is never restored; Max always starts at today. Uncheck after assigning Value,
            // because assigning Value can re-check the box.
            Filters_dateTimePicker_Max.Value = DateTime.Today;
            Filters_dateTimePicker_Min.Checked = false;
            Filters_dateTimePicker_Max.Checked = false;
        }

        /// <summary>Sets a column header width if the specified width is positive.</summary>
        private static void ApplyColumnWidth(ColumnHeader header, int width)
        {
            if (width > 0)
                header.Width = width;
        }

        /// <summary>Captures current window placement, splitters, column widths, behaviors, folders, and filters into settings.</summary>
        private void CaptureSettings()
        {
            WindowPlacementSettings? placement = NativeWindowPlacement.Get(Handle);
            if (placement is not null)
                _settings.Layout.WindowPlacement = placement;

            if (WindowState != FormWindowState.Minimized)
            {
                _settings.Layout.SplitMainDistance = splitContainerMain.SplitterDistance;
                _settings.Layout.SplitLowerDistance = splitContainerLower.SplitterDistance;
                _settings.Layout.SplitMiddleDistance = splitContainerMiddle.SplitterDistance;
            }

            _settings.Layout.FileDetailsDirectoryColumnWidth = FileDetails_Header_Directory.Width;
            _settings.Layout.FileDetailsFilenameColumnWidth = FileDetails_Header_Filename.Width;
            _settings.Layout.FileDetailsLastAccessColumnWidth = FileDetails_Header_LastAccess.Width;

            _settings.RootFolders = rootFoldersController.Export();

            _settings.Behavior.LoadRootFoldersOnStart = checkBox_InstantLoadDir.Checked;
            _settings.Behavior.ChooseRandomFileAfterDir = checkBox_InstantFile.Checked;
            _settings.Behavior.InstantlyOpenChosenFiles = checkBox_InstantOpen.Checked;
            _settings.Behavior.IncludeSubfolders = Settings_checkBox_Subfolders.Checked;
            _settings.Behavior.DarkMode = Settings_checkBox_DarkMode.Checked;
            _settings.Behavior.ExcludeDup = FileDetails_checkBox_ExcludeDup.Checked;
            _settings.Behavior.LastAccessWeight = Settings_trackBar_LastAccessWeight.Value;

            _settings.Filters = CaptureFilterSettings();
        }

        /// <summary>Captures text and date filter inputs into a FilterSettings object.</summary>
        private FilterSettings CaptureFilterSettings()
        {
            FilterSettings captured = filtersController.Export();
            captured.Include = Filters_textBox_Include.Text;
            captured.Exclude = Filters_textBox_Exclude.Text;
            captured.DateModifiedMin = Filters_dateTimePicker_Min.Checked ? Filters_dateTimePicker_Min.Value : null;
            captured.DateModifiedMax = Filters_dateTimePicker_Max.Checked ? Filters_dateTimePicker_Max.Value : null;
            return captured;
        }

        /// <summary>Sets a SplitContainer splitter distance clamped within valid panel min/max bounds.</summary>
        private static void SetSplitterSafe(SplitContainer sc, int savedDistance)
        {
            if (savedDistance <= 0)
                return;

            int totalSize = sc.Orientation == Orientation.Horizontal ? sc.Height : sc.Width;
            int maxDistance = totalSize - sc.Panel2MinSize - sc.SplitterWidth;

            if (maxDistance >= sc.Panel1MinSize)
            {
                sc.SplitterDistance = Math.Clamp(savedDistance, sc.Panel1MinSize, maxDistance);
            }
        }

        private void RootFolders_button_Add_Click(object sender, EventArgs e)
        {
            rootFoldersController.Add();
        }

        private void RootFolders_button_Edit_Click(object sender, EventArgs e)
        {
            rootFoldersController.EditSelected();
        }

        private void RootFolders_button_Remove_Click(object sender, EventArgs e)
        {
            rootFoldersController.RemoveSelected();
        }

        private void Filters_button_ExtAdd_Click(object sender, EventArgs e)
        {
            filtersController.AddExtensionGroup();
        }

        private void Filters_button_ExtRem_Click(object sender, EventArgs e)
        {
            filtersController.RemoveExtensionGroup();
        }

        /// <summary>Loads checked root folders into the folder tree using the current filters; does nothing if none are checked.</summary>
        private void Folders_toolStripButton_Load_Click(object sender, EventArgs e)
        {
            IReadOnlyList<string> checkedPaths = rootFoldersController.CheckedPaths;
            if (checkedPaths.Count == 0)
                return;

            FileFilter filter = FileFilter.FromSettings(CaptureFilterSettings());
            if (!filter.HasExtensionRestriction)
            {
                MessageBox.Show(
                    this,
                    "No extension filter is entered. All file types will be scanned.",
                    "No extension filter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            foldersTreeController.Load(checkedPaths, filter);
        }

        private void Folders_toolStripButton_Prev_Click(object sender, EventArgs e)
        {
            AutoPickFileIfEnabled(foldersTreeController.SelectRelativeFolder(-1));
        }

        private void Folders_toolStripButton_Random_Click(object sender, EventArgs e)
        {
            AutoPickFileIfEnabled(foldersTreeController.SelectRandomFolder());
        }

        private void Folders_toolStripButton_Next_Click(object sender, EventArgs e)
        {
            AutoPickFileIfEnabled(foldersTreeController.SelectRelativeFolder(1));
        }

        private void Files_toolStripButton_Reload_Click(object sender, EventArgs e)
        {
            foldersTreeController.ReloadFilesFromSelectedFolder();
        }

        private void Files_toolStripButton_Prev_Click(object sender, EventArgs e)
        {
            filesViewController.SelectRelative(-1);
        }

        private void Files_toolStripButton_Random_Click(object sender, EventArgs e)
        {
            filesViewController.SelectRandom();
        }

        private void Files_toolStripButton_Next_Click(object sender, EventArgs e)
        {
            filesViewController.SelectRelative(1);
        }

        private void RootFolders_folderBrowserDialog_Edit_HelpRequest(object sender, EventArgs e)
        {

        }

        /// <summary>Activates the double-clicked folder node, populating the files ListView, and auto-picks a file when that setting is enabled.</summary>
        private void Folders_treeView_DblClick(object sender, EventArgs e)
        {
            TreeViewHitTestInfo hit = Folders_treeView.HitTest(Folders_treeView.PointToClient(Cursor.Position));
            if (hit.Node is not null
                && hit.Location is TreeViewHitTestLocations.Label or TreeViewHitTestLocations.RightOfLabel)
            {
                AutoPickFileIfEnabled(foldersTreeController.OnFolderActivated(hit.Node));
            }
        }

        /// <summary>Picks a random file when a folder was just activated and "Choose a random file after selecting a directory" is checked.</summary>
        private void AutoPickFileIfEnabled(bool activated)
        {
            if (activated && checkBox_InstantFile.Checked)
                filesViewController.SelectRandom();
        }

        /// <summary>Runs the file indicated by the activated item using default Windows behavior.</summary>
        private void Files_listView_Activate(object sender, EventArgs e)
        {
            filesViewController.OpenSelected();
        }

        /// <summary>Runs the file indicated by the activated log item using default Windows behavior.</summary>
        private void FileDetails_listView_Activate(object sender, EventArgs e)
        {
            filesViewController.OpenLogSelected();
        }

        private void Settings_button_About_Click(object sender, EventArgs e)
        {
            using var dialog = new AboutDialog();
            dialog.ShowDialog(this);
        }

        private void FileDetails_button_ClearLog_Click(object sender, EventArgs e)
        {
            filesViewController.ClearLog();
        }
    }
}
