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
            Value = healthComponent.Health;
            MaxValue = healthComponent.MaxHealth;

            healthComponent.HealthChanged += UpdateStat;
            healthComponent.MaxHealthChanged += UpdateMaxStat;
        }
        else if(healthStatType == HealthStat.ARMOR)
        {
            Value = healthComponent.Armor;
            MaxValue = healthComponent.MaxArmor;

            healthComponent.ArmorChanged += UpdateStat;
            healthComponent.MaxArmorChanged += UpdateMaxStat;
        }
    }

    public void UpdateStat(int value)
    {
        Value = value;
    }

    public void UpdateMaxStat(int value)
    {
        MaxValue = value;
    }
}
