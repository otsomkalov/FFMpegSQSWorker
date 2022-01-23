using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Worker.Models;
using Worker.Services;
using Worker.Settings;

namespace Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqs, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _sqs = sqs;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during Worker execution:");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var amazonSettings = scope.ServiceProvider.GetRequiredService<IOptions<AmazonSettings>>().Value;
        var globalSettings = scope.ServiceProvider.GetRequiredService<IOptions<GlobalSettings>>().Value;
        var ffMpegService = scope.ServiceProvider.GetRequiredService<FFMpegService>();
        var foldersSettings = scope.ServiceProvider.GetRequiredService<IOptions<FoldersSettings>>().Value;

        var receiveMessageResponse = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = amazonSettings.InputQueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20
        }, cancellationToken);

        if (!receiveMessageResponse.Messages.Any())
        {
            await Task.Delay(TimeSpan.FromSeconds(globalSettings.Delay), cancellationToken);
        }

        foreach (var message in receiveMessageResponse.Messages)
        {
            await ProcessMessageAsync(message, amazonSettings, ffMpegService, foldersSettings);

            await _sqs.DeleteMessageAsync(amazonSettings.InputQueueUrl, message.ReceiptHandle, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(globalSettings.Delay), cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(Message receivedQueueMessage, AmazonSettings amazonSettings, FFMpegService ffMpegService, FoldersSettings foldersSettings)
    {
        var (inputFileName, desiredExtension) = JsonSerializer.Deserialize<InputMessage>(receivedQueueMessage.Body)!;

        var inputFilePath = Path.Combine(foldersSettings.InputFolderPath, inputFileName);

        var outputFilePath = await ffMpegService.ConvertAsync(inputFilePath, desiredExtension);

        var resultQueueMessage = new OutputMessage();

        if (string.IsNullOrEmpty(outputFilePath))
        {
            await _sqs.SendMessageAsync(new()
            {
                QueueUrl = amazonSettings.OutputQueueUrl,
                MessageBody = JsonSerializer.Serialize(resultQueueMessage)
            });

            return;
        }

        resultQueueMessage.OutputFilePath = outputFilePath;

        var thumbnailFilePath = await ffMpegService.GetThumbnailAsync(inputFilePath);

        if (string.IsNullOrEmpty(thumbnailFilePath))
        {
            await _sqs.SendMessageAsync(new()
            {
                QueueUrl = amazonSettings.OutputQueueUrl,
                MessageBody = JsonSerializer.Serialize(resultQueueMessage)
            });

            return;
        }

        resultQueueMessage.ThumbnailFilePath = thumbnailFilePath;

        await _sqs.SendMessageAsync(new()
        {
            QueueUrl = amazonSettings.OutputQueueUrl,
            MessageBody = JsonSerializer.Serialize(resultQueueMessage)
        });
    }
}