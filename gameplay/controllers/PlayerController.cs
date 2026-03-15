using Godot;
using System;

public partial class PlayerController : Controller
{
    public ChatPanel ChatPanel; // TODO: want to rethink how we route player input to the UI and make a more modular setup


    public override void _Ready()
    {
        base._Ready();

        GD.Print("Player controller created");
    }

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

        if(Input.IsActionJustPressed("chat_all"))
        {
            GD.Print($"open chat ran");

            PossessedPawn?.SetInputEnabled(false);
            ChatPanel.Open();
        }

        if (Input.IsActionJustPressed("chat_team"))
        {
            PossessedPawn?.SetInputEnabled(false);
            ChatPanel.Open();
        }

        if (Input.IsActionJustPressed("send_chat"))
        {
            PossessedPawn?.SetInputEnabled(true);
            ChatPanel.SendChat();
        }
    }

    public virtual ClientInputCommand AddInput(ClientInputCommand cmd)
    {
        if (Input.IsActionPressed("move_forward")) cmd.Input |= ClientInput.FORWARD;
        if (Input.IsActionPressed("move_back")) cmd.Input |= ClientInput.BACKWARD;
        if (Input.IsActionPressed("move_left")) cmd.Input |= ClientInput.STRAFE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd.Input |= ClientInput.STRAFE_RIGHT;
        if (Input.IsActionPressed("jump")) cmd.Input |= ClientInput.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd.Input |= ClientInput.FIRE_PRIMARY;

        return cmd;
    }

    public virtual void ApplyInput(ClientInputCommand cmd)
    {

    }
}

