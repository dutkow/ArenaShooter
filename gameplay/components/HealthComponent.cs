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
    private float _healthRechargeAmount;
    private float _shieldRechargeAmount;


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

    /// <summary>
    /// State related variables
    /// </summary>

    private float _timeSinceLastDamaged;

    public void ReceiveDamage(int amount)
    {
        _timeSinceLastDamaged = 0f;

        if (_shield > 0)
        {
            int shieldDamage = Math.Min(_shield, amount);
            _shield -= shieldDamage;

            ShieldChanged?.Invoke(_shield);
            ShieldDamaged?.Invoke();

            if (_shield == 0)
            {
                ShieldExhausted?.Invoke();
            }

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

    }

    public void ApplyShieldDamage(int amount)
    {

    }



    // add health and recharge type events, maybe add a gain health event, etc.
}
