using System.Diagnostics;
using Microsoft.Extensions.Options;
using Worker.Settings;

namespace Worker.Services;

public class FFMpegService
{
    private readonly FFMpegSettings _settings;
    private readonly FoldersSettings _foldersSettings;
    private readonly ILogger<FFMpegService> _logger;

    public FFMpegService(IOptions<FFMpegSettings> settings, ILogger<FFMpegService> logger,
        IOptions<FoldersSettings> directoriesSettings)
    {
        _logger = logger;
        _foldersSettings = directoriesSettings.Value;
        _settings = settings.Value;
    }

    public async Task<string> ConvertAsync(string inputFilePath, string desiredExtension)
    {
        var outputFileName = $"{Guid.NewGuid()}{desiredExtension}";
        var outputFilePath = Path.Combine(_foldersSettings.OutputFolderPath, outputFileName);

        var argumentsParts = new List<string>
        {
            $"-i {inputFilePath}",
            "-filter:v scale='trunc(iw/2)*2:trunc(ih/2)*2'",
            "-c:a aac",
            "-max_muxing_queue_size 1024",
            outputFilePath
        };

        var processStartInfo = new ProcessStartInfo
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = _settings.Path,
            Arguments = string.Join(' ', argumentsParts)
        };

        var process = Process.Start(processStartInfo);

        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            return outputFileName;
        }

        _logger.LogError("Error during FFMpeg file conversion: {Error}", error);

        return null;
    }

    public async Task<string> GetThumbnailAsync(string inputFilePath)
    {
        var thumbnailFileName = $"{Guid.NewGuid()}.jpg";
        var thumbnailFilePath = Path.Combine(_foldersSettings.ThumbnailsFolderPath, thumbnailFileName);

        var processStartInfo = new ProcessStartInfo
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = _settings.Path,
            Arguments = $"-i {inputFilePath} -ss 1 -vframes 1 {thumbnailFilePath}"
        };

        var process = Process.Start(processStartInfo);

        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Error during FFMpeg thumbnail creation: {Error}", error);
        }

        return thumbnailFilePath;
    }
}