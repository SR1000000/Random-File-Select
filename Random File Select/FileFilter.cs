namespace Random_File_Select;

/// <summary>Immutable file-matching rule built from filter settings; a file matches only if every enabled part passes.</summary>
public sealed class FileFilter
{
    private static readonly char[] WhitespaceSeparators = [' ', '\t', '\r', '\n'];

    private readonly HashSet<string> _extensions;
    private readonly string[] _include;
    private readonly string[] _exclude;
    private readonly DateTime? _minDay;
    private readonly DateTime? _maxDay;

    /// <summary>Creates a filter from raw text and optional date bounds; null/blank text and null dates impose no restriction.</summary>
    public FileFilter(
        string? extensionText,
        string? include,
        string? exclude,
        DateTime? dateModifiedMin,
        DateTime? dateModifiedMax)
    {
        _extensions = new HashSet<string>(
            Tokenize(extensionText).Select(t => t.TrimStart('.')).Where(t => t.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        _include = Tokenize(include);
        _exclude = Tokenize(exclude);

        _minDay = dateModifiedMin?.Date;
        _maxDay = dateModifiedMax?.Date;
        if (_minDay.HasValue && _maxDay.HasValue && _minDay.Value > _maxDay.Value)
            _minDay = _maxDay;
    }

    /// <summary>Builds a filter from persisted/captured filter settings.</summary>
    public static FileFilter FromSettings(FilterSettings settings) =>
        new(settings.ExtensionFilterText,
            settings.Include,
            settings.Exclude,
            settings.DateModifiedMin,
            settings.DateModifiedMax);

    /// <summary>True when at least one extension is listed; false means all file types pass the extension test.</summary>
    public bool HasExtensionRestriction => _extensions.Count > 0;

    /// <summary>Evaluates a file using its name and last-write time from the directory listing.</summary>
    public bool Matches(FileInfo file) => Matches(file.Name, file.LastWriteTime);

    /// <summary>Evaluates extension, include/exclude name substrings, and inclusive calendar-day modified range.</summary>
    public bool Matches(string fileName, DateTime lastWriteTime)
    {
        if (_extensions.Count > 0)
        {
            string extension = Path.GetExtension(fileName).TrimStart('.');
            if (!_extensions.Contains(extension))
                return false;
        }

        if (_include.Length > 0 && !ContainsAny(fileName, _include))
            return false;

        if (_exclude.Length > 0 && ContainsAny(fileName, _exclude))
            return false;

        if (_minDay.HasValue && lastWriteTime < _minDay.Value)
            return false;

        if (_maxDay.HasValue && lastWriteTime >= _maxDay.Value.AddDays(1))
            return false;

        return true;
    }

    /// <summary>Case-insensitively checks whether the name contains any of the terms.</summary>
    private static bool ContainsAny(string name, string[] terms)
    {
        foreach (string term in terms)
        {
            if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Splits text on whitespace, dropping blank terms.</summary>
    private static string[] Tokenize(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(WhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
}
