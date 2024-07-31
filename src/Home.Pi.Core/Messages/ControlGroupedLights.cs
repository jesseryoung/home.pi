namespace Home.Pi.Core.Messages;


public class ControlGroupedLightsMessage : Message
{
    public required string Group { get; init; }
    public bool On { get; init; } = true;
    public byte Brightness { get; init; } = 100;


}