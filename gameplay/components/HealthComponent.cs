using Godot;
using System;

[Flags]
public enum HealthStateFlags : byte
{
    HEALTH_CHANGED,
    MAX_HEALTH_CHANGED,
    ARMOR_CHANGED,
    MAX_ARMOR_CHANGED,
}

public struct HealthState
{
    public HealthStateFlags Flags;

    public byte Health;
    public byte MaxHealth;
    public byte Armor;
    public byte MaxArmor;
}

public class HealthComponent : Component
{
    public HealthState State;

    /// <summary>
    /// Base health and armor stats
    /// </summary>

    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; } = 100;

    public float HealthPercent => (float)Health / MaxHealth;
    public int MaxArmor { get; private set; } = 100;
    public int Armor { get; private set; } = 100;

    public float ArmorPercent => (float)Armor / MaxArmor;


    private bool _hasArmor => MaxArmor > 0;

    private bool _healthRecharges;
    private bool _armorRecharges;

    private float _healthRechargeDelay;
    private float _armorRechargeDelay;

    private float _healthRechargeInterval;
    private float _armorRechargeInterval;

    private int _healthRechargeAmount;
    private int _armorRechargeAmount;


    /// <summary>
    /// Health related events
    /// </summary>
    public event Action<int> HealthChanged;
    public event Action<int> MaxHealthChanged;

    public event Action<int> ArmorChanged;
    public event Action<int> MaxArmorChanged;

    public event Action<float> HealthPercentChanged;
    public event Action<float> ArmorPercentChanged;

    public event Action HealthDamaged;
    public event Action ArmorDamaged;

    public event Action Death;
    public event Action ArmorExhausted;

    public event Action HealthPartiallyRestored;
    public event Action ArmorPartiallyRestored;

    public event Action HealthFullyRestored;
    public event Action ArmorFullyRestored;

    public event Action HealthRechargeStarted;
    public event Action ArmorRechargeStarted;

    public event Action HealthRechargeStopped;
    public event Action ArmorRechargeStopped;

    /// <summary>
    /// State related variables
    /// </summary>

    private bool _isHealthFull => Health == MaxHealth;
    private bool _isArmorFull => Armor == MaxArmor;

    public bool IsAlive => Health > 0;
    private bool _isArmorExhausted => Armor <= 0;

    private bool _isHealthAndArmorFull => _isHealthFull && _isArmorFull;

    private double _timeSinceLastDamaged;


    private bool _isHealthRecharging;
    private bool _isArmorRecharging;

    private double _healthRechargeAccumulator;
    private double _armorRechargeAccumulator;

    public void Tick(double delta)
    {
        _timeSinceLastDamaged += delta;

        if (_armorRecharges && !_isArmorFull)
        {
            if (!_isArmorRecharging && _timeSinceLastDamaged >= _armorRechargeDelay)
            {
                StartArmorRecharge();
            }
        }

        if (_isArmorRecharging)
        {
            _armorRechargeAccumulator += delta;

            if (_armorRechargeAccumulator >= _armorRechargeInterval)
            {
                _armorRechargeAccumulator -= _armorRechargeInterval;
                ArmorRechargeEvent();
            }
        }

        if (_healthRecharges && !_isHealthFull)
        {
            if (!_isHealthRecharging && _timeSinceLastDamaged >= _healthRechargeDelay)
            {
                StartHealthRecharge();
            }
        }

        if (_isHealthRecharging)
        {
            _healthRechargeAccumulator += delta;

            if (_healthRechargeAccumulator >= _healthRechargeInterval)
            {
                _healthRechargeAccumulator -= _healthRechargeInterval;
                HealthRechargeEvent();
            }
        }
    }

    public void ApplyHealthDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        SetHealth(Health - amount);
    }

    public void ApplyArmorDamage(int amount)
    {
        if (amount <= 0 || _isArmorExhausted)
        {
            return;
        }

        SetArmor(Armor - amount);
    }

    public void ReceiveHealth(int amount)
    {
        if (amount <= 0 || _isHealthFull)
        {
            return;
        }

        SetHealth(Health + amount);
    }

    public void ReceiveArmor(int amount)
    {
        if (amount <= 0 || _isArmorFull)
            return;

        SetArmor(Armor + amount);
    }

    public void ApplyDamage(int amount)
    {
        _timeSinceLastDamaged = 0;

        if(amount <= 0)
        {
            return;
        }

        if (_isHealthRecharging)
        {
            StopHealthRecharge();
        }

        if (_isArmorRecharging)
        {
            StopArmorRecharge();
        }

        if (_hasArmor && Armor > 0)
        {
            int armorDamage = Math.Min(Armor, amount);
            ApplyArmorDamage(armorDamage);

            int remainingDamage = amount - armorDamage;
            if (remainingDamage > 0)
            {
                ApplyHealthDamage(remainingDamage);
            }
        }
        else
        {
            ApplyHealthDamage(amount);
        }

        if(Owner != null && Owner is IPlayerEntity playerEntity)
        {
            if(IsAlive)
            {
                //PlayerDied.Send(playerEntity.GetPlayerID(), 0);
            }
            else if (playerEntity.IsPlayerControlled())
            {
                //HealthUpdate.Send(playerEntity.GetPlayerID(), Health, ARMOR);
            }
        }
    }

    public void StartHealthRecharge()
    {
        _isHealthRecharging = true;
        HealthRechargeStarted?.Invoke();
    }

    public void StartArmorRecharge()
    {
        _isArmorRecharging = true;
        ArmorRechargeStarted?.Invoke();
    }

    public void StopHealthRecharge()
    {
        _isHealthRecharging = false;
        HealthRechargeStopped?.Invoke();
    }

    public void StopArmorRecharge()
    {
        _isArmorRecharging = false;
        ArmorRechargeStopped?.Invoke();
    }

    public void HealthRechargeEvent()
    {
        ReceiveHealth(_healthRechargeAmount);

        if (_isHealthFull)
        {
            StopHealthRecharge();
        }
    }

    public void ArmorRechargeEvent()
    {
        ReceiveArmor(_armorRechargeAmount);

        if (_isArmorFull)
        {
            StopArmorRecharge();
        }
    }

    public void SetHealth(int health)
    {
        health = Math.Clamp(health, 0, MaxHealth);

        if (Health == health)
        {
            return;
        }

        bool decreased = health < Health;
        bool increased = health > Health;

        Health = health;

        // Always fire value changed events
        HealthChanged?.Invoke(Health);
        HealthPercentChanged?.Invoke(HealthPercent);

        // Handle events based on change direction
        if (decreased)
        {
            HealthDamaged?.Invoke();

            if (IsAlive)
            {
                Death?.Invoke();
            }
        }
        else if (increased)
        {
            if (_isHealthFull)
            {
                HealthFullyRestored?.Invoke();
            }
            else
            {
                HealthPartiallyRestored?.Invoke();
            }
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        maxHealth = Math.Max(0, maxHealth);

        if (MaxHealth != maxHealth)
        {
            MaxHealth = maxHealth;
            MaxHealthChanged?.Invoke(maxHealth);
        }
    }

    public void SetArmor(int armor)
    {
        armor = Math.Clamp(armor, 0, MaxArmor);

        if (Armor == armor)
        {
            return;
        }

        bool decreased = armor < Armor;
        bool increased = armor > Armor;

        Armor = armor;

        ArmorChanged?.Invoke(Armor);
        ArmorPercentChanged?.Invoke(ArmorPercent);

        if (decreased)
        {
            ArmorDamaged?.Invoke();

            if (_isArmorExhausted)
            {
                ArmorExhausted?.Invoke();
            }
        }
        else if (increased)
        {
            if (_isArmorFull)
            {
                ArmorFullyRestored?.Invoke();
            }
            else
            {
                ArmorPartiallyRestored?.Invoke();
            }
        }
    }

    public void SetMaxArmor(int maxArmor)
    {
        maxArmor = Math.Max(0, maxArmor);

        if (MaxArmor != maxArmor)
        {
            MaxArmor = maxArmor;
            MaxArmorChanged?.Invoke(maxArmor);
        }
    }

    public void OnSpawned()
    {
        //SetMaxHealth(GameRules.Instance.MaxHealth);
        SetHealth(GameRules.Instance.StartingHealth);

        //SetMaxArmor(GameRules.Instance.MaxArmor);
        SetArmor(GameRules.Instance.MaxArmor);
    }

    public void ApplyState(HealthState state)
    {
        if ((state.Flags & HealthStateFlags.HEALTH_CHANGED) != 0)
        {
            SetHealth(state.Health);
        }

        if ((state.Flags & HealthStateFlags.MAX_HEALTH_CHANGED) != 0)
        {
            SetMaxHealth(state.MaxHealth);
        }

        if ((state.Flags & HealthStateFlags.ARMOR_CHANGED) != 0)
        {
            SetArmor(state.Armor);
        }

        if ((state.Flags & HealthStateFlags.MAX_ARMOR_CHANGED) != 0)
        {
            SetMaxArmor(state.MaxArmor);
        }
    }
}
