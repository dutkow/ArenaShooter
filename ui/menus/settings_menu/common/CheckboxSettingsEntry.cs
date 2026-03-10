using Godot;
using System;

public partial class CheckboxSettingsEntry : SettingsEntry
{
    [Export] Button CheckBox;

    private bool _checked;

    public Setting<bool> Setting;

    public void Init(Setting<bool> setting)
    {
        Setting = setting;

        _checked = setting.Value;

        CheckBox.Pressed += OnPressed;
    }

    public void OnPressed()
    {
        _checked = !_checked;

        if(_checked)
        {
            CheckBox.Text = "[X]";
        }
        else
        {
            CheckBox.Text = "[ ]";
        }

        Setting.Pending = _checked;
    }
}
