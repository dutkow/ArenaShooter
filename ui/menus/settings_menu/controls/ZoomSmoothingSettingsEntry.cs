using Godot;
using System;

public partial class ZoomSmoothingSettingsEntry : SliderSettingsEntryOld
{
    public override void _Ready()
    {
        base._Ready();

        InvertSlider = true;

        InitConfig(SettingsManager.Instance.SettingsConfig.Controls.ZoomSmoothing);
        InitSetting(SettingsManager.Instance.Settings.Controls.ZoomSmoothing);
    }

    public override float GetMinValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.ZoomSmoothing.Min;
    }

    public override float GetMaxValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.ZoomSmoothing.Max;
    }
}