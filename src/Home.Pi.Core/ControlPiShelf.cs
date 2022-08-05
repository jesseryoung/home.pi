namespace Home.Pi.Core;


public enum ControlPiShelfOperation
{
    Unspecified = 0,
    TurnOnClock = 1,
    TurnOffAnimations = 2
}

public class ControlPiShelfMessage : Message
{
    public ControlPiShelfOperation Operation { get; set; }
}