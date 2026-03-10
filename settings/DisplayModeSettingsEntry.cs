using Godot;
using System;
using System.Collections.Generic;
using static Godot.DisplayServer;



public partial class DisplayModeSettingsEntry
    : EnumDropdownSettingsEntry<DisplayMode>
{

    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Video.DisplayMode);
        Init(SettingsManager.Instance.Settings.Video.DisplayMode);

        DisplayManager.Instance.DisplayModeChanged += OnDisplayModeChanged;
    }


    public void OnDisplayModeChanged(DisplayMode displayMode)
    {
        var selectedIndex = Dropdown.Selected;
        var newModeIndex = (int)displayMode;
        if (newModeIndex != selectedIndex)
        {
            Dropdown.Select(newModeIndex);
        }
    }
}
