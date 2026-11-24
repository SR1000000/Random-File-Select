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
            // Set here rather than in the Designer so the title tracks the assembly's product name and version.
            Text = $"{Application.ProductName} v{Application.ProductVersion.Split('+')[0]}";
            _settings = settings;
            _settingsStore = settingsStore;

            Settings_comboBox_DarkLight.DataSource = Enum.GetValues<AppColorMode>();
            filtersController = new FiltersController(
                Filters_comboBox_Extensions,
                Filters_textBox_Include,
                Filters_textBox_Exclude,
                Filters_dateTimePicker_Min,
                Filters_dateTimePicker_Max);
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

            Settings_button_Help.Click += Settings_button_Help_Click;

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
                NativeWindowPlacement.Set(Handle, placement);
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

            filtersController.Load(_settings.Filters);
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

            _settings.Filters = filtersController.Export();
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

            FileFilter filter = FileFilter.FromSettings(filtersController.Export());
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

        private void Settings_button_Help_Click(object? sender, EventArgs e)
        {
            const string steps =
                "1. Add folders: in the Root Folders panel, click Add and choose a folder to add to the scan. Use Edit or Remove to change the list. Checkmark the folders you want to use.\r\n\r\n" +
                "2. Set filters (optional): choose or add file extensions (not recommended to have an empty extensions filter), and enter text to include or exclude in names. Set a minimum and maximum date if needed (tick to enable).\r\n\r\n" +
                "3. Load folders: click Load in the Folders toolbar. The checked root folders are scanned and shown as tree nodes.\r\n\r\n" +
                "4. Pick a folder: double-click a folder in the tree, or use the Random button in the Folders toolbar.\r\n\r\n" +
                "5. Randomly select a file: click the Random button in the Files toolbar, or use Previous or Next to step through the list manually. \nMoving the last-access weight slider left favors files you haven't opened recently.\r\n\r\n" +
                "6. Open the file: double-click it in the bottom Log box, or tick the instant-open option so randomly chosen files open automatically. \nDouble-clicking items in the Files list counts as selecting then opening.\r\n\r\n" +
                "Tip: Tick \"Choose a random file after selecting a directory\" to pick a file as soon as a folder is chosen. The log lists files you've selected; double-click an entry to open it again. \n" +
                "Checking the \"Chosen files include those within subfiles\" box allows you to select folders in the tree that have no matching files.";

            MessageBox.Show(this, steps, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FileDetails_button_ClearLog_Click(object sender, EventArgs e)
        {
            filesViewController.ClearLog();
        }
    }
}
