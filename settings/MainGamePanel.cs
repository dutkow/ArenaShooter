using Godot;
using System;

public enum MainGamePanelType
{
    DECISIONS,
    RESEARCH,
    DIPLOMACY,
    TRADE,
    CONSTRUCTION,
    INTELLIGENCE,
}

[Tool]
public partial class MainGamePanel : Control
{
    [Export] public MainGamePanelType MainGamePanelType;

    [Export] Button _closePanelButton;

    public override void _Ready()
    {
        base._Ready();

        _closePanelButton.Pressed += OnClosePanelButtonPressed;
    }
    
    public void OnClosePanelButtonPressed()
    {
        Close();
    }

    public virtual void Open()
    {
        Show();
    }

    public virtual void Close()
    {
        Hide();
    }
}
