using Godot;
using System;

public enum HealthStat
{
    HEALTH,
    ARMOR,
}

public partial class HealthStatBar : ProgressBar
{
    public void Initialize(HealthComponent healthComp, HealthStat healthStatType)
    {
        if(healthStatType == HealthStat.HEALTH)
        {
            Value = healthComp.Health;
            MaxValue = healthComp.MaxHealth;

            healthComp.HealthChanged += UpdateStat;
            healthComp.MaxHealthChanged += UpdateMaxStat;
        }
        else if(healthStatType == HealthStat.ARMOR)
        {
            Value = healthComp.Armor;
            MaxValue = healthComp.MaxArmor;

            healthComp.ArmorChanged += UpdateStat;
            healthComp.MaxArmorChanged += UpdateMaxStat;
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
