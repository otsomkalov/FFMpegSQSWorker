namespace Worker.Settings;

public record AmazonSettings(string InputQueueUrl, string OutputQueueUrl)
{
    public const string SectionName = "Amazon";
}
