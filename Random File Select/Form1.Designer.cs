namespace Random_File_Select
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            Bottom_StatusStrip = new StatusStrip();
            BottomStatusStrip_StatusLabel = new ToolStripStatusLabel();
            FileDetails_listView = new ListView();
            FileDetails_Header_Directory = new ColumnHeader();
            FileDetails_Header_Filename = new ColumnHeader();
            FileDetails_Header_LastAccess = new ColumnHeader();
            TableLayoutPanel_Top = new TableLayoutPanel();
            TopPanel_groupBoxLeft = new GroupBox();
            RootFolders_checkedListBox = new CheckedListBox();
            panel2 = new Panel();
            RootFolders_button_Edit = new Button();
            RootFolders_button_Remove = new Button();
            RootFolders_button_Add = new Button();
            TopPanel_groupBoxCenter = new GroupBox();
            label8 = new Label();
            label7 = new Label();
            Filters_dateTimePicker_Min = new DateTimePicker();
            Filters_dateTimePicker_Max = new DateTimePicker();
            Filters_button_ExtRem = new Button();
            Filters_button_ExtAdd = new Button();
            label6 = new Label();
            Filters_comboBox_Extensions = new ComboBox();
            Filters_textBox_Exclude = new TextBox();
            Filters_textBox_Include = new TextBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            TopPanel_groupBoxRight = new GroupBox();
            label9 = new Label();
            label5 = new Label();
            Settings_comboBox_DarkLight = new ComboBox();
            Settings_button_Help = new Button();
            Settings_button_About = new Button();
            Settings_checkBox_Subfolders = new CheckBox();
            label4 = new Label();
            Settings_trackBar_LastAccessWeight = new TrackBar();
            checkBox_InstantOpen = new CheckBox();
            checkBox_InstantFile = new CheckBox();
            FileDetails_button_ClearLog = new Button();
            splitContainerMiddle = new SplitContainer();
            Folders_treeView = new TreeView();
            Folders_toolStrip = new ClickThroughToolStrip();
            Folders_toolStripButton_Load = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripLabel1 = new ToolStripLabel();
            Folders_toolStripButton_Prev = new ToolStripButton();
            Folders_toolStripButton_Random = new ToolStripButton();
            Folders_toolStripButton_Next = new ToolStripButton();
            Files_listView = new ListView();
            Files_toolStrip = new ClickThroughToolStrip();
            Files_toolStripButton_Load = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripLabel2 = new ToolStripLabel();
            Files_toolStripButton_Prev = new ToolStripButton();
            Files_toolStripButton_Random = new ToolStripButton();
            Files_toolStripButton_Next = new ToolStripButton();
            splitContainerMain = new SplitContainer();
            splitContainerLower = new SplitContainer();
            panel1 = new Panel();
            FileDetails_checkBox_ExcludeDup = new CheckBox();
            RootFolders_folderBrowserDialog_Edit = new FolderBrowserDialog();
            RootFolders_folderBrowserDialog_Add = new FolderBrowserDialog();
            Bottom_StatusStrip.SuspendLayout();
            TableLayoutPanel_Top.SuspendLayout();
            TopPanel_groupBoxLeft.SuspendLayout();
            panel2.SuspendLayout();
            TopPanel_groupBoxCenter.SuspendLayout();
            TopPanel_groupBoxRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)Settings_trackBar_LastAccessWeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerMiddle).BeginInit();
            splitContainerMiddle.Panel1.SuspendLayout();
            splitContainerMiddle.Panel2.SuspendLayout();
            splitContainerMiddle.SuspendLayout();
            Folders_toolStrip.SuspendLayout();
            Files_toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLower).BeginInit();
            splitContainerLower.Panel1.SuspendLayout();
            splitContainerLower.Panel2.SuspendLayout();
            splitContainerLower.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Bottom_StatusStrip
            // 
            Bottom_StatusStrip.Items.AddRange(new ToolStripItem[] { BottomStatusStrip_StatusLabel });
            Bottom_StatusStrip.Location = new Point(0, 577);
            Bottom_StatusStrip.Name = "Bottom_StatusStrip";
            Bottom_StatusStrip.ShowItemToolTips = true;
            Bottom_StatusStrip.Size = new Size(646, 22);
            Bottom_StatusStrip.TabIndex = 0;
            Bottom_StatusStrip.Text = "statusStrip1";
            // 
            // BottomStatusStrip_StatusLabel
            // 
            BottomStatusStrip_StatusLabel.Name = "BottomStatusStrip_StatusLabel";
            BottomStatusStrip_StatusLabel.Size = new Size(631, 17);
            BottomStatusStrip_StatusLabel.Spring = true;
            BottomStatusStrip_StatusLabel.Text = "Placeholder";
            BottomStatusStrip_StatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // FileDetails_listView
            // 
            FileDetails_listView.Columns.AddRange(new ColumnHeader[] { FileDetails_Header_Directory, FileDetails_Header_Filename, FileDetails_Header_LastAccess });
            FileDetails_listView.Dock = DockStyle.Fill;
            FileDetails_listView.FullRowSelect = true;
            FileDetails_listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            FileDetails_listView.Location = new Point(0, 24);
            FileDetails_listView.MultiSelect = false;
            FileDetails_listView.Name = "FileDetails_listView";
            FileDetails_listView.Size = new Size(646, 110);
            FileDetails_listView.TabIndex = 1;
            FileDetails_listView.UseCompatibleStateImageBehavior = false;
            FileDetails_listView.View = View.Details;
            FileDetails_listView.ItemActivate += FileDetails_listView_Activate;
            // 
            // FileDetails_Header_Directory
            // 
            FileDetails_Header_Directory.Text = "Directory";
            FileDetails_Header_Directory.Width = 150;
            // 
            // FileDetails_Header_Filename
            // 
            FileDetails_Header_Filename.Text = "Filename";
            FileDetails_Header_Filename.Width = 300;
            // 
            // FileDetails_Header_LastAccess
            // 
            FileDetails_Header_LastAccess.Text = "Last Accessed";
            FileDetails_Header_LastAccess.Width = 120;
            // 
            // TableLayoutPanel_Top
            // 
            TableLayoutPanel_Top.ColumnCount = 3;
            TableLayoutPanel_Top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel_Top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228F));
            TableLayoutPanel_Top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 212F));
            TableLayoutPanel_Top.Controls.Add(TopPanel_groupBoxLeft, 0, 0);
            TableLayoutPanel_Top.Controls.Add(TopPanel_groupBoxCenter, 1, 0);
            TableLayoutPanel_Top.Controls.Add(TopPanel_groupBoxRight, 2, 0);
            TableLayoutPanel_Top.Dock = DockStyle.Fill;
            TableLayoutPanel_Top.Location = new Point(0, 0);
            TableLayoutPanel_Top.MinimumSize = new Size(0, 245);
            TableLayoutPanel_Top.Name = "TableLayoutPanel_Top";
            TableLayoutPanel_Top.RowCount = 1;
            TableLayoutPanel_Top.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel_Top.Size = new Size(646, 245);
            TableLayoutPanel_Top.TabIndex = 2;
            // 
            // TopPanel_groupBoxLeft
            // 
            TopPanel_groupBoxLeft.Controls.Add(RootFolders_checkedListBox);
            TopPanel_groupBoxLeft.Controls.Add(panel2);
            TopPanel_groupBoxLeft.Dock = DockStyle.Fill;
            TopPanel_groupBoxLeft.Location = new Point(3, 3);
            TopPanel_groupBoxLeft.Name = "TopPanel_groupBoxLeft";
            TopPanel_groupBoxLeft.Size = new Size(200, 239);
            TopPanel_groupBoxLeft.TabIndex = 0;
            TopPanel_groupBoxLeft.TabStop = false;
            TopPanel_groupBoxLeft.Text = "Root folders";
            // 
            // RootFolders_checkedListBox
            // 
            RootFolders_checkedListBox.CheckOnClick = true;
            RootFolders_checkedListBox.Dock = DockStyle.Fill;
            RootFolders_checkedListBox.FormattingEnabled = true;
            RootFolders_checkedListBox.HorizontalScrollbar = true;
            RootFolders_checkedListBox.IntegralHeight = false;
            RootFolders_checkedListBox.Location = new Point(3, 51);
            RootFolders_checkedListBox.Name = "RootFolders_checkedListBox";
            RootFolders_checkedListBox.Size = new Size(194, 185);
            RootFolders_checkedListBox.TabIndex = 3;
            // 
            // panel2
            // 
            panel2.Controls.Add(RootFolders_button_Edit);
            panel2.Controls.Add(RootFolders_button_Remove);
            panel2.Controls.Add(RootFolders_button_Add);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(3, 19);
            panel2.Name = "panel2";
            panel2.Size = new Size(194, 32);
            panel2.TabIndex = 4;
            // 
            // RootFolders_button_Edit
            // 
            RootFolders_button_Edit.Location = new Point(66, 3);
            RootFolders_button_Edit.Name = "RootFolders_button_Edit";
            RootFolders_button_Edit.Size = new Size(61, 23);
            RootFolders_button_Edit.TabIndex = 1;
            RootFolders_button_Edit.Text = "Edit";
            RootFolders_button_Edit.UseVisualStyleBackColor = true;
            RootFolders_button_Edit.Click += RootFolders_button_Edit_Click;
            // 
            // RootFolders_button_Remove
            // 
            RootFolders_button_Remove.Location = new Point(128, 3);
            RootFolders_button_Remove.Name = "RootFolders_button_Remove";
            RootFolders_button_Remove.Size = new Size(61, 23);
            RootFolders_button_Remove.TabIndex = 2;
            RootFolders_button_Remove.Text = "Remove";
            RootFolders_button_Remove.UseVisualStyleBackColor = true;
            RootFolders_button_Remove.Click += RootFolders_button_Remove_Click;
            // 
            // RootFolders_button_Add
            // 
            RootFolders_button_Add.Location = new Point(4, 3);
            RootFolders_button_Add.Name = "RootFolders_button_Add";
            RootFolders_button_Add.Size = new Size(61, 23);
            RootFolders_button_Add.TabIndex = 0;
            RootFolders_button_Add.Text = "Add";
            RootFolders_button_Add.UseVisualStyleBackColor = true;
            RootFolders_button_Add.Click += RootFolders_button_Add_Click;
            // 
            // TopPanel_groupBoxCenter
            // 
            TopPanel_groupBoxCenter.Controls.Add(label8);
            TopPanel_groupBoxCenter.Controls.Add(label7);
            TopPanel_groupBoxCenter.Controls.Add(Filters_dateTimePicker_Min);
            TopPanel_groupBoxCenter.Controls.Add(Filters_dateTimePicker_Max);
            TopPanel_groupBoxCenter.Controls.Add(Filters_button_ExtRem);
            TopPanel_groupBoxCenter.Controls.Add(Filters_button_ExtAdd);
            TopPanel_groupBoxCenter.Controls.Add(label6);
            TopPanel_groupBoxCenter.Controls.Add(Filters_comboBox_Extensions);
            TopPanel_groupBoxCenter.Controls.Add(Filters_textBox_Exclude);
            TopPanel_groupBoxCenter.Controls.Add(Filters_textBox_Include);
            TopPanel_groupBoxCenter.Controls.Add(label3);
            TopPanel_groupBoxCenter.Controls.Add(label2);
            TopPanel_groupBoxCenter.Controls.Add(label1);
            TopPanel_groupBoxCenter.Dock = DockStyle.Fill;
            TopPanel_groupBoxCenter.Location = new Point(209, 3);
            TopPanel_groupBoxCenter.Name = "TopPanel_groupBoxCenter";
            TopPanel_groupBoxCenter.Size = new Size(222, 239);
            TopPanel_groupBoxCenter.TabIndex = 1;
            TopPanel_groupBoxCenter.TabStop = false;
            TopPanel_groupBoxCenter.Text = "Filters";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(1, 133);
            label8.Name = "label8";
            label8.Size = new Size(117, 15);
            label8.TabIndex = 12;
            label8.Text = "Date Modified (Min):";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(3, 104);
            label7.Name = "label7";
            label7.Size = new Size(118, 15);
            label7.TabIndex = 11;
            label7.Text = "Date Modified (Max):";
            // 
            // Filters_dateTimePicker_Min
            // 
            Filters_dateTimePicker_Min.Checked = false;
            Filters_dateTimePicker_Min.Format = DateTimePickerFormat.Short;
            Filters_dateTimePicker_Min.Location = new Point(124, 127);
            Filters_dateTimePicker_Min.Name = "Filters_dateTimePicker_Min";
            Filters_dateTimePicker_Min.ShowCheckBox = true;
            Filters_dateTimePicker_Min.Size = new Size(98, 23);
            Filters_dateTimePicker_Min.TabIndex = 10;
            Filters_dateTimePicker_Min.Value = new DateTime(2000, 1, 1, 21, 13, 0, 0);
            // 
            // Filters_dateTimePicker_Max
            // 
            Filters_dateTimePicker_Max.Checked = false;
            Filters_dateTimePicker_Max.Format = DateTimePickerFormat.Short;
            Filters_dateTimePicker_Max.Location = new Point(124, 98);
            Filters_dateTimePicker_Max.Name = "Filters_dateTimePicker_Max";
            Filters_dateTimePicker_Max.ShowCheckBox = true;
            Filters_dateTimePicker_Max.Size = new Size(98, 23);
            Filters_dateTimePicker_Max.TabIndex = 9;
            // 
            // Filters_button_ExtRem
            // 
            Filters_button_ExtRem.Location = new Point(202, 69);
            Filters_button_ExtRem.Name = "Filters_button_ExtRem";
            Filters_button_ExtRem.Size = new Size(20, 23);
            Filters_button_ExtRem.TabIndex = 8;
            Filters_button_ExtRem.Text = "-";
            Filters_button_ExtRem.UseVisualStyleBackColor = true;
            Filters_button_ExtRem.Click += Filters_button_ExtRem_Click;
            // 
            // Filters_button_ExtAdd
            // 
            Filters_button_ExtAdd.Location = new Point(185, 69);
            Filters_button_ExtAdd.Name = "Filters_button_ExtAdd";
            Filters_button_ExtAdd.Size = new Size(20, 23);
            Filters_button_ExtAdd.TabIndex = 7;
            Filters_button_ExtAdd.Text = "+";
            Filters_button_ExtAdd.UseVisualStyleBackColor = true;
            Filters_button_ExtAdd.Click += Filters_button_ExtAdd_Click;
            // 
            // label6
            // 
            label6.Dock = DockStyle.Bottom;
            label6.Location = new Point(3, 186);
            label6.Name = "label6";
            label6.Size = new Size(216, 50);
            label6.TabIndex = 6;
            label6.Text = "Filters are case-insensitive substrings separated by spaces.  Changes will take effect after reloading folders.";
            // 
            // Filters_comboBox_Extensions
            // 
            Filters_comboBox_Extensions.FormattingEnabled = true;
            Filters_comboBox_Extensions.Items.AddRange(new object[] { "zip 7z rar" });
            Filters_comboBox_Extensions.Location = new Point(62, 70);
            Filters_comboBox_Extensions.Name = "Filters_comboBox_Extensions";
            Filters_comboBox_Extensions.Size = new Size(123, 23);
            Filters_comboBox_Extensions.TabIndex = 5;
            // 
            // Filters_textBox_Exclude
            // 
            Filters_textBox_Exclude.Location = new Point(62, 43);
            Filters_textBox_Exclude.Name = "Filters_textBox_Exclude";
            Filters_textBox_Exclude.Size = new Size(160, 23);
            Filters_textBox_Exclude.TabIndex = 4;
            // 
            // Filters_textBox_Include
            // 
            Filters_textBox_Include.Location = new Point(62, 16);
            Filters_textBox_Include.Name = "Filters_textBox_Include";
            Filters_textBox_Include.Size = new Size(160, 23);
            Filters_textBox_Include.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 73);
            label3.Name = "label3";
            label3.Size = new Size(65, 15);
            label3.TabIndex = 2;
            label3.Text = "Extensions:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 46);
            label2.Name = "label2";
            label2.Size = new Size(50, 15);
            label2.TabIndex = 1;
            label2.Text = "Exclude:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 19);
            label1.Name = "label1";
            label1.Size = new Size(49, 15);
            label1.TabIndex = 0;
            label1.Text = "Include:";
            // 
            // TopPanel_groupBoxRight
            // 
            TopPanel_groupBoxRight.Controls.Add(label9);
            TopPanel_groupBoxRight.Controls.Add(label5);
            TopPanel_groupBoxRight.Controls.Add(Settings_comboBox_DarkLight);
            TopPanel_groupBoxRight.Controls.Add(Settings_button_Help);
            TopPanel_groupBoxRight.Controls.Add(Settings_button_About);
            TopPanel_groupBoxRight.Controls.Add(Settings_checkBox_Subfolders);
            TopPanel_groupBoxRight.Controls.Add(label4);
            TopPanel_groupBoxRight.Controls.Add(Settings_trackBar_LastAccessWeight);
            TopPanel_groupBoxRight.Controls.Add(checkBox_InstantOpen);
            TopPanel_groupBoxRight.Controls.Add(checkBox_InstantFile);
            TopPanel_groupBoxRight.Dock = DockStyle.Fill;
            TopPanel_groupBoxRight.Location = new Point(437, 3);
            TopPanel_groupBoxRight.Name = "TopPanel_groupBoxRight";
            TopPanel_groupBoxRight.Size = new Size(206, 239);
            TopPanel_groupBoxRight.TabIndex = 2;
            TopPanel_groupBoxRight.TabStop = false;
            TopPanel_groupBoxRight.Text = "Settings";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(6, 127);
            label9.Name = "label9";
            label9.Size = new Size(63, 15);
            label9.TabIndex = 12;
            label9.Text = "Dark/Light";
            // 
            // label5
            // 
            label5.Location = new Point(135, 116);
            label5.Name = "label5";
            label5.Size = new Size(69, 32);
            label5.TabIndex = 11;
            label5.Text = "<-(Requires Restart)";
            // 
            // Settings_comboBox_DarkLight
            // 
            Settings_comboBox_DarkLight.DropDownStyle = ComboBoxStyle.DropDownList;
            Settings_comboBox_DarkLight.FormattingEnabled = true;
            Settings_comboBox_DarkLight.Items.AddRange(new object[] { "System", "Light", "Dark" });
            Settings_comboBox_DarkLight.Location = new Point(70, 122);
            Settings_comboBox_DarkLight.Name = "Settings_comboBox_DarkLight";
            Settings_comboBox_DarkLight.Size = new Size(62, 23);
            Settings_comboBox_DarkLight.TabIndex = 10;
            // 
            // Settings_button_Help
            // 
            Settings_button_Help.Location = new Point(23, 210);
            Settings_button_Help.Name = "Settings_button_Help";
            Settings_button_Help.Size = new Size(82, 23);
            Settings_button_Help.TabIndex = 9;
            Settings_button_Help.Text = "How to Use";
            Settings_button_Help.UseVisualStyleBackColor = true;
            // 
            // Settings_button_About
            // 
            Settings_button_About.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Settings_button_About.Location = new Point(124, 210);
            Settings_button_About.Name = "Settings_button_About";
            Settings_button_About.Size = new Size(48, 23);
            Settings_button_About.TabIndex = 8;
            Settings_button_About.Text = "About";
            Settings_button_About.UseVisualStyleBackColor = true;
            Settings_button_About.Click += Settings_button_About_Click;
            // 
            // Settings_checkBox_Subfolders
            // 
            Settings_checkBox_Subfolders.Location = new Point(6, 86);
            Settings_checkBox_Subfolders.Name = "Settings_checkBox_Subfolders";
            Settings_checkBox_Subfolders.Size = new Size(166, 34);
            Settings_checkBox_Subfolders.TabIndex = 5;
            Settings_checkBox_Subfolders.Text = "Chosen files include those within subfolders";
            Settings_checkBox_Subfolders.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.Location = new Point(3, 148);
            label4.Name = "label4";
            label4.Size = new Size(201, 32);
            label4.TabIndex = 4;
            label4.Text = "LastAccess weight (left is older more likely, right is newer more likely)";
            // 
            // Settings_trackBar_LastAccessWeight
            // 
            Settings_trackBar_LastAccessWeight.LargeChange = 2;
            Settings_trackBar_LastAccessWeight.Location = new Point(2, 183);
            Settings_trackBar_LastAccessWeight.Name = "Settings_trackBar_LastAccessWeight";
            Settings_trackBar_LastAccessWeight.Size = new Size(195, 45);
            Settings_trackBar_LastAccessWeight.TabIndex = 3;
            Settings_trackBar_LastAccessWeight.Value = 5;
            // 
            // checkBox_InstantOpen
            // 
            checkBox_InstantOpen.AutoSize = true;
            checkBox_InstantOpen.Location = new Point(6, 60);
            checkBox_InstantOpen.Name = "checkBox_InstantOpen";
            checkBox_InstantOpen.Size = new Size(166, 19);
            checkBox_InstantOpen.TabIndex = 2;
            checkBox_InstantOpen.Text = "Instantly open chosen files";
            checkBox_InstantOpen.UseVisualStyleBackColor = true;
            // 
            // checkBox_InstantFile
            // 
            checkBox_InstantFile.Location = new Point(6, 13);
            checkBox_InstantFile.Name = "checkBox_InstantFile";
            checkBox_InstantFile.Size = new Size(166, 42);
            checkBox_InstantFile.TabIndex = 1;
            checkBox_InstantFile.Text = "Choose a random file after selecting a directory";
            checkBox_InstantFile.UseVisualStyleBackColor = true;
            // 
            // FileDetails_button_ClearLog
            // 
            FileDetails_button_ClearLog.Location = new Point(373, -2);
            FileDetails_button_ClearLog.Name = "FileDetails_button_ClearLog";
            FileDetails_button_ClearLog.Size = new Size(66, 23);
            FileDetails_button_ClearLog.TabIndex = 7;
            FileDetails_button_ClearLog.Text = "Clear Log";
            FileDetails_button_ClearLog.UseVisualStyleBackColor = true;
            FileDetails_button_ClearLog.Click += FileDetails_button_ClearLog_Click;
            // 
            // splitContainerMiddle
            // 
            splitContainerMiddle.BorderStyle = BorderStyle.Fixed3D;
            splitContainerMiddle.Dock = DockStyle.Fill;
            splitContainerMiddle.Location = new Point(0, 0);
            splitContainerMiddle.Name = "splitContainerMiddle";
            // 
            // splitContainerMiddle.Panel1
            // 
            splitContainerMiddle.Panel1.Controls.Add(Folders_treeView);
            splitContainerMiddle.Panel1.Controls.Add(Folders_toolStrip);
            // 
            // splitContainerMiddle.Panel2
            // 
            splitContainerMiddle.Panel2.Controls.Add(Files_listView);
            splitContainerMiddle.Panel2.Controls.Add(Files_toolStrip);
            splitContainerMiddle.Size = new Size(646, 190);
            splitContainerMiddle.SplitterDistance = 308;
            splitContainerMiddle.TabIndex = 3;
            // 
            // Folders_treeView
            // 
            Folders_treeView.Dock = DockStyle.Fill;
            Folders_treeView.FullRowSelect = true;
            Folders_treeView.HideSelection = false;
            Folders_treeView.Location = new Point(0, 25);
            Folders_treeView.Name = "Folders_treeView";
            Folders_treeView.ShowNodeToolTips = true;
            Folders_treeView.Size = new Size(304, 161);
            Folders_treeView.TabIndex = 0;
            // 
            // Folders_toolStrip
            // 
            Folders_toolStrip.BackColor = SystemColors.Control;
            Folders_toolStrip.CanOverflow = false;
            Folders_toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            Folders_toolStrip.Items.AddRange(new ToolStripItem[] { Folders_toolStripButton_Load, toolStripSeparator1, toolStripLabel1, Folders_toolStripButton_Prev, Folders_toolStripButton_Random, Folders_toolStripButton_Next });
            Folders_toolStrip.Location = new Point(0, 0);
            Folders_toolStrip.Name = "Folders_toolStrip";
            Folders_toolStrip.Size = new Size(304, 25);
            Folders_toolStrip.TabIndex = 1;
            Folders_toolStrip.Text = "toolStrip1";
            // 
            // Folders_toolStripButton_Load
            // 
            Folders_toolStripButton_Load.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Folders_toolStripButton_Load.Image = (Image)resources.GetObject("Folders_toolStripButton_Load.Image");
            Folders_toolStripButton_Load.ImageTransparentColor = Color.Magenta;
            Folders_toolStripButton_Load.Name = "Folders_toolStripButton_Load";
            Folders_toolStripButton_Load.Size = new Size(78, 22);
            Folders_toolStripButton_Load.Text = "Load Folders";
            Folders_toolStripButton_Load.Click += Folders_toolStripButton_Load_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(43, 22);
            toolStripLabel1.Text = "Folder:";
            // 
            // Folders_toolStripButton_Prev
            // 
            Folders_toolStripButton_Prev.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Folders_toolStripButton_Prev.Image = (Image)resources.GetObject("Folders_toolStripButton_Prev.Image");
            Folders_toolStripButton_Prev.ImageTransparentColor = Color.Magenta;
            Folders_toolStripButton_Prev.Name = "Folders_toolStripButton_Prev";
            Folders_toolStripButton_Prev.Size = new Size(56, 22);
            Folders_toolStripButton_Prev.Text = "Previous";
            Folders_toolStripButton_Prev.Click += Folders_toolStripButton_Prev_Click;
            // 
            // Folders_toolStripButton_Random
            // 
            Folders_toolStripButton_Random.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Folders_toolStripButton_Random.Image = (Image)resources.GetObject("Folders_toolStripButton_Random.Image");
            Folders_toolStripButton_Random.ImageTransparentColor = Color.Magenta;
            Folders_toolStripButton_Random.Name = "Folders_toolStripButton_Random";
            Folders_toolStripButton_Random.Size = new Size(56, 22);
            Folders_toolStripButton_Random.Text = "Random";
            Folders_toolStripButton_Random.Click += Folders_toolStripButton_Random_Click;
            // 
            // Folders_toolStripButton_Next
            // 
            Folders_toolStripButton_Next.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Folders_toolStripButton_Next.Image = (Image)resources.GetObject("Folders_toolStripButton_Next.Image");
            Folders_toolStripButton_Next.ImageTransparentColor = Color.Magenta;
            Folders_toolStripButton_Next.Name = "Folders_toolStripButton_Next";
            Folders_toolStripButton_Next.Size = new Size(35, 22);
            Folders_toolStripButton_Next.Text = "Next";
            Folders_toolStripButton_Next.Click += Folders_toolStripButton_Next_Click;
            // 
            // Files_listView
            // 
            Files_listView.Dock = DockStyle.Fill;
            Files_listView.Location = new Point(0, 25);
            Files_listView.MultiSelect = false;
            Files_listView.Name = "Files_listView";
            Files_listView.ShowItemToolTips = true;
            Files_listView.Size = new Size(330, 161);
            Files_listView.TabIndex = 3;
            Files_listView.UseCompatibleStateImageBehavior = false;
            Files_listView.View = View.List;
            Files_listView.VirtualMode = true;
            Files_listView.ItemActivate += Files_listView_Activate;
            // 
            // Files_toolStrip
            // 
            Files_toolStrip.BackColor = SystemColors.Control;
            Files_toolStrip.CanOverflow = false;
            Files_toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            Files_toolStrip.Items.AddRange(new ToolStripItem[] { Files_toolStripButton_Load, toolStripSeparator2, toolStripLabel2, Files_toolStripButton_Prev, Files_toolStripButton_Random, Files_toolStripButton_Next });
            Files_toolStrip.Location = new Point(0, 0);
            Files_toolStrip.Name = "Files_toolStrip";
            Files_toolStrip.Size = new Size(330, 25);
            Files_toolStrip.TabIndex = 2;
            Files_toolStrip.Text = "toolStrip2";
            // 
            // Files_toolStripButton_Load
            // 
            Files_toolStripButton_Load.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Files_toolStripButton_Load.Image = (Image)resources.GetObject("Files_toolStripButton_Load.Image");
            Files_toolStripButton_Load.ImageTransparentColor = Color.Magenta;
            Files_toolStripButton_Load.Name = "Files_toolStripButton_Load";
            Files_toolStripButton_Load.Size = new Size(73, 22);
            Files_toolStripButton_Load.Text = "Reload Files";
            Files_toolStripButton_Load.Click += Files_toolStripButton_Reload_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new Size(28, 22);
            toolStripLabel2.Text = "File:";
            // 
            // Files_toolStripButton_Prev
            // 
            Files_toolStripButton_Prev.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Files_toolStripButton_Prev.Image = (Image)resources.GetObject("Files_toolStripButton_Prev.Image");
            Files_toolStripButton_Prev.ImageTransparentColor = Color.Magenta;
            Files_toolStripButton_Prev.Name = "Files_toolStripButton_Prev";
            Files_toolStripButton_Prev.Size = new Size(56, 22);
            Files_toolStripButton_Prev.Text = "Previous";
            Files_toolStripButton_Prev.Click += Files_toolStripButton_Prev_Click;
            // 
            // Files_toolStripButton_Random
            // 
            Files_toolStripButton_Random.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Files_toolStripButton_Random.Image = (Image)resources.GetObject("Files_toolStripButton_Random.Image");
            Files_toolStripButton_Random.ImageTransparentColor = Color.Magenta;
            Files_toolStripButton_Random.Name = "Files_toolStripButton_Random";
            Files_toolStripButton_Random.Size = new Size(56, 22);
            Files_toolStripButton_Random.Text = "Random";
            Files_toolStripButton_Random.Click += Files_toolStripButton_Random_Click;
            // 
            // Files_toolStripButton_Next
            // 
            Files_toolStripButton_Next.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Files_toolStripButton_Next.Image = (Image)resources.GetObject("Files_toolStripButton_Next.Image");
            Files_toolStripButton_Next.ImageTransparentColor = Color.Magenta;
            Files_toolStripButton_Next.Name = "Files_toolStripButton_Next";
            Files_toolStripButton_Next.Size = new Size(35, 22);
            Files_toolStripButton_Next.Text = "Next";
            Files_toolStripButton_Next.Click += Files_toolStripButton_Next_Click;
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel1;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            splitContainerMain.Orientation = Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(TableLayoutPanel_Top);
            splitContainerMain.Panel1MinSize = 245;
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(splitContainerLower);
            splitContainerMain.Size = new Size(646, 577);
            splitContainerMain.SplitterDistance = 245;
            splitContainerMain.TabIndex = 4;
            splitContainerMain.TabStop = false;
            // 
            // splitContainerLower
            // 
            splitContainerLower.Dock = DockStyle.Fill;
            splitContainerLower.Location = new Point(0, 0);
            splitContainerLower.Name = "splitContainerLower";
            splitContainerLower.Orientation = Orientation.Horizontal;
            // 
            // splitContainerLower.Panel1
            // 
            splitContainerLower.Panel1.Controls.Add(splitContainerMiddle);
            // 
            // splitContainerLower.Panel2
            // 
            splitContainerLower.Panel2.Controls.Add(FileDetails_listView);
            splitContainerLower.Panel2.Controls.Add(panel1);
            splitContainerLower.Size = new Size(646, 328);
            splitContainerLower.SplitterDistance = 190;
            splitContainerLower.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(FileDetails_checkBox_ExcludeDup);
            panel1.Controls.Add(FileDetails_button_ClearLog);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(646, 24);
            panel1.TabIndex = 2;
            // 
            // FileDetails_checkBox_ExcludeDup
            // 
            FileDetails_checkBox_ExcludeDup.AutoSize = true;
            FileDetails_checkBox_ExcludeDup.Checked = true;
            FileDetails_checkBox_ExcludeDup.CheckState = CheckState.Checked;
            FileDetails_checkBox_ExcludeDup.Location = new Point(6, 2);
            FileDetails_checkBox_ExcludeDup.Name = "FileDetails_checkBox_ExcludeDup";
            FileDetails_checkBox_ExcludeDup.Size = new Size(364, 19);
            FileDetails_checkBox_ExcludeDup.TabIndex = 9;
            FileDetails_checkBox_ExcludeDup.Text = "Files listed in the Log below are excluded from random selection";
            FileDetails_checkBox_ExcludeDup.UseVisualStyleBackColor = true;
            // 
            // RootFolders_folderBrowserDialog_Edit
            // 
            RootFolders_folderBrowserDialog_Edit.AutoUpgradeEnabled = false;
            // 
            // RootFolders_folderBrowserDialog_Add
            // 
            RootFolders_folderBrowserDialog_Add.AutoUpgradeEnabled = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(646, 599);
            Controls.Add(splitContainerMain);
            Controls.Add(Bottom_StatusStrip);
            MinimumSize = new Size(662, 400);
            Name = "Form1";
            Text = "Random File Selection v1.0.1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Shown += Form1_Shown;
            Bottom_StatusStrip.ResumeLayout(false);
            Bottom_StatusStrip.PerformLayout();
            TableLayoutPanel_Top.ResumeLayout(false);
            TopPanel_groupBoxLeft.ResumeLayout(false);
            panel2.ResumeLayout(false);
            TopPanel_groupBoxCenter.ResumeLayout(false);
            TopPanel_groupBoxCenter.PerformLayout();
            TopPanel_groupBoxRight.ResumeLayout(false);
            TopPanel_groupBoxRight.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)Settings_trackBar_LastAccessWeight).EndInit();
            splitContainerMiddle.Panel1.ResumeLayout(false);
            splitContainerMiddle.Panel1.PerformLayout();
            splitContainerMiddle.Panel2.ResumeLayout(false);
            splitContainerMiddle.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMiddle).EndInit();
            splitContainerMiddle.ResumeLayout(false);
            Folders_toolStrip.ResumeLayout(false);
            Folders_toolStrip.PerformLayout();
            Files_toolStrip.ResumeLayout(false);
            Files_toolStrip.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            splitContainerLower.Panel1.ResumeLayout(false);
            splitContainerLower.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerLower).EndInit();
            splitContainerLower.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip Bottom_StatusStrip;
        private ToolStripStatusLabel BottomStatusStrip_StatusLabel;
        private ListView FileDetails_listView;
        private ColumnHeader FileDetails_Header_Directory;
        private ColumnHeader FileDetails_Header_Filename;
        private TableLayoutPanel TableLayoutPanel_Top;
        private GroupBox TopPanel_groupBoxLeft;
        private GroupBox TopPanel_groupBoxCenter;
        private GroupBox TopPanel_groupBoxRight;
        private Button RootFolders_button_Add;
        private Button RootFolders_button_Remove;
        private Button RootFolders_button_Edit;
        private CheckedListBox RootFolders_checkedListBox;
        private TextBox Filters_textBox_Exclude;
        private TextBox Filters_textBox_Include;
        private Label label3;
        private Label label2;
        private Label label1;
        private CheckBox checkBox_InstantOpen;
        private CheckBox checkBox_InstantFile;
        private SplitContainer splitContainerMiddle;
        private ColumnHeader FileDetails_Header_LastAccess;
        private TreeView Folders_treeView;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLower;
        private Panel panel2;
        private ComboBox Filters_comboBox_Extensions;
        private Label label6;
        private Button Filters_button_ExtRem;
        private Button Filters_button_ExtAdd;
        private DateTimePicker Filters_dateTimePicker_Max;
        private DateTimePicker Filters_dateTimePicker_Min;
        private Label label8;
        private Label label7;
        private ClickThroughToolStrip Folders_toolStrip;
        private ListView Files_listView;
        private ClickThroughToolStrip Files_toolStrip;
        private ToolStripButton Folders_toolStripButton_Load;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripLabel toolStripLabel1;
        private ToolStripButton Folders_toolStripButton_Prev;
        private ToolStripButton Folders_toolStripButton_Random;
        private ToolStripButton Folders_toolStripButton_Next;
        private ToolStripButton Files_toolStripButton_Load;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripLabel toolStripLabel2;
        private ToolStripButton Files_toolStripButton_Prev;
        private ToolStripButton Files_toolStripButton_Random;
        private ToolStripButton Files_toolStripButton_Next;
        private Label label4;
        private TrackBar Settings_trackBar_LastAccessWeight;
        private CheckBox Settings_checkBox_Subfolders;
        private FolderBrowserDialog RootFolders_folderBrowserDialog_Edit;
        private FolderBrowserDialog RootFolders_folderBrowserDialog_Add;
        private Button Settings_button_About;
        private Button FileDetails_button_ClearLog;
        private Panel panel1;
        private CheckBox FileDetails_checkBox_ExcludeDup;
        private Button Settings_button_Help;
        private ComboBox Settings_comboBox_DarkLight;
        private Label label9;
        private Label label5;
    }
}
