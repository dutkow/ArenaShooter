using Godot;
using System;

public class HealthComponent : Component
{
    /// <summary>
    /// Base health and shield stats
    /// </summary>

    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; } = 100;

    public float HealthPercent => (float)Health / MaxHealth;
    public int MaxShield { get; private set; } = 100;
    public int Shield { get; private set; } = 100;

    public float ShieldPercent => (float)Shield / MaxShield;


    private bool _hasShield => MaxShield > 0;

    // Whether or not health and shield automatically recharge after a period of time post-taking damage
    private bool _healthRecharges;
    private bool _shieldRecharges;

    // Amount of time which must pass without taking damage before recharge begins
    private float _healthRechargeDelay;
    private float _shieldRechargeDelay;

    // When recharging, how much time passes before each regeneration event
    private float _healthRechargeInterval;
    private float _shieldRechargeInterval;

    // When recharging, amount which recharges in each generation event
    private int _healthRechargeAmount;
    private int _shieldRechargeAmount;


    /// <summary>
    /// Health related events
    /// </summary>
    public event Action<int> HealthChanged;
    public event Action<int> MaxHealthChanged;

    public event Action<int> ShieldChanged;
    public event Action<int> MaxShieldChanged;

    public event Action<float> HealthPercentChanged;
    public event Action<float> ShieldPercentChanged;

    public event Action HealthDamaged;
    public event Action ShieldDamaged;

    public event Action Death;
    public event Action ShieldExhausted;

    public event Action HealthPartiallyRestored;
    public event Action ShieldPartiallyRestored;

    public event Action HealthFullyRestored;
    public event Action ShieldFullyRestored;

    public event Action HealthRechargeStarted;
    public event Action ShieldRechargeStarted;

    public event Action HealthRechargeStopped;
    public event Action ShieldRechargeStopped;

    /// <summary>
    /// State related variables
    /// </summary>

    private bool _isHealthFull => Health == MaxHealth;
    private bool _isShieldFull => Shield == MaxShield;

    private bool _isDead => Health <= 0;
    private bool _isShieldExhausted => Shield <= 0;

    private bool _isHealthAndShieldFull => _isHealthFull && _isShieldFull;

    private double _timeSinceLastDamaged;


    private bool _isHealthRecharging;
    private bool _isShieldRecharging;

    private double _healthRechargeAccumulator;
    private double _shieldRechargeAccumulator;

    public void Tick(double delta)
    {
        _timeSinceLastDamaged += delta;

        if (_shieldRecharges && !_isShieldFull)
        {
            if (!_isShieldRecharging && _timeSinceLastDamaged >= _shieldRechargeDelay)
            {
                StartShieldRecharge();
            }
        }

        if (_isShieldRecharging)
        {
            _shieldRechargeAccumulator += delta;

            if (_shieldRechargeAccumulator >= _shieldRechargeInterval)
            {
                _shieldRechargeAccumulator -= _shieldRechargeInterval;
                ShieldRechargeEvent();
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
        if (amount <= 0 || _isDead)
        {
            return;
        }

        SetHealth(Health - amount);
    }

    public void ApplyShieldDamage(int amount)
    {
        if (amount <= 0 || _isShieldExhausted)
        {
            return;
        }

        SetShield(Shield - amount);
    }

    public void ReceiveHealth(int amount)
    {
        if (amount <= 0 || _isHealthFull)
        {
            return;
        }

        SetHealth(Health + amount);
    }

    public void ReceiveShield(int amount)
    {
        if (amount <= 0 || _isShieldFull)
            return;

        SetShield(Shield + amount);
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

        if (_isShieldRecharging)
        {
            StopShieldRecharge();
        }

        if (_hasShield && Shield > 0)
        {
            int shieldDamage = Math.Min(Shield, amount);
            ApplyShieldDamage(shieldDamage);

            int remainingDamage = amount - shieldDamage;
            if (remainingDamage > 0)
                ApplyHealthDamage(remainingDamage);
        }
        else
        {
            ApplyHealthDamage(amount);
        }

        if(Owner != null && Owner is IPlayerEntity playerEntity)
        {
            if(_isDead)
            {
                PlayerDied.Send(playerEntity.GetPlayerID(), 0);
            }
            else if (playerEntity.IsPlayerControlled())
            {
                HealthUpdate.Send(playerEntity.GetPlayerID(), Health, Shield);
            }
        }
    }

    public void StartHealthRecharge()
    {
        _isHealthRecharging = true;
        HealthRechargeStarted?.Invoke();
    }

    public void StartShieldRecharge()
    {
        _isShieldRecharging = true;
        ShieldRechargeStarted?.Invoke();
    }

    public void StopHealthRecharge()
    {
        _isHealthRecharging = false;
        HealthRechargeStopped?.Invoke();
    }

    public void StopShieldRecharge()
    {
        _isShieldRecharging = false;
        ShieldRechargeStopped?.Invoke();
    }

    public void HealthRechargeEvent()
    {
        ReceiveHealth(_healthRechargeAmount);

        if (_isHealthFull)
        {
            StopHealthRecharge();
        }
    }

    public void ShieldRechargeEvent()
    {
        ReceiveShield(_shieldRechargeAmount);

        if (_isShieldFull)
        {
            StopShieldRecharge();
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

            if (_isDead)
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

    public void SetShield(int shield)
    {
        shield = Math.Clamp(shield, 0, MaxShield);

        if (Shield == shield)
        {
            return;
        }

        bool decreased = shield < Shield;
        bool increased = shield > Shield;

        Shield = shield;

        ShieldChanged?.Invoke(Shield);
        ShieldPercentChanged?.Invoke(ShieldPercent);

        if (decreased)
        {
            ShieldDamaged?.Invoke();

            if (_isShieldExhausted)
            {
                ShieldExhausted?.Invoke();
            }
        }
        else if (increased)
        {
            if (_isShieldFull)
            {
                ShieldFullyRestored?.Invoke();
            }
            else
            {
                ShieldPartiallyRestored?.Invoke();
            }
        }
    }
}
