namespace Random_File_Select.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsPath;

    public SettingsStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RandomFileSelectSettings_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _settingsPath = Path.Combine(_tempDirectory, "RandomFileSelect.settings");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public void Load_missingFile_returnsDefaults()
    {
        var store = new SettingsStore(_settingsPath);
        AppSettings settings = store.Load();

        Assert.NotNull(settings.Layout);
        Assert.NotNull(settings.Behavior);
        Assert.NotNull(settings.Filters);
        Assert.Empty(settings.RootFolders);
        Assert.Null(settings.Layout.WindowPlacement);
    }

    [Fact]
    public void Load_corruptJson_returnsDefaults()
    {
        File.WriteAllText(_settingsPath, "{ not valid json");
        var store = new SettingsStore(_settingsPath);

        AppSettings settings = store.Load();

        Assert.Empty(settings.RootFolders);
    }

    [Fact]
    public void Load_legacyXml_returnsDefaults()
    {
        File.WriteAllText(_settingsPath, "<?xml version='1.0'?><SettingsFile />");
        var store = new SettingsStore(_settingsPath);

        AppSettings settings = store.Load();

        Assert.Empty(settings.RootFolders);
    }

    [Fact]
    public void Load_nullNestedSections_normalizesDefaults()
    {
        File.WriteAllText(_settingsPath, """
            {
              "Layout": null,
              "RootFolders": null,
              "Behavior": null,
              "Filters": { "ExtensionGroups": null }
            }
            """);
        var store = new SettingsStore(_settingsPath);

        AppSettings settings = store.Load();

        Assert.NotNull(settings.Layout);
        Assert.NotNull(settings.Behavior);
        Assert.NotNull(settings.Filters);
        Assert.NotNull(settings.Filters.ExtensionGroups);
        Assert.Empty(settings.RootFolders);
    }

    [Fact]
    public void SaveLoad_roundTrip_preservesAllSections()
    {
        var store = new SettingsStore(_settingsPath);
        var original = new AppSettings
        {
            Layout = new WindowLayoutSettings
            {
                WindowPlacement = new WindowPlacementSettings
                {
                    NormalLeft = 10,
                    NormalTop = 20,
                    NormalRight = 810,
                    NormalBottom = 620,
                    ShowCmd = 3
                },
                SplitMainDistance = 223,
                SplitLowerDistance = 160,
                SplitMiddleDistance = 307,
                FileDetailsDirectoryColumnWidth = 301,
                FileDetailsFilenameColumnWidth = 201,
                FileDetailsLastAccessColumnWidth = 101
            },
            RootFolders =
            [
                new RootFolderEntry { Path = @"C:\foo", IsChecked = true },
                new RootFolderEntry { Path = @"D:\bar", IsChecked = false }
            ],
            Behavior = new BehaviorSettings
            {
                LoadRootFoldersOnStart = true,
                ChooseRandomFileAfterDir = false,
                InstantlyOpenChosenFiles = true,
                IncludeSubfolders = true,
                DarkMode = true,
                LastAccessWeight = 7
            },
            Filters = new FilterSettings
            {
                Include = "inc",
                Exclude = "exc",
                ExtensionGroups = ["mkv mp4", "zip 7z"],
                SelectedExtensionGroupIndex = 1,
                DateModifiedMin = new DateTime(2020, 1, 1),
                DateModifiedMax = new DateTime(2025, 6, 15)
            }
        };

        store.Save(original);
        AppSettings loaded = store.Load();

        Assert.NotNull(loaded.Layout.WindowPlacement);
        Assert.Equal(10, loaded.Layout.WindowPlacement.NormalLeft);
        Assert.Equal(3, loaded.Layout.WindowPlacement.ShowCmd);
        Assert.Equal(223, loaded.Layout.SplitMainDistance);
        Assert.Equal(2, loaded.RootFolders.Count);
        Assert.True(loaded.RootFolders[0].IsChecked);
        Assert.False(loaded.RootFolders[1].IsChecked);
        Assert.True(loaded.Behavior.LoadRootFoldersOnStart);
        Assert.True(loaded.Behavior.DarkMode);
        Assert.Equal(7, loaded.Behavior.LastAccessWeight);
        Assert.Equal("inc", loaded.Filters.Include);
        Assert.Equal(1, loaded.Filters.SelectedExtensionGroupIndex);
        Assert.Equal(new DateTime(2020, 1, 1), loaded.Filters.DateModifiedMin);
    }

    [Theory]
    [InlineData("custom ext")]
    [InlineData("")]
    public void SaveLoad_extensionFilterText_roundTrips(string text)
    {
        var store = new SettingsStore(_settingsPath);
        store.Save(new AppSettings { Filters = new FilterSettings { ExtensionFilterText = text } });

        Assert.Equal(text, store.Load().Filters.ExtensionFilterText);
    }

    [Fact]
    public void Load_olderJsonWithoutExtensionFilterText_yieldsNull()
    {
        File.WriteAllText(_settingsPath, """{ "Filters": { "Include": "x" } }""");

        AppSettings settings = new SettingsStore(_settingsPath).Load();

        Assert.Null(settings.Filters.ExtensionFilterText);
        Assert.Equal("x", settings.Filters.Include);
    }

    [Fact]
    public void SaveLoad_nullDateBounds_roundTrip()
    {
        var store = new SettingsStore(_settingsPath);
        store.Save(new AppSettings
        {
            Filters = new FilterSettings { DateModifiedMin = null, DateModifiedMax = new DateTime(2025, 1, 2) }
        });

        FilterSettings loaded = store.Load().Filters;

        Assert.Null(loaded.DateModifiedMin);
        Assert.Equal(new DateTime(2025, 1, 2), loaded.DateModifiedMax);
    }

    [Fact]
    public void Save_overwrite_doesNotLeaveTmpFile()
    {
        var store = new SettingsStore(_settingsPath);
        store.Save(new AppSettings());
        store.Save(new AppSettings { Behavior = new BehaviorSettings { LastAccessWeight = 3 } });

        Assert.False(File.Exists(_settingsPath + ".tmp"));
        Assert.Equal(3, store.Load().Behavior.LastAccessWeight);
    }
}