namespace Random_File_Select.Tests;

public class RootFolderCollectionTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly RootFolderCollection _collection = new();

    public RootFolderCollectionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RandomFileSelectTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public void TryAdd_validPath_addsUnchecked()
    {
        bool ok = _collection.TryAdd(_tempDirectory, out RootFolderEntry? added, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(added);
        Assert.False(added.IsChecked);
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(_tempDirectory)), added.Path);
        Assert.Single(_collection.Items);
    }

    [Fact]
    public void TryAdd_duplicateCaseInsensitive_fails()
    {
        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));

        string alternateCase = AlternateCase(_tempDirectory);
        bool ok = _collection.TryAdd(alternateCase, out _, out string? error);

        Assert.False(ok);
        Assert.Equal("Folder already in list.", error);
        Assert.Single(_collection.Items);
    }

    [Fact]
    public void TryAdd_trailingSlash_normalizesAsDuplicate()
    {
        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));

        string withTrailing = _tempDirectory.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        bool ok = _collection.TryAdd(withTrailing, out _, out string? error);

        Assert.False(ok);
        Assert.Equal("Folder already in list.", error);
    }

    [Fact]
    public void TryAdd_driveRoot_staysDriveRoot()
    {
        string driveRoot = Path.GetPathRoot(Environment.SystemDirectory)!;
        if (!Directory.Exists(driveRoot))
            return;

        bool ok = _collection.TryAdd(driveRoot, out RootFolderEntry? added, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(driveRoot)), added!.Path);
    }

    [Fact]
    public void TryAdd_nonexistent_fails()
    {
        string missing = Path.Combine(_tempDirectory, "does-not-exist");
        bool ok = _collection.TryAdd(missing, out _, out string? error);

        Assert.False(ok);
        Assert.Equal("That folder does not exist.", error);
        Assert.Empty(_collection.Items);
    }

    [Fact]
    public void TryReplace_toDuplicate_fails()
    {
        string secondDirectory = Path.Combine(_tempDirectory, "second");
        Directory.CreateDirectory(secondDirectory);

        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));
        Assert.True(_collection.TryAdd(secondDirectory, out _, out _));

        bool ok = _collection.TryReplace(1, _tempDirectory, out _, out string? error);

        Assert.False(ok);
        Assert.Equal("Folder already in list.", error);
    }

    [Fact]
    public void TryReplace_preservesCheckedState()
    {
        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));
        _collection.SetChecked(0, true);

        string renamed = Path.Combine(_tempDirectory, "renamed");
        Directory.CreateDirectory(renamed);

        bool ok = _collection.TryReplace(0, renamed, out RootFolderEntry? updated, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.True(updated!.IsChecked);
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(renamed)), updated.Path);
    }

    [Fact]
    public void RemoveAt_invalidIndex_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _collection.RemoveAt(0));
    }

    [Fact]
    public void LoadSaved_preservesMissingPathAndCheckedState()
    {
        string missing = Path.Combine(_tempDirectory, "not-present");
        _collection.LoadSaved([new RootFolderEntry { Path = missing, IsChecked = true }]);

        Assert.Single(_collection.Items);
        Assert.True(_collection.Items[0].IsChecked);
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(missing)), _collection.Items[0].Path);
    }

    [Fact]
    public void LoadSaved_ignoresBlankAndDuplicatePaths()
    {
        _collection.LoadSaved(
        [
            new RootFolderEntry { Path = "   ", IsChecked = true },
            new RootFolderEntry { Path = _tempDirectory, IsChecked = false },
            new RootFolderEntry { Path = AlternateCase(_tempDirectory), IsChecked = true }
        ]);

        Assert.Single(_collection.Items);
        Assert.False(_collection.Items[0].IsChecked);
    }

    [Fact]
    public void Export_returnsIndependentCopies()
    {
        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));
        _collection.SetChecked(0, true);

        List<RootFolderEntry> exported = _collection.Export();
        exported[0].Path = "mutated";
        exported[0].IsChecked = false;

        Assert.NotEqual("mutated", _collection.Items[0].Path);
        Assert.True(_collection.Items[0].IsChecked);
    }

    [Fact]
    public void CheckedPaths_returnsOnlyChecked()
    {
        Assert.True(_collection.TryAdd(_tempDirectory, out _, out _));
        string secondDirectory = Path.Combine(_tempDirectory, "second");
        Directory.CreateDirectory(secondDirectory);
        Assert.True(_collection.TryAdd(secondDirectory, out _, out _));
        _collection.SetChecked(0, true);

        var checkedPaths = _collection.CheckedPaths.ToList();

        Assert.Single(checkedPaths);
        Assert.Equal(_collection.Items[0].Path, checkedPaths[0]);
    }

    private static string AlternateCase(string path)
    {
        var chars = path.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsLetter(chars[i]))
            {
                chars[i] = char.IsUpper(chars[i])
                    ? char.ToLowerInvariant(chars[i])
                    : char.ToUpperInvariant(chars[i]);
                break;
            }
        }

        return new string(chars);
    }
}