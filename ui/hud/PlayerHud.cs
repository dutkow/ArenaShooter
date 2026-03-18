using Godot;
using System;

public partial class PlayerHud : Control
{
    [Export] private ShieldBar _shieldBar;
    [Export] private HealthBar _healthBar;

    [Export] private Scoreboard _scoreboard;

    public void AssignToArenaCharacter(ArenaCharacterOld character)
    {
        var healthComponent = character.HealthComponent;

        _shieldBar.AssignHealthComponent(healthComponent);
        _healthBar.AssignHealthComponent(healthComponent);
    }

    public void AssignToCharacter(Character character)
    {
        var healthComponent = character.HealthComp;

        _shieldBar.AssignHealthComponent(healthComponent);
        _healthBar.AssignHealthComponent(healthComponent);
    }

    public void OpenScoreboard()
    {
        _scoreboard.Visible = true;
    }

    public void CloseScoreboard()
    {
        _scoreboard.Visible = false;
    }

    public void Initialize()
    {
        _scoreboard.Initialize();
    }
}
