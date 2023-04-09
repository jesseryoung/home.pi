using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using MediatR;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Options
        services.AddOptions<QueueStorageOptions>().BindConfiguration("QueueStorageOptions");
        services.AddOptions<PiShelfOptions>().BindConfiguration("PiShelfOptions");
        services.AddOptions<WakeUpPcOptions>().BindConfiguration("WakeUpPcOptions");

        // Services
        services.AddSingleton<TokenCredential, DefaultAzureCredential>()
            .AddQueue()
            .AddHostedService<Daemon>()
            .AddMediatR(typeof(Daemon), typeof(Message));
    })
    .Build();


await host.RunAsync();


internal class Daemon : BackgroundService
{
    private readonly ILogger<Daemon> logger;
    private readonly QueueClient queueClient;
    private readonly IServiceProvider serviceProvider;

    // Time delay between queue polls
    private readonly PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

    public Daemon(ILogger<Daemon> logger, QueueClient queueClient, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.queueClient = queueClient;
        this.serviceProvider = serviceProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // WTF, I don't even log in my production apps.
        this.logger.LogInformation("Starting up.");

        // Man, this is getting serious, I'm putting real comments in here.
        // OperationCanceledException suck        
        while (await this.timer.WaitForNextTickAsync(stoppingToken)
            && !stoppingToken.IsCancellationRequested)
        {
            // Get all the messages we can
            var queueMessages = await this.queueClient.ReceiveMessagesAsync(maxMessages: 32, cancellationToken: stoppingToken);

            if (queueMessages.Value.Length > 0)
            {
                this.logger.LogInformation("Got {} messages from queue.", queueMessages.Value.Length);
            }

            // Group up the messages by their message type.
            var messageGroups = queueMessages.Value.Select(m => new
            {
                Message = Message.Deserialize(m.Body.ToString()),
                m.MessageId,
                m.PopReceipt,
                m.InsertedOn
            })
            .Where(m => m.Message != null)
            .GroupBy(m => m.Message!.GetType());

            foreach (var messageGroup in messageGroups)
            {
                // Only process groups with matching message types
                if (messageGroup.Key != null)
                {
                    // Only process latest message in group
                    var latestMessage = messageGroup.OrderByDescending(m => m.InsertedOn).First();
                    this.logger.LogInformation("Executing message handler for '{}'.", messageGroup.Key);
                    try
                    {
                        using var scope = this.serviceProvider.CreateAsyncScope();
                        var mediator = this.serviceProvider.GetRequiredService<IMediator>();
                        await mediator.Publish(latestMessage.Message!, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Error executing message handler for '{}'.", messageGroup.Key);
                    }

                    // Leave messages on the queue that I'm not configured to handle
                    // I'm not sure why, but that kind of sounds like a bad idea.... fuck it.
                    // Also, I'm not actually putting them back on the queue, I'm just ignore them and waiting for their pop token to expire.
                    foreach (var message in messageGroup)
                    {
                        await this.queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, CancellationToken.None);
                    }
                }
            }
        }

        this.logger.LogInformation("Shutting down.");
    }
}
