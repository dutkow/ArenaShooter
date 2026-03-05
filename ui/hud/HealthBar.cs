using Godot;
using System;

public partial class HealthBar : ProgressBar
{
    public void AssignHealthComponent(HealthComponent healthComponent)
    {
        MaxValue = healthComponent.MaxHealth;
        Value = healthComponent.Health;

        healthComponent.HealthChanged += OnHealthChanged;
    }

    public void OnHealthChanged(int newValue)
    {
        Value = newValue;
    }
}
