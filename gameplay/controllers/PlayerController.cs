using Godot;
using System;

public partial class PlayerController : Controller
{
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustPressed("net_profile_great"))
        {
            NetworkSender.SetNetworkProfile(NetworkSender.NetworkProfile.GREAT);
            GD.Print("Network profile: Great");
        }
        else if (Input.IsActionJustPressed("net_profile_good"))
        {
            NetworkSender.SetNetworkProfile(NetworkSender.NetworkProfile.GOOD);
            GD.Print("Network profile: Good");
        }
        else if (Input.IsActionJustPressed("net_profile_average"))
        {
            NetworkSender.SetNetworkProfile(NetworkSender.NetworkProfile.AVERAGE);
            GD.Print("Network profile: Average");
        }
        else if (Input.IsActionJustPressed("net_profile_bad"))
        {
            NetworkSender.SetNetworkProfile(NetworkSender.NetworkProfile.BAD);
            GD.Print("Network profile: Bad");
        }
        else if (Input.IsActionJustPressed("net_emulation_toggle"))
        {
            NetworkSender.ToggleNetEmulation(!NetworkSender.EmulationEnabled);
            GD.Print($"Emulation enabled: {NetworkSender.EmulationEnabled}");
        }
    }
}
