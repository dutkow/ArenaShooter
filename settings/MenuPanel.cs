using Godot;
using System;

public partial class MenuPanel : Control
{
    [Export] public MenuPanelHeader MenuPanelHeader;
    [Export] public ButtonWrapper ClosePanelButton;

    private string _titleText;

    [Export]
    public string TitleText
    {
        get => _titleText;
        set
        {
            _titleText = value;
            MenuPanelHeader.TitleLabel.Text = _titleText;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        ClosePanelButton.Pressed += Close;
    }

    public virtual void Close()
    {
        Hide();
    }
}
