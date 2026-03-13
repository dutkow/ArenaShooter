using Godot;
using System;

public partial class UIRoot : Control
{
    public static UIRoot Instance { get; private set; }

    [Export] PackedScene _playerHudScene;
    private PlayerHud _playerHud;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

    }

    public void OnPossessedCharacter(Character character)
    {
        EnsurePlayerHud();

        _playerHud.AssignToCharacter(character);

        ShowPlayerHud();
    }

    public void OnPossessedArenaCharacter(ArenaCharacterOld character)
    {
        EnsurePlayerHud();

        _playerHud.AssignToArenaCharacter(character);

        ShowPlayerHud();
    }

    public void EnsurePlayerHud()
    {
        if (_playerHud == null)
        {
            _playerHud = (PlayerHud)_playerHudScene.Instantiate();
            AddChild(_playerHud);
        }
    }

    public void ShowPlayerHud()
    {
        _playerHud.Show();
    }

    public void HidePlayerHud()
    {
        _playerHud.Hide();
    }
}
