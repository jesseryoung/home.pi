using System.Net;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Home.Pi.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((builder, services) =>
    {
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddOptions<QueueStorageOptions>().BindConfiguration("QueueStorageOptions");
        services.AddQueue();
    })
    .Build();

await host.RunAsync();





public class Functions
{
    private readonly QueueClient queueClient;

    public Functions(QueueClient queueClient)
    {
        this.queueClient = queueClient;
    }

    [Function(nameof(Health))]
    public async Task<HttpResponseData> Health([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            Hello = "Wrold"
        });
        return response;
    }


    [Function(nameof(WakeUp))]
    public async Task<HttpResponseData> WakeUp([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        var message = new WakeUpPcMessage();

        await this.queueClient.SendMessageAsync(message.Serialize());

        return request.CreateResponse(HttpStatusCode.OK);
    }


    [Function(nameof(TurnOffAnimations))]
    public async Task<HttpResponseData> TurnOffAnimations([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        var message = new ControlPiShelfMessage()
        {
            Operation = ControlPiShelfOperation.TurnOffAnimations
        };
        await this.queueClient.SendMessageAsync(message.Serialize());

        return request.CreateResponse(HttpStatusCode.OK);
    }


    [Function(nameof(TurnOnClock))]
    public async Task<HttpResponseData> TurnOnClock([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        var message = new ControlPiShelfMessage()
        {
            Operation = ControlPiShelfOperation.TurnOnClock
        };
        await this.queueClient.SendMessageAsync(message.Serialize());

        return request.CreateResponse(HttpStatusCode.OK);
    }


}