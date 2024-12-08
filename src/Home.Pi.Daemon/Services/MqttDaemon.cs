using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Server;

namespace Home.Pi.Daemon.Services;

internal class MqttDaemon(MqttServer mqttServer, ILogger<MqttDaemon> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly MqttServer mqttServer = mqttServer;
    private readonly ILogger<MqttDaemon> logger = logger;
    private readonly IServiceProvider serviceProvider = serviceProvider;

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
    private static readonly Dictionary<(string topic, string action), Message> buttonMessageMap = new()
    {
        { ("zigbee2mqtt/office_wall_tap", "press_1"), new TurnOffAllLightsMessage()},
        { ("zigbee2mqtt/office_wall_tap", "press_2"), new ControlGroupedLightsMessage() { Group = "KitchenAndHallway" }},
        { ("zigbee2mqtt/office_wall_tap", "press_3"), new ControlGroupedLightsMessage() { Group = "Office" }},
        { ("zigbee2mqtt/office_wall_tap", "press_4"), new ControlGroupedLightsMessage() { Group = "LivingRoom" }},
        { ("zigbee2mqtt/house_tap", "single"), new ChillMessage()},
        { ("zigbee2mqtt/house_tap", "long"), new WakeUpPcMessage()},
    };

    private static readonly string[] tempSensors = ["zigbee2mqtt/kitchen_sink_temp"];

    public static Message[] CreateMessage(MqttApplicationMessage applicationMessage)
    {
        if (buttonMessageMap.Keys.Any(k => k.topic.ToLowerInvariant() == applicationMessage.Topic?.ToLowerInvariant()))
        {
            var payload = JsonSerializer.Deserialize<ButtonActionPayload>(applicationMessage.PayloadSegment, serializerOptions);

            if (payload != null
                && payload.Action != null
                && buttonMessageMap.TryGetValue((applicationMessage.Topic.ToLowerInvariant(), payload.Action.ToLowerInvariant()), out var message))
            {
                return [message];
            }
        }

        if (tempSensors.Contains(applicationMessage.Topic, StringComparer.InvariantCultureIgnoreCase))
        {
            var payload = JsonSerializer.Deserialize<TempSensorPayload>(applicationMessage.PayloadSegment, serializerOptions);
            if (payload != null)
            {
                return [new TemperatureSensorMessage() {
                    Device = applicationMessage.Topic.Replace("zigbee2mqtt/", "", StringComparison.InvariantCultureIgnoreCase),
                    Battery = payload.Battery,
                    Humidity = payload.Humidity,
                    LinkQuality = payload.LinkQuality,
                    Temperature = payload.Temperature,
                }];
            }
        }

        return [];
    }


    private record ButtonActionPayload(string Action);
    private record TempSensorPayload(int Battery, double Humidity, int LinkQuality, double Temperature);
}