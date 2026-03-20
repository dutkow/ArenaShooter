using Godot;
using System;

public enum HealthStat
{
    HEALTH,
    ARMOR,
}

public partial class HealthStatBar : ProgressBar
{
    public void Initialize(PlayerState playerState, HealthStat healthStatType)
    {
        if(healthStatType == HealthStat.HEALTH)
        {
            Value = playerState.CharacterPrivateState.Health;
            MaxValue = playerState.CharacterPrivateState.MaxHealth;

            playerState.HealthChanged += UpdateStat;
            playerState.MaxHealthChanged += UpdateMaxStat;
        }
        else if(healthStatType == HealthStat.ARMOR)
        {
            Value = playerState.CharacterPrivateState.Armor;
            MaxValue = playerState.CharacterPrivateState.MaxArmor;

            playerState.ArmorChanged += UpdateStat;
            playerState.MaxArmorChanged += UpdateMaxStat;
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
