using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon;

public class PiShelfOptions
{
    public string? ShelfPiAddress { get; set; }
}
public class ControlPiShelfHandler : IMessageHandler<ControlPiShelfMessage>
{
    private readonly string shelfPiAddress;
    private readonly ILogger<ControlPiShelfHandler> logger;

    public ControlPiShelfHandler(IOptions<PiShelfOptions> options, ILogger<ControlPiShelfHandler> logger)
    {
        if (null == options.Value.ShelfPiAddress)
        {
            throw new ArgumentNullException($"Missing {nameof(PiShelfOptions.ShelfPiAddress)}");
        }

        this.shelfPiAddress = options.Value.ShelfPiAddress;
        this.logger = logger;
    }
    public async Task Handle(ControlPiShelfMessage message)
    {
        this.logger.LogInformation($"Issuing a '{message.Operation.ToString()}' operation to {this.shelfPiAddress}.");
        using var client = new HttpClient();

        var response = message.Operation switch
        {
            ControlPiShelfOperation.TurnOnClock => await client.PostAsync($"{this.shelfPiAddress}/startClock", null),
            ControlPiShelfOperation.TurnOffAnimations => await client.PostAsync($"{this.shelfPiAddress}/stopAnimations", null),
            _ => default
        };

        response?.EnsureSuccessStatusCode();
    }
}