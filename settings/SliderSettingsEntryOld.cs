using Godot;
using System;

public partial class SliderSettingsEntryOld : SettingsEntry
{
    [Export] public Slider Slider;
    [Export] public Label PercentageLabel;

    [Export] public Button IncreaseButton;
    [Export] public Button DecreaseButton;

    [Export] public float InitialDelay = 0.25f;
    [Export] public float RepeatInterval = 0.05f;

    [Export] public bool InvertSlider = false;

    private Timer _repeatTimer;
    private double _currentDelta;

    public double SliderRange => Slider.MaxValue - Slider.MinValue;

    public Setting<float> Setting;

    public void InitSetting(Setting<float> setting)
    {
        Setting = setting;

        setting.Changed += OnSettingValueChanged;

        Slider.ValueChanged += OnValueChanged;

        Slider.MinValue = GetMinValue();
        Slider.MaxValue = GetMaxValue();
        Slider.Value = GetValue();

        Slider.Step = 0.01 * SliderRange;

        Slider.Value = Slider.Value;

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

    public virtual float GetValue()
    {
        double value = Setting.Value;
        double finalValue = InvertSlider ? Invert(value) : value;
        return (float)finalValue;
    }

    public virtual float GetMinValue() { return 0.0f; }
    public virtual float GetMaxValue() { return 1.0f; }

    public virtual void OnValueChanged(double value)
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
        {
            _repeatTimer.Start();
        }
    }

    private void OnButtonStop()
    {
        _repeatTimer.Stop();
    }

    private void OnRepeatTick()
    {
        ModifyValue(_currentDelta);
    }

    public void ModifyValue(double delta)
    {
        if (delta == 0.0)
            return;

        double previousValue = Slider.Value;
        Slider.Value = Mathf.Clamp((float)(previousValue + delta), Slider.MinValue, Slider.MaxValue);
    }

    private double Invert(double value)
    {
        return Slider.MinValue + Slider.MaxValue - value;
    }

    public void OnSettingValueChanged(float value)
    {
        Slider.Value = InvertSlider ? Invert(value) : value;
    }
}
