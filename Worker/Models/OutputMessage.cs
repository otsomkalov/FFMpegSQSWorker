namespace Worker.Models;

public record OutputMessage
{
    public string OutputFilePath { get; set; }

    public string ThumbnailFilePath { get; set; }
}