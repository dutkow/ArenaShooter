using Godot;
using System;
using System.Reflection;

public partial class SliderSettingsEntry : SettingsEntry
{
    [Export] public Slider Slider;
    [Export] public Label PercentageLabel;

    [Export] public Button IncreaseButton;
    [Export] public Button DecreaseButton;

    [Export] public float InitialDelay = 0.25f;
    [Export] public float RepeatInterval = 0.05f;
    [Export] public double StepAmount;

    [Export] public bool InvertSlider = false;

    // NEW: the string path such as "Audio.MasterVolume"
    [Export] public string SettingPath = "";

    private Timer _repeatTimer;
    private double _currentDelta;

    public double SliderRange => Slider.MaxValue - Slider.MinValue;

    private Setting<float> Setting;
    private float MinValue;
    private float MaxValue;

    public override void _Ready()
    {
        base._Ready();

        if (string.IsNullOrWhiteSpace(SettingPath))
        {
            GD.PushError($"{Name}: SettingPath is empty.");
            return;
        }

        if (!ResolveSettingPath())
        {
            GD.PushError($"{Name}: Failed to resolve setting path: {SettingPath}");
            return;
        }

        InitSetting();
    }

    private bool ResolveSettingPath()
    {
        var configRoot = SettingsManager.Instance.SettingsConfig;
        var settingsRoot = SettingsManager.Instance.Settings;

        string[] parts = SettingPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            GD.PushError("SettingPath must be in format 'Group.Property'");
            return false;
        }

        string group = parts[0];
        string property = parts[1];

        // Get config group (Example: SettingsConfig.Audio)
        object configGroup = configRoot.GetType().GetProperty(group)?.GetValue(configRoot);
        object settingsGroup = settingsRoot.GetType().GetProperty(group)?.GetValue(settingsRoot);

        if (configGroup == null || settingsGroup == null)
            return false;

        // Get config property (Example: MasterVolume.Min / Max)
        PropertyInfo configProp = configGroup.GetType().GetProperty(property);
        if (configProp == null)
            return false;

        dynamic configValue = configProp.GetValue(configGroup);
        MinValue = configValue.Min;
        MaxValue = configValue.Max;

        // Get Setting<float> (Example: Settings.Audio.MasterVolume)
        PropertyInfo settingProp = settingsGroup.GetType().GetProperty(property);
        if (settingProp == null)
            return false;

        Setting = settingProp.GetValue(settingsGroup) as Setting<float>;

        var config = configProp.GetValue(configGroup) as SettingsConfig;
        InitConfig(config);

        return Setting != null;
    }

    private void InitSetting()
    {
        Setting.Changed += OnSettingValueChanged;

        Slider.MinValue = MinValue;
        Slider.MaxValue = MaxValue;
        Slider.Value = GetValue();
        Slider.Step = 0.01 * SliderRange;

        Slider.ValueChanged += OnValueChanged;

        _repeatTimer = new Timer
        {
            OneShot = false,
            WaitTime = RepeatInterval,
            Autostart = false,
        };
        AddChild(_repeatTimer);

        _repeatTimer.Timeout += OnRepeatTick;

        IncreaseButton.ButtonDown += () => OnButtonStart(+Slider.Step);
        DecreaseButton.ButtonDown += () => OnButtonStart(-Slider.Step);

        IncreaseButton.ButtonUp += OnButtonStop;
        DecreaseButton.ButtonUp += OnButtonStop;
    }

    public float GetValue()
    {
        double value = Setting.Value;
        return (float)(InvertSlider ? Invert(value) : value);
    }

    public void OnValueChanged(double value)
    {
        double actualValue = InvertSlider ? Invert(value) : value;
        Setting.Pending = (float)actualValue;

        double percent = (Slider.Value - Slider.MinValue) / SliderRange * 100.0;
        PercentageLabel.Text = $"{Mathf.RoundToInt((float)percent)}%";
    }

    private async void OnButtonStart(double delta)
    {
        _currentDelta = delta;

        ModifyValue(delta);

        await ToSignal(GetTree().CreateTimer(InitialDelay), "timeout");

        if (IncreaseButton.ButtonPressed || DecreaseButton.ButtonPressed)
            _repeatTimer.Start();
    }

    private void OnButtonStop() => _repeatTimer.Stop();

    private void OnRepeatTick() => ModifyValue(_currentDelta);

    public void ModifyValue(double delta)
    {
        if (delta == 0.0) return;

        Slider.Value = Mathf.Clamp(
            (float)(Slider.Value + delta),
            Slider.MinValue,
            Slider.MaxValue
        );
    }

    private double Invert(double value) => Slider.MinValue + Slider.MaxValue - value;

    public void OnSettingValueChanged(float value)
    {
        Slider.Value = InvertSlider ? Invert(value) : value;
    }
}
