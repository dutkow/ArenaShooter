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

    public void Input (InputEvent @event)
    {
        if (Godot.Input.IsActionJustPressed("toggle_command_console"))
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
    }
}
