namespace Random_File_Select
{
    public partial class Form1 : Form
    {
        private readonly ISettingsStore _settingsStore;
        private readonly AppSettings _settings;
        private readonly FiltersController filtersController;
        private readonly RootFoldersController rootFoldersController;
        private readonly FilesViewController filesViewController;
        private readonly FoldersTreeController foldersTreeController;

        /// <summary>Parameterless constructor kept for the WinForms Designer.</summary>
        public Form1() : this(new AppSettings(), new SettingsStore(SettingsStore.DefaultFilePath))
        {
        }

        public Form1(AppSettings settings, ISettingsStore settingsStore)
        {
            InitializeComponent();
            _settings = settings;
            _settingsStore = settingsStore;

            Settings_comboBox_DarkLight.DataSource = Enum.GetValues<AppColorMode>();
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

            foldersTreeController.FolderActivated += (_, _) => AutoPickFileIfEnabled(true);

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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ApplySettings();

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

        /// <summary>Populates UI controls, column widths, and controllers from the current settings.</summary>
        private void ApplySettings()
        {
            rootFoldersController.Load(_settings.RootFolders);

            BehaviorSettings behavior = _settings.Behavior;
            checkBox_InstantFile.Checked = behavior.ChooseRandomFileAfterDir;
            checkBox_InstantOpen.Checked = behavior.InstantlyOpenChosenFiles;
            Settings_checkBox_Subfolders.Checked = behavior.IncludeSubfolders;
            Settings_comboBox_DarkLight.SelectedItem = behavior.ColorMode;
            FileDetails_checkBox_ExcludeDup.Checked = behavior.ExcludeDup;
            Settings_trackBar_LastAccessWeight.Value = Math.Clamp(
                behavior.LastAccessWeight,
                Settings_trackBar_LastAccessWeight.Minimum,
                Settings_trackBar_LastAccessWeight.Maximum);

            ApplyColumnWidth(FileDetails_Header_Directory, _settings.Layout.FileDetailsDirectoryColumnWidth);
            ApplyColumnWidth(FileDetails_Header_Filename, _settings.Layout.FileDetailsFilenameColumnWidth);
            ApplyColumnWidth(FileDetails_Header_LastAccess, _settings.Layout.FileDetailsLastAccessColumnWidth);

            ApplyFilterSettings(_settings.Filters);
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
            var layout = _settings.Layout;
            var behavior = _settings.Behavior;

            WindowPlacementSettings? placement = NativeWindowPlacement.Get(Handle);
            if (placement is not null)
                layout.WindowPlacement = placement;

            if (WindowState != FormWindowState.Minimized)
            {
                layout.SplitMainDistance = splitContainerMain.SplitterDistance;
                layout.SplitLowerDistance = splitContainerLower.SplitterDistance;
                layout.SplitMiddleDistance = splitContainerMiddle.SplitterDistance;
            }

            layout.FileDetailsDirectoryColumnWidth = FileDetails_Header_Directory.Width;
            layout.FileDetailsFilenameColumnWidth = FileDetails_Header_Filename.Width;
            layout.FileDetailsLastAccessColumnWidth = FileDetails_Header_LastAccess.Width;

            _settings.RootFolders = rootFoldersController.Export();

            behavior.ChooseRandomFileAfterDir = checkBox_InstantFile.Checked;
            behavior.InstantlyOpenChosenFiles = checkBox_InstantOpen.Checked;
            behavior.IncludeSubfolders = Settings_checkBox_Subfolders.Checked;
            behavior.ColorMode = Settings_comboBox_DarkLight.SelectedItem is AppColorMode mode
                ? mode
                : AppColorMode.System;
            behavior.ExcludeDup = FileDetails_checkBox_ExcludeDup.Checked;
            behavior.LastAccessWeight = Settings_trackBar_LastAccessWeight.Value;

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
