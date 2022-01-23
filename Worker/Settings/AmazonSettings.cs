namespace Worker.Settings;

public record AmazonSettings
{
    public const string SectionName = "Amazon";

    public string InputQueueUrl { get; init; }

    public string OutputQueueUrl { get; init; }
}
