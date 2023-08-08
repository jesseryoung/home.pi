using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Server;

namespace Home.Pi.Daemon.Services;

internal class MqttDaemon : BackgroundService
{
    private readonly MqttServer mqttServer;
    private readonly ILogger<MqttDaemon> logger;
    private readonly IServiceProvider serviceProvider;

    public MqttDaemon(MqttServer mqttServer, ILogger<MqttDaemon> logger, IServiceProvider serviceProvider)
    {
        this.mqttServer = mqttServer;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Starting up");
        this.mqttServer.InterceptingPublishAsync += async args =>
        {
            try
            {
                var messages = MqttMessageFactory.CreateMessage(args.ApplicationMessage);
                if (messages.Any())
                {
                    using var scope = this.serviceProvider.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await Task.WhenAll(messages
                        .Select(m => mediator.Publish(m, stoppingToken))
                    );
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing message on topic {}", args.ApplicationMessage.Topic);
            }
        };

        await this.mqttServer.StartAsync();
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
        this.logger.LogInformation("Stopping");
        await this.mqttServer.StopAsync();

    }
}

internal class MqttMessageFactory
{

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly Dictionary<(string topic, string action), Message> topicMessageMap = new()
    {
        { ("zigbee2mqtt/office_tap_desk", "button_1_press"), new TurnOffAllLightsMessage()}
    };

    public static Message[] CreateMessage(MqttApplicationMessage applicationMessage)
    {
        if (topicMessageMap.Keys.Any(k => k.topic.ToLowerInvariant() == applicationMessage.Topic?.ToLowerInvariant()))
        {
            var payload = JsonSerializer.Deserialize<ButtonActionMessage>(applicationMessage.PayloadSegment, serializerOptions);

            if (payload != null
                && payload.Action != null
                && topicMessageMap.TryGetValue((applicationMessage.Topic.ToLowerInvariant(), payload.Action.ToLowerInvariant()), out var message))
            {
                return new[] { message };
            }
        }

        return Array.Empty<Message>();
    }


    private record ButtonActionMessage(string Action);
}