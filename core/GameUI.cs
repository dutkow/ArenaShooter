using Godot;
using System;

public partial class GameUI : LevelUI
{
    public static GameUI Instance { get; private set; }

    [Export] PlayerHud _playerHud;
    [Export] Scoreboard _scoreboard;
    [Export] MatchHud _matchHud;
    [Export] GameMenu _menu;
    [Export] SettingsMenu _settingsMenu;

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }

    public void ShowPlayerHUD()
    {
        _playerHud.Show();
    }

    public void HidePlayerHUD()
    {
        _playerHud.Hide();
    }

    public void ShowScoreboard()
    {
        _scoreboard.Show();
    }

    public void HideScoreboard()
    {
        _scoreboard.Hide();
    }

    public void ShowMatchHud()
    {
        _matchHud.Show();
    }

    public void HideMatchHud()
    {
        _matchHud.Hide();
    }

    public void ShowSettingsMenu()
    {
        _settingsMenu.Show();
    }

    public void HideSettingsMenu()
    {
        _settingsMenu.Hide();
    }

    public void ShowGameMenu()
    {
        _menu.Show();
    }

    public void HideGameMenu()
    {
        _menu.Hide();
    }

    public void ShowPrompt(string text)
    {

    }

    public void HidePrompt()
    {

    }

    public void OnPossessedCharacter(Character character)
    {
        _playerHud.AssignToCharacter(character);
        _playerHud.Show();
    }

    public void PopulateInitialPlayerList()
    {
        _scoreboard.PopulateInitialPlayerList();
    }
}
