using System;

public class HealthComponent
{
    /// <summary>
    /// Base health and shield stats
    /// </summary>

    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; } = 100;

    public int MaxShield { get; private set; } = 100;
    public int Shield { get; private set; } = 100;

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

    public event Action HealthExhausted;
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

    private bool _isHealthExhausted => Health <= 0;
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

    public void ApplyDamage(int amount)
    {
        _timeSinceLastDamaged = 0;

        if(_isHealthRecharging)
        {
            StopHealthRecharge();
        }

        if(_isShieldRecharging)
        {
            StopShieldRecharge();
        }

        if (Shield > 0)
        {
            int shieldDamage = Math.Min(Shield, amount);
            ApplyShieldDamage(shieldDamage);

            int remainingDamage = amount - shieldDamage;

            if (remainingDamage > 0)
            {
                ApplyHealthDamage(remainingDamage);
            }
        }
        else
        {
            ApplyHealthDamage(amount);
        }
    }

    public void ApplyHealthDamage(int amount)
    {
        if (amount <= 0 || _isHealthExhausted)
        {
            return;
        }

        Health = Math.Max(0, Health - amount);

        HealthChanged?.Invoke(Health);
        HealthPercentChanged?.Invoke((float)Health / MaxHealth);
        HealthDamaged?.Invoke();

        if (_isHealthExhausted)
        {
            HealthExhausted?.Invoke();
        }
    }

    public void ApplyShieldDamage(int amount)
    {
        if (amount <= 0 || _isShieldExhausted)
        {
            return;
        }

        Shield = Math.Max(0, Shield - amount);

        ShieldChanged?.Invoke(Shield);
        ShieldPercentChanged?.Invoke((float)Shield / MaxShield);
        ShieldDamaged?.Invoke();

        if (_isShieldExhausted)
        {
            ShieldExhausted?.Invoke();
        }
    }

    public void ReceiveHealth(int amount)
    {
        if (amount <= 0 || _isHealthFull)
        {
            return;
        }

        Health = Math.Min(MaxHealth, Health + amount);

        HealthChanged?.Invoke(Health);
        HealthPercentChanged?.Invoke((float)Health / MaxHealth);

        if (_isHealthFull)
        {
            HealthFullyRestored?.Invoke();
        }
        else
        {
            HealthPartiallyRestored?.Invoke();
        }
    }

    public void ReceiveShield(int amount)
    {
        if (amount <= 0 || _isShieldFull)
        {
            return;
        }

        Shield = Math.Min(MaxShield, Shield + amount);

        ShieldChanged?.Invoke(Shield);
        ShieldPercentChanged?.Invoke((float)Shield / MaxShield);

        if (_isShieldFull)
        {
            ShieldFullyRestored?.Invoke();
        }
        else
        {
            ShieldPartiallyRestored?.Invoke();
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
}
