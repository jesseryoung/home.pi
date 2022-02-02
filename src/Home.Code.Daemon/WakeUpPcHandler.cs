using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Home.Code.Daemon;

public class WakeUpPcOptions
{
    public string? MACAddress { get; set; }
}
public class WakeUpPcHandler : IMessageHandler<WakeUpPcMessage>
{
    private readonly ILogger<WakeUpPcHandler> logger;
    private readonly string macAddress;
    private static readonly IPEndPoint BroadcastAddress = new(0xffffffff, 9);

    public WakeUpPcHandler(ILogger<WakeUpPcHandler> logger, IOptions<WakeUpPcOptions> options)
    {
        if (options.Value == null || options.Value.MACAddress == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        this.logger = logger;
        this.macAddress = options.Value.MACAddress;
    }

    public async Task Handle(WakeUpPcMessage message)
    {
        this.logger.LogInformation("Waking up the computer.");
        var address = Convert.FromHexString(this.macAddress.Replace("-", ""));
        var repeatedAddress = Enumerable
            .Range(0, 16)
            .SelectMany(i => address)
            .ToArray();

        var magicPacket = Enumerable
            .Range(0, 6)
            .Select(i => (byte)255)
            .Concat(repeatedAddress)
            .ToArray();

        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.EnableBroadcast = true;

        await sock.SendToAsync(magicPacket, SocketFlags.None, BroadcastAddress);
        await Task.Delay(500);
        await sock.SendToAsync(magicPacket, SocketFlags.None, BroadcastAddress);
        await Task.Delay(500);
        await sock.SendToAsync(magicPacket, SocketFlags.None, BroadcastAddress);
    }
}
