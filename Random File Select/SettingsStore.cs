using System.Text.Json;

namespace Random_File_Select;

/// <summary>Contract for loading and persisting application settings.</summary>
public interface ISettingsStore
{
    /// <summary>Loads settings from storage, returning defaults on failure.</summary>
    AppSettings Load();

    /// <summary>Persists settings to storage.</summary>
    void Save(AppSettings settings);
}

/// <summary>File-based JSON persistence store for application settings using atomic replacement.</summary>
public sealed class SettingsStore : ISettingsStore
{
    /// <summary>Default settings file path located in the application base directory.</summary>
    public static string DefaultFilePath =>
        Path.Combine(AppContext.BaseDirectory, "RandomFileSelect.settings");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    /// <summary>Initializes the store with the specified file path.</summary>
    public SettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>Reads and deserializes JSON settings from disk, returning normalized defaults if missing or invalid.</summary>
    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
                return new AppSettings();

            Normalize(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Serializes settings to a temporary JSON file and atomically replaces the destination file.</summary>
    public void Save(AppSettings settings)
    {
        string directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("Settings path has no directory.");

        Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        string tempPath = _filePath + ".tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    /// <summary>Ensures nested settings instances and collections are non-null and enum values are defined.</summary>
    internal static void Normalize(AppSettings settings)
    {
        settings.Layout ??= new WindowLayoutSettings();
        settings.Behavior ??= new BehaviorSettings();
        settings.Filters ??= new FilterSettings();
        if (!Enum.IsDefined(settings.Behavior.ColorMode))
            settings.Behavior.ColorMode = AppColorMode.System;
        settings.RootFolders ??= [];
        settings.Filters.ExtensionGroups ??= [];
    }
}
