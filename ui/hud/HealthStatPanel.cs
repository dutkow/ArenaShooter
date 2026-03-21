using Godot;
using System;

public partial class HealthStatPanel : Control
{
    [Export] HealthStat _healthStatType;
    [Export] HealthStatDisplay _healthStatDisplay;
    [Export] HealthStatBar _healthStatBar;


    public void Initialize(PlayerStateOld playerState)
    {
        _healthStatDisplay.Initialize(playerState, _healthStatType);
        _healthStatBar.Initialize(playerState, _healthStatType);
    }
}
