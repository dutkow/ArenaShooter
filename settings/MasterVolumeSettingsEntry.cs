using Godot;
using System;

public partial class MasterVolumeSettingsEntry : SliderSettingsEntryOld
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Audio.MasterVolume);
        InitSetting(SettingsManager.Instance.Settings.Audio.MasterVolume);
    }

    public override float GetMinValue()
    {
        return SettingsManager.Instance.SettingsConfig.Audio.MasterVolume.Min;
    }

    public override float GetMaxValue()
    {
        return SettingsManager.Instance.SettingsConfig.Audio.MasterVolume.Max;
    }
}
