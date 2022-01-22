namespace Worker.Settings;

public record FFMpegSettings(string Path)
{
    public const string SectionName = "FFMpeg";
}