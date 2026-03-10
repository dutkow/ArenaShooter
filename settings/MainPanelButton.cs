using Godot;
using System;

public partial class MainPanelButton : Control
{
    [Export] MainGamePanelType _gamePanelType;
    [Export] Button _button;
    [Export] TextureRect _iconTextureRect;
    [Export] Texture2D _texture;

    public event Action<MainGamePanelType> Pressed;

    public override void _Ready()
    {
        base._Ready();

        _button.Pressed += OnButtonPressed;

        _iconTextureRect.Texture = _texture;
    }

    public void OnButtonPressed()
    {
        Pressed?.Invoke(_gamePanelType);
    }

}
