namespace Home.Pi.Core.Messages;

public class TemperatureSensorMessage : Message
{
    public required string Device { get; init; }
    public int Battery { get; init; }
    public double Humidity { get; init; }
    public int LinkQuality { get; init; }
    public double Temperature { get; init; }
}