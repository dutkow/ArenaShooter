using Godot;
using System;

public partial class LockCursorSettingsEntry : CheckboxSettingsEntry
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Controls.LockCursorToWindow);
        Init(SettingsManager.Instance.Settings.Controls.LockCursorToWindow);
    }
}
