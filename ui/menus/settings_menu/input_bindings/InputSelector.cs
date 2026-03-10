using Godot;
using System;


public partial class InputSelector : Control
{
    [Export] public InputBindingType InputBindingType;
    [Export] public string _pendingInputString = "...";
    [Export] public Button SelectorButton;
    [Export] public Button ClearButton;

    public override void _Ready()
    {
        base._Ready();

        SelectorButton.Pressed += OnSelectorButtonPressed;
        ClearButton.Pressed += OnClearButtonPressed;
    }

    public void OnSelectorButtonPressed()
    {
        SelectorButton.Text = _pendingInputString;
    }

    public void OnClearButtonPressed()
    {
        SelectorButton.Text = " ";
    }

}
