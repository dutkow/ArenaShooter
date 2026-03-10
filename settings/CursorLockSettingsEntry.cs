using Godot;
using System;

public enum CursorLockMode
{
    FULL_SCREEN_ONLY,
    WINDOWED_ONLY,
    BOTH,
    NONE,
}

public partial class CursorLockSettingsEntry
    : EnumDropdownSettingsEntry<CursorLockMode>
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Controls.CursorLock);
        Init(SettingsManager.Instance.Settings.Controls.CursorLock);

        CursorManager.Instance.CursorLockModeChanged += OnCursorLockModeChanged;
    }

    public void OnCursorLockModeChanged(CursorLockMode cursorLockMode)
    {
        var selectedIndex = Dropdown.Selected;
        var newModeIndex = (int)cursorLockMode;
        if (newModeIndex != selectedIndex)
        {
            Dropdown.Select(newModeIndex);
        }
    }
}