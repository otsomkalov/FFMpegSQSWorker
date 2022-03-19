using System.Diagnostics;
using Microsoft.Extensions.Options;
using Worker.Settings;

namespace Worker.Services;

public class FFMpegService
{
    private readonly FFMpegSettings _settings;
    private readonly ILogger<FFMpegService> _logger;

    public FFMpegService(IOptions<FFMpegSettings> settings, ILogger<FFMpegService> logger)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<bool> ConvertAsync(string inputFilePath, string arguments, string outputFilePath)
    {
        var argumentsParts = new List<string>
        {
            $"-i {inputFilePath}",
            arguments,
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
            return true;
        }

        _logger.LogError("Error during FFMpeg file conversion: {Error}", error);

        return false;
    }
}