using Home.Pi.Daemon.Controllers;

namespace Home.Pi.Daemon.Handlers;



internal class TurnOffAllLightsHandler : MessageHandler<TurnOffAllLightsMessage>
{
    private readonly ILogger<TurnOffAllLightsHandler> logger;
    private readonly IHueLightController hueLightController;

    public TurnOffAllLightsHandler(ILogger<TurnOffAllLightsHandler> logger, IHueLightController hueLightController)
    {
        this.logger = logger;
        this.hueLightController = hueLightController;
    }
    public override async Task HandleMessage(TurnOffAllLightsMessage notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Turning off all the lights...");
        await this.hueLightController.TurnOffAllLights();
    }
}