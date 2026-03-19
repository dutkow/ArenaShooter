using Godot;
using System;


public partial class PlayerController : Controller
{
    public ChatPanel ChatPanel; // TODO: want to rethink how we route player input to the UI and make a more modular setup

    public InputMode InputMode;

    private PlayerHud _playerHud;

    public override void _Ready()
    {
        base._Ready();

        GD.Print("Player controller created");

        _playerHud = (PlayerHud)UIRoot.Instance.PlayerHudScene.Instantiate(); // need to rethink where to store UI packed scenes for this sort of thing
        UIRoot.Instance.AddChild(_playerHud);

    }

    public override void Possess(Pawn pawn)
    {
        base.Possess(pawn);

        if(pawn is Character character)
        {
            _playerHud.AssignToCharacter(character);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);


        if (Input.IsActionJustPressed("scoreboard"))
        {
            _playerHud.OpenScoreboard();
        }

        if (Input.IsActionJustReleased("scoreboard"))
        {
            _playerHud.CloseScoreboard();
        }


        if (InputMode == InputMode.GAME)
        {
            if (Input.IsActionJustPressed("chat_all"))
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
    }

    public virtual ClientInputCommand AddInput(ClientInputCommand cmd)
    {
        if (Input.IsActionPressed("move_forward")) cmd.Flags |= InputFlags.FORWARD;
        if (Input.IsActionPressed("move_back")) cmd.Flags |= InputFlags.BACKWARD;
        if (Input.IsActionPressed("move_left")) cmd.Flags |= InputFlags.STRAFE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd.Flags |= InputFlags.STRAFE_RIGHT;
        if (Input.IsActionPressed("jump")) cmd.Flags |= InputFlags.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd.Flags |= InputFlags.FIRE_PRIMARY;

        return cmd;
    }

    public virtual void ApplyInput(ClientInputCommand cmd)
    {

    }

    public void Initialize()
    {
        _playerHud.Initialize();
    }
}

