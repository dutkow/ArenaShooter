using Godot;
using System;

public partial class HealthStatPanel : Control
{
    [Export] HealthStat _healthStatType;
    [Export] HealthStatDisplay _healthStatDisplay;
    [Export] HealthStatBar _healthStatBar;

    public void AssignToHealthComponent(HealthComponent healthComp)
    {
        _healthStatDisplay.AssignHealthComponent(healthComp, _healthStatType);

        _healthStatBar.AssignHealthComponent(healthComp, _healthStatType);
    }
}
