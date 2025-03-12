using System.Net.Http.Json;

namespace Home.Pi.Daemon.Controllers;


internal class HueLightControllerOptions
{
    public required string HueBaseAddress { get; init; }
    public required string HueApplicationKey { get; init; }
}
internal interface IHueLightController
{
    Task<bool> GetGroupState(string groupId);
    Task SetGroup(string groupId, bool on, byte brightness);
    Task ActivateScene(string sceneId);
}

internal class HueLightController(HttpClient httpClient, ILogger<HueLightController> logger) : IHueLightController
{
    private record Response<T>(IReadOnlyList<T> Data);
    private record State(bool On);
    private record GroupedLight(string Id, State On);

    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<HueLightController> logger = logger;

    public async Task ActivateScene(string sceneId)
    {
        var sceneRequest = new
        {
            recall = new
            {
                action = "active"
            }
        };

        var response = await this.httpClient.PutAsJsonAsync($"resource/scene/{sceneId}", sceneRequest);
        var responseText = await response.Content.ReadAsStringAsync();
        this.logger.LogInformation("Recieved '{}'", responseText);
    }

    public async Task<bool> GetGroupState(string groupId)
    {
        var response = await this.httpClient.GetFromJsonAsync<Response<GroupedLight>>($"resource/grouped_light/{groupId}");
        this.logger.LogInformation("Recieved '{}'", response);
        return response?.Data?.SingleOrDefault()?.On?.On ?? false;
    }

    public async Task SetGroup(string groupId, bool on, byte brightness)
    {
        var groupRequest = new
        {
            on = new
            {
                on
            },
            dimming = new
            {
                brightness
            }
        };
        var response = await this.httpClient.PutAsJsonAsync($"resource/grouped_light/{groupId}", groupRequest);
        var responseText = await response.Content.ReadAsStringAsync();
        this.logger.LogInformation("Recieved '{}'", responseText);
    }

}
