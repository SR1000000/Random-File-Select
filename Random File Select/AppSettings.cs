namespace Random_File_Select;

/// <summary>Application configuration container</summary>
public sealed class AppSettings
{
    public WindowLayoutSettings Layout { get; set; } = new();
    public List<RootFolderEntry> RootFolders { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public FilterSettings Filters { get; set; } = new();
}

/// <summary>Layout settings</summary>
public sealed class WindowLayoutSettings
{
    public WindowPlacementSettings? WindowPlacement { get; set; }

    public int SplitMainDistance { get; set; }
    public int SplitLowerDistance { get; set; }
    public int SplitMiddleDistance { get; set; }
    public int FileDetailsDirectoryColumnWidth { get; set; }
    public int FileDetailsFilenameColumnWidth { get; set; }
    public int FileDetailsLastAccessColumnWidth { get; set; }
}

/// <summary>Serialized Win32 WINDOWPLACEMENT coordinates</summary>
public sealed class WindowPlacementSettings
{
    public int NormalLeft { get; set; }
    public int NormalTop { get; set; }
    public int NormalRight { get; set; }
    public int NormalBottom { get; set; }
    public int ShowCmd { get; set; }
}

/// <summary>User preference flags</summary>
public sealed class BehaviorSettings
{
    public bool LoadRootFoldersOnStart { get; set; }
    public bool ChooseRandomFileAfterDir { get; set; }
    public bool InstantlyOpenChosenFiles { get; set; }
    public bool IncludeSubfolders { get; set; }
    public int LastAccessWeight { get; set; } = 5;
}

/// <summary>Search and filter criteria for file matching.</summary>
public sealed class FilterSettings
{
    public string Include { get; set; } = string.Empty;
    public string Exclude { get; set; } = string.Empty;
    public List<string> ExtensionGroups { get; set; } = new();
    public int SelectedExtensionGroupIndex { get; set; }

    /// <summary>Live text of the extension ComboBox (may be custom or empty); null in older settings files.</summary>
    public string? ExtensionFilterText { get; set; }
    public DateTime? DateModifiedMin { get; set; }
    public DateTime? DateModifiedMax { get; set; }
}
