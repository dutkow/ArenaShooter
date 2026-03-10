using Godot;
using System;

public partial class ZoomSpeedSettingsEntry : SliderSettingsEntryOld
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Controls.ZoomSpeed);
        InitSetting(SettingsManager.Instance.Settings.Controls.ZoomSpeed);
    }

    public override float GetMinValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.ZoomSpeed.Min;
    }

    public override float GetMaxValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.ZoomSpeed.Max;
    }
}
