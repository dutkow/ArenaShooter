using Godot;
using System;

public partial class PlayerHud : Control
{
    [Export] private ShieldBar _shieldBar;
    [Export] private HealthBar _healthBar;

    public void AssignToArenaCharacter(ArenaCharacter character)
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
}
