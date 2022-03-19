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
    private readonly AmazonSettings _amazonSettings;
    private readonly GlobalSettings _globalSettings;
    private readonly FFMpegService _ffMpegService;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqs, IOptions<AmazonSettings> amazonSettings,
        IOptions<GlobalSettings> globalSettings, FFMpegService ffMpegService)
    {
        _logger = logger;
        _sqs = sqs;
        _ffMpegService = ffMpegService;
        _globalSettings = globalSettings.Value;
        _amazonSettings = amazonSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job execution started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running job");

                await RunAsync(stoppingToken);

                _logger.LogInformation("Job execution has finished");

                await Task.Delay(TimeSpan.FromSeconds(_globalSettings.Delay), stoppingToken);
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

        var queueMessage = receiveMessageResponse.Messages.FirstOrDefault();

        if (queueMessage == null)
        {
            return;
        }

        var (id, inputFilePath, arguments, outputFilePath) = JsonSerializer.Deserialize<InputMessage>(queueMessage.Body)!;
        var conversionResult = await _ffMpegService.ConvertAsync(inputFilePath, arguments, outputFilePath);

        if (conversionResult)
        {
            var resultQueueMessage = new OutputMessage(id);

            await _sqs.SendMessageAsync(new()
            {
                QueueUrl = _amazonSettings.OutputQueueUrl,
                MessageBody = JsonSerializer.Serialize(resultQueueMessage)
            }, cancellationToken);
        }

        await _sqs.DeleteMessageAsync(_amazonSettings.InputQueueUrl, queueMessage.ReceiptHandle, cancellationToken);
    }
}