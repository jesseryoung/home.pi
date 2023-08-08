using Azure.Core;
using Azure.Identity;
using Home.Pi.Daemon.Services;
using Home.Pi.Daemon.Handlers;
using MediatR;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;
using MQTTnet;
using Home.Pi.Daemon.Controllers;
using Microsoft.Extensions.Options;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Options
        services.AddOptions<QueueStorageOptions>().BindConfiguration("QueueStorageOptions");
        services.AddOptions<PiShelfOptions>().BindConfiguration("PiShelfOptions");
        services.AddOptions<WakeUpPcOptions>().BindConfiguration("WakeUpPcOptions");
        services.AddOptions<HueLightControllerOptions>().BindConfiguration("HueLightControllerOptions");

        services
            .AddHttpClient<IHueLightController, HueLightController>((s, client) =>
            {
                var options = s.GetRequiredService<IOptions<HueLightControllerOptions>>();
                client.BaseAddress = new Uri(options.Value.HueBaseAddress);
                client.DefaultRequestHeaders.Add("hue-application-key", options.Value.HueApplicationKey);
            })
            .ConfigureHttpMessageHandlerBuilder(b => b.PrimaryHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            });


        // Services
        services
            .AddSingleton<TokenCredential, DefaultAzureCredential>()
            .AddQueue()
            .AddTransient(s =>
            {
                return new MqttFactory()
                    .CreateMqttServer(new MqttServerOptionsBuilder()
                        .WithDefaultEndpoint()
                        .Build()
                    );
            })
            .AddHostedService<QueueDaemon>()
            .AddHostedService<MqttDaemon>()
            .AddMediatR(typeof(QueueDaemon), typeof(Message));
    })
    .Build();


await host.RunAsync();
