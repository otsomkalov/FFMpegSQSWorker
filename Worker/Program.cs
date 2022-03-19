using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Worker.Services;
using Worker.Settings;

void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    var configuration = context.Configuration;

    services.Configure<AmazonSettings>(configuration.GetSection(AmazonSettings.SectionName))
        .Configure<FFMpegSettings>(configuration.GetSection(FFMpegSettings.SectionName))
        .Configure<GlobalSettings>(configuration);

    services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(new EnvironmentVariablesAWSCredentials(), RegionEndpoint.EUCentral1))
        .AddSingleton<FFMpegService>();

    services.AddHostedService<Worker.Worker>();

    services.AddApplicationInsightsTelemetryWorkerService();
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(ConfigureServices)
    .Build();

await host.RunAsync();
