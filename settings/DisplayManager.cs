using Godot;
using System;
using System.Collections.Generic;
using static Godot.DisplayServer;

public enum DisplayMode
{
    FULLSCREEN,
    WINDOWED,
}

public partial class DisplayManager : Node
{
    public static DisplayManager Instance { get; private set; }

    public event Action<DisplayMode> DisplayModeChanged;

    public DisplayMode DisplayMode;

    private static readonly Dictionary<DisplayMode, WindowMode> _windowModesByDisplayMode = new()
    {
        { DisplayMode.FULLSCREEN, WindowMode.ExclusiveFullscreen },
        { DisplayMode.WINDOWED, WindowMode.Windowed },
    };

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        SettingsManager.Instance.Settings.Video.DisplayMode.Changed += SetDisplayMode;

        SetDisplayMode(SettingsManager.Instance.Settings.Video.DisplayMode.Value);
    }

    public void SetDisplayMode(DisplayMode mode)
    {
        if(_windowModesByDisplayMode.TryGetValue(mode, out var windowMode))
        {
            DisplayServer.WindowSetMode(windowMode);
            DisplayMode = mode;
            DisplayModeChanged?.Invoke(mode);
        }
    }

    public void SetResolution(Resolution resolution)
    {

    }
}
