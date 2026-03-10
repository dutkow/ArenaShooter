using Godot;
using System;
using System.Collections.Generic;

public enum SettingApplyMode
{
    IMMEDIATE,
    ON_SAVE,
    ON_RESTART,
}

public interface ISetting
{
    void Apply();
    void Revert();
    bool IsDirty { get; }
}

public class Setting<T> : ISetting
{
    private T _value;
    private T _pending;
    private T _original;

    private float _max;

    private float _isEqualThreshold = 0.005f;

    public T Value => _value;

    private bool AreEqual(T a, T b)
    {
        if (typeof(T) == typeof(float))
        {
            float fa = (float)(object)a;
            float fb = (float)(object)b;
            return Math.Abs(fa - fb) < _isEqualThreshold;
        }

        return EqualityComparer<T>.Default.Equals(a, b);
    }
    public bool IsPendingDirty => !AreEqual(_pending, _original);

    public T Pending
    {
        get => _pending;
        set
        {
            if (!AreEqual(_pending, value))
            {
                if (!IsPendingDirty)
                {
                    _original = _value;
                }

                _pending = value;

                if (IsPendingDirty)
                {
                    SettingsManager.Instance.RegisterPendingChange(this);
                }
                else
                {
                    SettingsManager.Instance.UnregisterPendingChange(this);
                }

                if (ApplyMode == SettingApplyMode.IMMEDIATE)
                {
                    _value = _pending;
                    Changed?.Invoke(_value);
                }
            }
        }
    }

    public SettingApplyMode ApplyMode { get; set; } = SettingApplyMode.ON_SAVE;

    public bool IsDirty => !EqualityComparer<T>.Default.Equals(_value, _pending);

    public event Action<T> Changed;

    public Setting(T value, SettingApplyMode mode = SettingApplyMode.ON_SAVE)
    {
        _value = value;
        _pending = value;
        _original = value;
        ApplyMode = mode;
    }

    public static Setting<T> Create(T value, SettingsConfig gameConfig)
    {
        return new Setting<T>(value, gameConfig.SettingApplyMode);
    }


    public static Setting<T> FromValue(T value, SettingApplyMode mode = SettingApplyMode.ON_SAVE)
    {
        return new Setting<T>(value, mode);
    }

    public static Setting<T> FromConfig<T>(EnumSettingConfig config)
        where T : struct, Enum
    {
        T enumValue = TextUtils.EnumFromString<T>(config.Value.ToUpper());
        return new Setting<T>(enumValue, config.SettingApplyMode);
    }


    public static Setting<float> FromConfig(FloatSettingConfig config)
    {
        var setting = new Setting<float>(config.Value, config.SettingApplyMode);
        setting._isEqualThreshold = config.IsEqualTolerance;
        return setting;
    }



    public void Apply()
    {
        _value = _pending;
        Changed?.Invoke(_value);

        SettingsManager.Instance.UnregisterPendingChange(this);
        _original = _value;
    }

    public void Revert()
    {
        _pending = _original;

        if (ApplyMode == SettingApplyMode.IMMEDIATE)
        {
            _value = _original;
        }

        Changed?.Invoke(_value);

        SettingsManager.Instance.UnregisterPendingChange(this);
    }
}

