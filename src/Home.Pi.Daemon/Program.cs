using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
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
            .AddMessageHandlers()
            .AddHostedService<Daemon>();
    })
    .Build();


await host.RunAsync();


class Daemon : IHostedService
{
    private const long MinimumWaitTimeMilliseconds = 5000;
    private readonly ILogger<Daemon> logger;
    private readonly QueueClient queueClient;
    private readonly IServiceProvider serviceProvider;

    public Daemon(ILogger<Daemon> logger, QueueClient queueClient, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.queueClient = queueClient;
        this.serviceProvider = serviceProvider;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // WTF, I don't even log in my production apps.
        this.logger.LogInformation("Starting up.");

        // Man, this is getting serious, I'm putting real comments in here.
        while (!cancellationToken.IsCancellationRequested)
        {
            // Get all the messages we can
            var queueMessages = await this.queueClient.ReceiveMessagesAsync(maxMessages: 32);

            if (queueMessages.Value.Length > 0)
            {
                this.logger.LogInformation($"Got {queueMessages.Value.Length} messages from queue.");
            }

            var sw = Stopwatch.StartNew();


            // Group up the messages by their message type.
            var messageGroups = queueMessages.Value.Select(m => new
            {
                Message = Message.Deserialize(m.Body.ToString()),
                MessageId = m.MessageId,
                PopReceipt = m.PopReceipt,
                InsertedOn = m.InsertedOn
            })
            .GroupBy(m => m.Message.MessageType);

            foreach (var messageGroup in messageGroups)
            {
                // Only process groups with matching message types
                if (messageGroup.Key != null)
                {
                    // Only process latest message in group
                    var latestMessage = messageGroup.OrderByDescending(m => m.InsertedOn).First();
                    this.logger.LogInformation($"Executing message handler for '{messageGroup.Key}'.");
                    try
                    {
                        // Wooooo magic box message handler!
                        await this.serviceProvider.InvokeMessageHandler(latestMessage.Message.Value!);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"Error executing message handler for '{messageGroup.Key}'.");
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

            if (sw.ElapsedMilliseconds < MinimumWaitTimeMilliseconds)
            {
                // Oh no, not accounting for an integer overflow!? What are you gonna do about it? Cry?
                await Task.Delay((int)(MinimumWaitTimeMilliseconds - sw.ElapsedMilliseconds));
            }
        }

        this.logger.LogInformation("Shutting down.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
