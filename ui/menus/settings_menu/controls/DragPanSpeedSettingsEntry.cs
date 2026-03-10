using Godot;
using System;
public partial class DragPanSpeedSettingsEntry : SliderSettingsEntryOld
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Controls.DragPanSpeed);
        InitSetting(SettingsManager.Instance.Settings.Controls.DragPanSpeed);
    }

    public override float GetMinValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.DragPanSpeed.Min;
    }

    public override float GetMaxValue()
    {
        return SettingsManager.Instance.SettingsConfig.Controls.DragPanSpeed.Max;
    }
}