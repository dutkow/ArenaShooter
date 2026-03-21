using Godot;
using System;

public partial class HealthStatPanel : Control
{
    [Export] HealthStat _healthStatType;
    [Export] HealthStatDisplay _healthStatDisplay;
    [Export] HealthStatBar _healthStatBar;


    public void Initialize(HealthComponent healthComp)
    {
        _healthStatDisplay.Initialize(healthComp, _healthStatType);
        _healthStatBar.Initialize(healthComp, _healthStatType);
    }
}
