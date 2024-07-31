using Home.Pi.Daemon.Controllers;
using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon.Handlers;



internal class ControlGroupedLightsHandlerOptions
{
    public required Dictionary<string, string> GroupNames { get; init; }
}

internal class ControlGroupedLightsHandler(ILogger<ControlGroupedLightsHandler> logger, IHueLightController hueLightController, IOptions<ControlGroupedLightsHandlerOptions> options)
          : MessageHandler<ControlGroupedLightsMessage>
{
    private readonly ILogger<ControlGroupedLightsHandler> logger = logger;
    private readonly IHueLightController hueLightController = hueLightController;
    private readonly ControlGroupedLightsHandlerOptions options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public override async Task HandleMessage(ControlGroupedLightsMessage notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Controlling {}", notification.Group);

        await this.hueLightController.SetGroup(this.options.GroupNames[notification.Group], notification.On, notification.Brightness);
    }
}