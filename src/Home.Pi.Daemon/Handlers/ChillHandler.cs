using Home.Pi.Daemon.Controllers;
using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon.Handlers;

internal class ChillOptions
{
    public required string ChillSceneId { get; init; }
    public required Dictionary<string, string> LightServerAnimations { get; init; }
}

internal class ChillHandler(ILogger<ChillHandler> logger, IHueLightController hueLightController, ILightServerController lightServerController, IOptions<ChillOptions> options) : MessageHandler<ChillMessage>
{
    private readonly ILogger<ChillHandler> logger = logger;
    private readonly IHueLightController hueLightController = hueLightController;
    private readonly ILightServerController lightServerController = lightServerController;
    private readonly ChillOptions options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public override async Task HandleMessage(ChillMessage notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Chilling...");
        var tasks = new List<Task>()
        {
            this.hueLightController.ActivateScene(this.options.ChillSceneId)
        };
        tasks.AddRange(this.options.LightServerAnimations.Select(kv => this.lightServerController.StartAnimation(kv.Key, kv.Value)));

        await Task.WhenAll(tasks);
    }
}
