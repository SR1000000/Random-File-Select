using System.Text.Json;
using System.Text.Json.Serialization;

namespace Random_File_Select;

/// <summary>Application color theme preference.</summary>
[JsonConverter(typeof(AppColorModeJsonConverter))]
public enum AppColorMode
{
    System,
    Light,
    Dark
}

/// <summary>Reads and writes <see cref="AppColorMode"/> as a string, falling back to System on invalid values instead of failing the whole settings file.</summary>
public sealed class AppColorModeJsonConverter : JsonConverter<AppColorMode>
{
    /// <summary>Parses a defined enum name or number, or skips the value and returns System when it is invalid.</summary>
    public override AppColorMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        AppColorMode mode = AppColorMode.System;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                Enum.TryParse(reader.GetString(), ignoreCase: true, out mode);
                break;
            case JsonTokenType.Number when reader.TryGetInt32(out int number):
                mode = (AppColorMode)number;
                break;
        }

        reader.Skip();
        return Enum.IsDefined(mode) ? mode : AppColorMode.System;
    }

    /// <summary>Writes the enum name as a JSON string.</summary>
    public override void Write(Utf8JsonWriter writer, AppColorMode value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>Conversion helpers for <see cref="AppColorMode"/>.</summary>
public static class AppColorModeExtensions
{
    /// <summary>Maps the app preference to the WinForms <see cref="SystemColorMode"/>.</summary>
    public static SystemColorMode ToSystemColorMode(this AppColorMode mode) => mode switch
    {
        AppColorMode.Light => SystemColorMode.Classic,
        AppColorMode.Dark => SystemColorMode.Dark,
        _ => SystemColorMode.System
    };
}

/// <summary>Application configuration container</summary>
public sealed class AppSettings
{
    public WindowLayoutSettings Layout { get; set; } = new();
    public List<RootFolderEntry> RootFolders { get; set; } = [];
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
    public bool ChooseRandomFileAfterDir { get; set; }
    public bool InstantlyOpenChosenFiles { get; set; }
    public bool IncludeSubfolders { get; set; }
    public AppColorMode ColorMode { get; set; } = AppColorMode.System;
    public bool ExcludeDup { get; set; } = true;
    public int LastAccessWeight { get; set; } = 5;
}

/// <summary>Search and filter criteria for file matching.</summary>
public sealed class FilterSettings
{
    public string Include { get; set; } = string.Empty;
    public string Exclude { get; set; } = string.Empty;
    public List<string> ExtensionGroups { get; set; } = [];
    public int SelectedExtensionGroupIndex { get; set; }

    /// <summary>Live text of the extension ComboBox (may be custom or empty); null in older settings files.</summary>
    public string? ExtensionFilterText { get; set; }
    public DateTime? DateModifiedMin { get; set; }
    public DateTime? DateModifiedMax { get; set; }
}
