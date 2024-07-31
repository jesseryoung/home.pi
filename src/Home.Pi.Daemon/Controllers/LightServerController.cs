namespace Home.Pi.Daemon.Controllers;


internal interface ILightServerController
{
    Task TurnOff(string server);
    Task StartAnimation(string server, string animation);
}

internal class LightServerController(HttpClient httpClient, ILogger<LightServerController> logger) : ILightServerController
{
    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<LightServerController> logger = logger;

    public async Task StartAnimation(string server, string animation)
    {
        var response = await this.httpClient.GetStringAsync($"http://{server}:5000/startAnimation/{animation}");
        this.logger.LogInformation("Recieved '{}' from {}", response, server);
    }

    public async Task TurnOff(string server)
    {
        var response = await this.httpClient.GetStringAsync($"http://{server}:5000/stop");
        this.logger.LogInformation("Recieved '{}' from {}", response, server);
    }


}