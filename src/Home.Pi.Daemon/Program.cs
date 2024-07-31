using Home.Pi.Daemon.Services;
using Home.Pi.Daemon.Handlers;
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
        services.AddOptions<WakeUpPcOptions>().BindConfiguration(nameof(WakeUpPcOptions));
        services.AddOptions<HueLightControllerOptions>().BindConfiguration(nameof(HueLightControllerOptions));
        services.AddOptions<TurnOffAllLightsOptions>().BindConfiguration(nameof(TurnOffAllLightsOptions));
        services.AddOptions<ChillOptions>().BindConfiguration(nameof(ChillOptions));
        services.AddOptions<ControlGroupedLightsHandlerOptions>().BindConfiguration(nameof(ControlGroupedLightsHandlerOptions));

        services
            .AddHttpClient<IHueLightController, HueLightController>((s, client) =>
            {
                var options = s.GetRequiredService<IOptions<HueLightControllerOptions>>();
                client.BaseAddress = new Uri(options.Value.HueBaseAddress);
                client.DefaultRequestHeaders.Add("hue-application-key", options.Value.HueApplicationKey);
            })
            .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            });


        // Services
        services
            .AddScoped<ILightServerController, LightServerController>()
            .AddTransient(s =>
            {
                return new MqttFactory()
                    .CreateMqttServer(new MqttServerOptionsBuilder()
                        .WithDefaultEndpoint()
                        .Build()
                    );
            })
            .AddHostedService<MqttDaemon>()
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(typeof(MqttDaemon).Assembly);
                cfg.RegisterServicesFromAssemblies(typeof(Message).Assembly);
            });
    })
    .Build();


await host.RunAsync();
