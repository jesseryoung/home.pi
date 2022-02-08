namespace Home.Pi.Core;


public enum ControlPiShelfOperation
{
    Unspecified = 0,
    TurnOnClock = 1,
    TurnOffAnimations = 2
}

[Message(nameof(ControlPiShelfMessage))]
public class ControlPiShelfMessage : Message
{
    public ControlPiShelfOperation Operation { get; set; }
}