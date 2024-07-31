using Home.Pi.Daemon.Controllers;
using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon.Handlers;

internal class TurnOffAllLightsOptions
{
    public required string AllLightsGroupId { get; init; }
    public required string[] LightServers { get; init; }
}

internal class TurnOffAllLightsHandler(ILogger<TurnOffAllLightsHandler> logger, IHueLightController hueLightController, ILightServerController lightServerController, IOptions<TurnOffAllLightsOptions> options) : MessageHandler<TurnOffAllLightsMessage>
{
    private readonly ILogger<TurnOffAllLightsHandler> logger = logger;
    private readonly IHueLightController hueLightController = hueLightController;
    private readonly ILightServerController lightServerController = lightServerController;
    private readonly TurnOffAllLightsOptions options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public override async Task HandleMessage(TurnOffAllLightsMessage notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Turning off all the lights...");

        var tasks = new List<Task>()
        {
            this.hueLightController.SetGroup(this.options.AllLightsGroupId, false, 0)
        };
        tasks.AddRange(this.options.LightServers.Select(this.lightServerController.TurnOff));

        await Task.WhenAll(tasks);
    }
}