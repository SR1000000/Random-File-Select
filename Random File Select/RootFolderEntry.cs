namespace Random_File_Select;

public class RootFolderEntry
{
    public string Path { get; set; } = string.Empty;
    public bool IsChecked { get; set; }

    public override string ToString() => Path;
}