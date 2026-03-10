using Godot;
using System;

public partial class SettingsEntry : Control
{
    [Export] public Label SettingNameLabel;

    public virtual void InitConfig(SettingsConfig config)
    {
        SettingNameLabel.Text = config.LocalizationKey;
    }
}
