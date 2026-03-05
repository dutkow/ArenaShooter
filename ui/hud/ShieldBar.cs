using Godot;
using System;

public partial class ShieldBar : ProgressBar
{
    public void AssignHealthComponent(HealthComponent healthComponent)
    {
        MaxValue = healthComponent.MaxShield;
        Value = healthComponent.Shield;

        healthComponent.ShieldChanged += OnShieldChanged;
    }

    public void OnShieldChanged(int newValue)
    {
        Value = newValue;
    }
}