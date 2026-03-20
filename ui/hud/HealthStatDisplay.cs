using Godot;
using System;

public partial class HealthStatDisplay : Control
{
    [Export] Label _healthStatLabel;
    [Export] Label _maxHealthStatLabel;

    public void AssignHealthComponent(HealthComponent healthComponent, HealthStat healthStatType)
    {
        if (healthStatType == HealthStat.HEALTH)
        {
            healthComponent.HealthChanged += OnStatChanged;
            healthComponent.MaxHealthChanged += OnMaxStatChanged;
        }
        else if (healthStatType == HealthStat.ARMOR)
        {
            healthComponent.ArmorChanged += OnStatChanged;
            healthComponent.MaxArmorChanged += OnMaxStatChanged;
        }
    }

    public void OnStatChanged(int value)
    {
        _healthStatLabel.Text = value.ToString();
    }

    public void OnMaxStatChanged(int value)
    {
        _maxHealthStatLabel.Text = value.ToString();
    }
}
