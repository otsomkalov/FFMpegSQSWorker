# FFMpegSQSWorker

Background FFMpeg worker using SQS

## Getting Started
### Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download) or higher

### Installing

**Project:**
1. Clone project
2. Update **appsettings.json**
3. Set **AWS_ACCESS_KEY_ID** and **AWS_SECRET_ACCESS_KEY** environment variables
4. `dotnet run`

Alternatively you can use `infinitu1327/ffmpeg-sqs-worker` docker image

## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [aws-sdk-net](https://github.com/aws/aws-sdk-net) - The official AWS SDK for .NET