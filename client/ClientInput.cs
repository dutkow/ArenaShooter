using Godot;
using System;

public class ClientInput
{
    public static ClientInput Instance { get; private set; }

    InputMode Mode;

    public ClientInput()
    {
        Instance = this;
    }

    public void HandleInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("toggle_command_console"))
        {
            bool toggledOn = CommandConsole.Instance.Toggle();

            if (toggledOn)
            {
                Mode = InputMode.CONSOLE;

                ClientGame.Instance?.LocalPlayerController?.PossessedPawn?.SetInputEnabled(false);
            }
            else
            {
                Mode = InputMode.GAME;

                ClientGame.Instance?.LocalPlayerController?.PossessedPawn?.SetInputEnabled(true);
            }
        }

        if(Input.IsActionJustPressed("navigate_back"))
        {
            ClientGame.Instance?.LocalPlayerController?.OnNavigateBackPressed(); // todo: rethink input handling and UI navigation
            GD.Print("navigate back pressed!");
        }

        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                ClientGame.Instance?.LocalPlayerController?.PossessedPawn?.SetInputEnabled(false);
            }
            else if (Input.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                ClientGame.Instance?.LocalPlayerController?.PossessedPawn?.SetInputEnabled(true);
            }
        }
    }


}
