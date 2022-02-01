namespace Home.Code.Daemon;
public class WakeUpPcHandler : IMessageHandler<WakeUpPcMessage>
{
    private readonly ILogger<WakeUpPcHandler> logger;

    public WakeUpPcHandler(ILogger<WakeUpPcHandler> logger)
    {
        this.logger = logger;
    }

    public Task Handle(WakeUpPcMessage message)
    {
        this.logger.LogInformation("Woah, this actually worked");
        return Task.CompletedTask;
    }
}
