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

    public virtual ClientInputCommand AddInput(ClientInputCommand cmd)
    {
        if (Input.IsActionPressed("move_forward")) cmd.Input |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back")) cmd.Input |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left")) cmd.Input |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd.Input |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionPressed("jump")) cmd.Input |= InputCommand.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd.Input |= InputCommand.FIRE_PRIMARY;

        return cmd;
    }

    public virtual void ApplyInput(ClientInputCommand cmd)
    {

    }
}

