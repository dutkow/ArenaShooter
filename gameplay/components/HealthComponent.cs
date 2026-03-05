using Godot;
using Godot.NativeInterop;
using System;

public class HealthComponent
{
    /// <summary>
    /// Base health and shield stats
    /// </summary>

    private int _maxHealth = 100;
    private int _health = 100;

    private int _shield = 100;
    private int _maxShield = 100;

    private bool _hasShield => _maxShield > 0;

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

    private bool _isHealthFull => _health == _maxHealth;
    private bool _isShieldFull => _shield == _maxShield;

    private bool _isHealthExhausted => _health <= 0;
    private bool _isShieldExhausted => _shield <= 0;

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

    public void ReceiveDamage(int amount)
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

        if (_shield > 0)
        {
            int shieldDamage = Math.Min(_shield, amount);
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

        _health = Math.Max(0, _health - amount);

        HealthChanged?.Invoke(_health);
        HealthPercentChanged?.Invoke((float)_health / _maxHealth);
        HealthDamaged?.Invoke();

        if (_health == 0)
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

        _shield = Math.Max(0, _shield - amount);

        ShieldChanged?.Invoke(_shield);
        ShieldPercentChanged?.Invoke((float)_shield / _maxShield);
        ShieldDamaged?.Invoke();

        if (_shield == 0)
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

        _health = Math.Min(_maxHealth, _health + amount);

        HealthChanged?.Invoke(_health);
        HealthPercentChanged?.Invoke((float)_health / _maxHealth);

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

        _shield = Math.Min(_maxShield, _shield + amount);

        ShieldChanged?.Invoke(_shield);
        ShieldPercentChanged?.Invoke((float)_shield / _maxShield);

        if (_shield == _maxShield)
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
