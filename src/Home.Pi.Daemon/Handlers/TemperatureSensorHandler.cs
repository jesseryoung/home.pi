
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon.Handlers;

internal class TemperatureSensorHanlderOptions
{
    public required string StorageAccountName { get; init; }
    public required string TableName { get; init; }
}

internal class TemperatureSensorHandler(ILogger<TemperatureSensorHandler> logger, IOptions<TemperatureSensorHanlderOptions> options) : MessageHandler<TemperatureSensorMessage>
{

    private readonly TemperatureSensorHanlderOptions options = options.Value ?? throw new ArgumentNullException(nameof(options));
    public override async Task HandleMessage(TemperatureSensorMessage notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Got temperature reading for {}", notification.Device);


        var tableClient = new TableClient(new Uri($"https://{this.options.StorageAccountName}.table.core.windows.net/{this.options.TableName}"), this.options.TableName, new DefaultAzureCredential());

        var temperatureReading = new TableEntity(notification.Device, Guid.NewGuid().ToString())
        {
            { "timestamp", DateTime.UtcNow },
            { "battery", notification.Battery },
            { "humidity", notification.Humidity},
            { "linkquality", notification.LinkQuality },
            { "temperature", notification.Temperature }
        };


        await tableClient.AddEntityAsync(temperatureReading);

    }
}