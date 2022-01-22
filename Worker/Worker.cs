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
    private readonly AmazonSettings _amazonSettings;
    private readonly IAmazonSQS _sqs;
    private readonly FFMpegService _ffMpegService;
    private readonly GlobalSettings _globalSettings;

    public Worker(ILogger<Worker> logger, IOptions<AmazonSettings> amazonOptions, IAmazonSQS sqs, FFMpegService ffMpegService,
        IOptions<GlobalSettings> globalSettings)
    {
        _logger = logger;
        _sqs = sqs;
        _ffMpegService = ffMpegService;
        _globalSettings = globalSettings.Value;
        _amazonSettings = amazonOptions.Value;
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
        var receiveMessageResponse = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _amazonSettings.InputQueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20
        }, cancellationToken);

        if (!receiveMessageResponse.Messages.Any())
        {
            await Task.Delay(TimeSpan.FromSeconds(_globalSettings.Delay), cancellationToken);
        }

        foreach (var message in receiveMessageResponse.Messages)
        {
            await ProcessMessageAsync(message);
        }
    }

    private async Task ProcessMessageAsync(Message receivedQueueMessage)
    {
        var (inputFilePath, desiredExtension) = JsonSerializer.Deserialize<InputMessage>(receivedQueueMessage.Body)!;

        var outputFilePath = await _ffMpegService.ConvertAsync(inputFilePath, desiredExtension);

        var resultQueueMessage = new OutputMessage();

        if (string.IsNullOrEmpty(outputFilePath))
        {
            await _sqs.SendMessageAsync(new()
            {
                QueueUrl = _amazonSettings.OutputQueueUrl,
                MessageBody = JsonSerializer.Serialize(resultQueueMessage)
            });

            return;
        }

        resultQueueMessage.OutputFilePath = outputFilePath;

        var thumbnailFilePath = await _ffMpegService.GetThumbnailAsync(inputFilePath);

        if (string.IsNullOrEmpty(thumbnailFilePath))
        {
            await _sqs.SendMessageAsync(new()
            {
                QueueUrl = _amazonSettings.OutputQueueUrl,
                MessageBody = JsonSerializer.Serialize(resultQueueMessage)
            });

            return;
        }

        resultQueueMessage.ThumbnailFilePath = thumbnailFilePath;

        await _sqs.SendMessageAsync(new()
        {
            QueueUrl = _amazonSettings.OutputQueueUrl,
            MessageBody = JsonSerializer.Serialize(resultQueueMessage)
        });

        await Task.Delay(TimeSpan.FromSeconds(_globalSettings.Delay));
    }
}