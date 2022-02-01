using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        // Oh yeah, I'm hardcoding this - what are you going to do, tell my boss?
        services.AddScoped<QueueClient>(s =>
        {
            var creds = s.GetRequiredService<TokenCredential>();
            var client = new QueueClient(new Uri("https://storhome.queue.core.windows.net/messages"), creds);
            client.CreateIfNotExists();
            return client;
        });
        services.AddMessageHandlers();
        services.AddHostedService<Daemon>();
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
        // Man, this is getting serious, I'm putting real comments in this method.
        this.logger.LogInformation("Starting up.");

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
