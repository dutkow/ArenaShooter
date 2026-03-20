using Godot;
using System;

public enum HealthStat
{
    HEALTH,
    ARMOR,
}

public partial class HealthStatBar : ProgressBar
{
    public void AssignHealthComponent(HealthComponent healthComponent, HealthStat healthStatType)
    {
        if(healthStatType == HealthStat.HEALTH)
        {
            healthComponent.HealthChanged += OnStatChanged;
            healthComponent.MaxHealthChanged += OnMaxStatChanged;
        }
        else if(healthStatType == HealthStat.ARMOR)
        {
            healthComponent.ArmorChanged += OnStatChanged;
            healthComponent.MaxArmorChanged += OnMaxStatChanged;
        }
    }

    public void OnStatChanged(int value)
    {
        Value = value;
    }

    public void OnMaxStatChanged(int value)
    {
        MaxValue = value;
    }
}
