using Godot;
using System;

public partial class HealthStatDisplay : Control
{
    [Export] Label _healthStatLabel;
    [Export] Label _maxHealthStatLabel;

    public void Initialize(HealthComponent healthComp, HealthStat healthStatType)
    {
        if (healthStatType == HealthStat.HEALTH)
        {
            UpdateStat(healthComp.State.Health);
            UpdateMaxStat(healthComp.State.MaxHealth);

            healthComp.HealthChanged += UpdateStat;
            healthComp.MaxHealthChanged += UpdateMaxStat;
        }
        else if (healthStatType == HealthStat.ARMOR)
        {
            UpdateStat(healthComp.Armor);
            UpdateMaxStat(healthComp.MaxArmor);

            healthComp.ArmorChanged += UpdateStat;
            healthComp.MaxArmorChanged += UpdateMaxStat;
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
