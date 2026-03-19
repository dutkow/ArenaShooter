using Godot;
using System;

public enum InputMode
{
    GAME,
    UI,
    CHAT,
    CONSOLE,
}


public partial class PlayerController : Controller
{
    public ChatPanel ChatPanel; // TODO: want to rethink how we route player input to the UI and make a more modular setup

    public InputMode InputMode;

    private InputMode _previousInputMode;


    public override void _Ready()
    {
        base._Ready();

        GD.Print("Player controller created");
    }

    public void RestorePreviousInputMode()
    {
        SetInputMode(_previousInputMode);
    }

    public void SetInputMode(InputMode mode)
    {
        if(InputMode == mode)
        {
            return;
        }

        InputMode = mode;

        switch(InputMode)
        {
            case InputMode.GAME:
                PossessedPawn?.SetInputEnabled(true);
                Input.MouseMode = Input.MouseModeEnum.Captured;
                break;

            case InputMode.UI:
                PossessedPawn?.SetInputEnabled(false);
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;

            case InputMode.CONSOLE:
                _previousInputMode = InputMode;
                break;
        }
    }

    public override void Possess(Pawn pawn)
    {
        base.Possess(pawn);

        if(pawn is Character character)
        {
            GameUI.Instance.OnPossessedCharacter(character);
            //_playerHud.AssignToCharacter(character);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);


        if (Input.IsActionJustPressed("scoreboard"))
        {
            GameUI.Instance.ShowScoreboard();
        }

        if (Input.IsActionJustReleased("scoreboard"))
        {
            GameUI.Instance.HideScoreboard();
        }

        if (Input.IsActionJustPressed("menu"))
        {
            GameUI.Instance.ShowGameMenu();
            SetInputMode(InputMode.UI);
        }


        if (InputMode == InputMode.GAME)
        {
            if (Input.IsActionJustPressed("chat_all"))
            {
                GD.Print($"open chat ran");

                PossessedPawn?.SetInputEnabled(false);
                ChatPanel.Open();

                SetInputMode(InputMode.CHAT);
            }

            if (Input.IsActionJustPressed("chat_team"))
            {
                PossessedPawn?.SetInputEnabled(false);
                ChatPanel.Open();

                SetInputMode(InputMode.CHAT);
            }
        }

        if(InputMode == InputMode.CHAT)
        {
            if (Input.IsActionJustPressed("send_chat"))
            {
                PossessedPawn?.SetInputEnabled(true);
                ChatPanel.SendChat();

                SetInputMode(InputMode.GAME);
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

    public void OnNavigateBackPressed()
    {
        /*
        if(_playerHud.PauseMenu.Visible)
        {
            _playerHud.PauseMenu.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
            PossessedPawn?.SetInputEnabled(true);
        }
        else
        {
            _playerHud.PauseMenu.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            PossessedPawn?.SetInputEnabled(false);
        }*/
    }

    public void Initialize()
    {
        //_playerHud.Push();
    }


}

