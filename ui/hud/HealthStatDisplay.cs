using Godot;
using System;

public partial class HealthStatDisplay : Control
{
    [Export] Label _healthStatLabel;
    [Export] Label _maxHealthStatLabel;

    public void Initialize(PlayerState playerState, HealthStat healthStatType)
    {
        if (healthStatType == HealthStat.HEALTH)
        {
            UpdateStat(playerState.CharacterPrivateState.Health);
            UpdateMaxStat(playerState.CharacterPrivateState.MaxHealth);

            playerState.HealthChanged += UpdateStat;
            playerState.MaxHealthChanged += UpdateMaxStat;
        }
        else if (healthStatType == HealthStat.ARMOR)
        {
            UpdateStat(playerState.CharacterPrivateState.Armor);
            UpdateMaxStat(playerState.CharacterPrivateState.MaxArmor);

            playerState.ArmorChanged += UpdateStat;
            playerState.MaxArmorChanged += UpdateMaxStat;
        }
    }

    public void UpdateStat(int value)
    {
        _healthStatLabel.Text = value.ToString();
    }

    public void UpdateMaxStat(int value)
    {
        _maxHealthStatLabel.Text = "/" + value.ToString();
    }
}
