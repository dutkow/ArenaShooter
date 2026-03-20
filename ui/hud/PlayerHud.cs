using Godot;
using System;

public partial class PlayerHud : Control
{
    [Export] private HealthStatPanel _armorPanel;
    [Export] private HealthStatPanel _healthPanel;

    public void AssignToArenaCharacter(ArenaCharacterOld character)
    {
        var healthComponent = character.HealthComponent;

        //.AssignHealthComponent(healthComponent);
        //_healthPanel.AssignHealthComponent(healthComponent);
    }

    public void AssignToCharacter(Character character)
    {
        var healthComponent = character.HealthComp;

        _armorPanel.AssignToHealthComponent(healthComponent);
        _healthPanel.AssignToHealthComponent(healthComponent);
    }
}
