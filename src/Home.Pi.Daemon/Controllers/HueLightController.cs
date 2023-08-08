using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Home.Pi.Daemon.Controllers;


internal class HueLightControllerOptions
{
    public required string HueBaseAddress { get; init; }
    public required string HueApplicationKey { get; init; }
    public required string AllLightsGroupId { get; init; }
}
internal interface IHueLightController
{
    Task TurnOffAllLights();
}

internal class HueLightController : IHueLightController
{
    private readonly HttpClient httpClient;
    private readonly HueLightControllerOptions options;

    public HueLightController(HttpClient httpClient, IOptions<HueLightControllerOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }
    public async Task TurnOffAllLights()
    {
        var groupRequest = new
        {
            on = new
            {
                on = false
            },
            dimming = new
            {
                brightness = 0
            }
        };
        await this.httpClient.PutAsJsonAsync($"resource/grouped_light/{this.options.AllLightsGroupId}", groupRequest);
    }

}
