using Godot;
using System;

public partial class LevelUI : CanvasLayer
{
    public static LevelUI Instance { get; private set; }

    [Export] public PackedScene _playerHudScene;

    private PlayerHud _playerHud;
    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

        _playerHud = (PlayerHud)_playerHudScene.Instantiate();
        _playerHud.Visible = false;
        AddChild(_playerHud);
    }

    public void ShowPlayerHud()
    {
        _playerHud.Show();
    }
}
