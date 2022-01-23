namespace Worker.Settings;

public record FoldersSettings
{
    public const string SectionName = "Folders";

    public string InputFolderPath { get; init; }

    public string OutputFolderPath { get; init; }

    public string ThumbnailsFolderPath { get; init; }
}