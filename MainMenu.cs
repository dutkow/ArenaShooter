using Godot;
using System;

public partial class MainMenu : Control
{
    [Export] Button _hostGameButton;

    [Export] HostGameMenu _hostGameMenu;

    public override void _Ready()
    {
        base._Ready();

        _hostGameButton.Pressed += OnHostGameButtonPressed;
    }

    public void OnHostGameButtonPressed()
    {
        _hostGameMenu.Open();
    }
}
