using Azure.Core;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Home.Code.Contracts;

public static class QueueExtensiosn
{

    public static IServiceCollection AddQueue(this IServiceCollection services)
    {
        services.AddSingleton<QueueClient>(s =>
        {
            var options = s.GetRequiredService<IOptions<QueueStorageOptions>>();
            if (options.Value.Account!.StartsWith("https://"))
            {
                var uri = new Uri(options.Value.Account);
                return new QueueClient(new Uri(uri, options.Value.QueueName), s.GetRequiredService<TokenCredential>());
            }
            else
            {
                var client = new QueueClient(options.Value.Account, options.Value.QueueName);
                client.CreateIfNotExists();
                return client;
            }
        });

        return services;
    }
}