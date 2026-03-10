using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;


public class RuntimeUserSettings
{
    public RuntimeVideoSettings Video { get; set; } = new();
    public RuntimeGameOptions Game { get; set; } = new();
    public RuntimeControlsSettings Controls { get; set; } = new();
    public RuntimeInterfaceSettings Interface { get; set; } = new();
    public RuntimeAudioSettings Audio { get; set; } = new();

    public static RuntimeUserSettings FromConfig(UserSettingsConfig disk)
    {
        var config = SettingsManager.Instance.SettingsConfig;
       
        return new RuntimeUserSettings
        {
            Video = new RuntimeVideoSettings
            {
                DisplayMode = Setting<DisplayMode>.FromConfig<DisplayMode>(config.Video.DisplayMode),
                Resolution = Setting<Resolution>.FromValue(disk.Video.Resolution, SettingApplyMode.IMMEDIATE),
                VSync = Setting<bool>.FromValue(disk.Video.VSync),
                FrameLimit = Setting<int>.FromValue(disk.Video.FrameLimit),
            },
            Game = new RuntimeGameOptions
            {
                Language = Setting<string>.FromValue(disk.Game.Language)
            },
            Controls = new RuntimeControlsSettings
            {
                PanSpeed = Setting<float>.Create(disk.Controls.PanSpeed, config.Controls.PanSpeed),
                EdgeScrollSpeed = Setting<float>.Create(disk.Controls.EdgeScrollSpeed, config.Controls.EdgeScrollSpeed),
                DragPanSpeed = Setting<float>.Create(disk.Controls.DragPanSpeed, config.Controls.DragPanSpeed),
                PanSmoothing = Setting<float>.Create(disk.Controls.PanSmoothing, config.Controls.PanSmoothing),
                ZoomSpeed = Setting<float>.Create(disk.Controls.ZoomSpeed, config.Controls.ZoomSpeed),
                ZoomSmoothing = Setting<float>.Create(disk.Controls.ZoomSmoothing, config.Controls.ZoomSmoothing),
                LockCursorToWindow = Setting<bool>.Create(disk.Controls.LockCursorToWindow, config.Controls.LockCursorToWindow),
                CursorLock = Setting<CursorLockMode>.FromValue(TextUtils.EnumFromString<CursorLockMode>(disk.Controls.CursorLock.ToUpper()), SettingApplyMode.IMMEDIATE),
            },
            Interface = new RuntimeInterfaceSettings
            {
                UIScale = Setting<float>.FromValue(disk.Interface.UIScale)
            },
            Audio = new RuntimeAudioSettings
            {
                MasterVolume = Setting<float>.FromValue(disk.Audio.MasterVolume),
                MusicVolume = Setting<float>.FromValue(disk.Audio.MusicVolume),
                InterfaceVolume = Setting<float>.FromValue(disk.Audio.InterfaceVolume),
                WorldVolume = Setting<float>.FromValue(disk.Audio.WorldVolume),
            }
        };
    }

    public UserSettingsConfig ToConfig()
    {
        return new UserSettingsConfig
        {
            Video = new UserVideoSettings
            {
                DisplayMode = Video.DisplayMode.Value.ToString().ToLower(),
                Resolution = Video.Resolution.Value,
                VSync = Video.VSync.Value,
                FrameLimit = Video.FrameLimit.Value,
            },
            Game = new UserGameOptions
            {
                Language = Game.Language.Value
            },
            Controls = new UserControlsSettings
            {
                PanSpeed = Controls.PanSpeed.Value,
                EdgeScrollSpeed = Controls.EdgeScrollSpeed.Value,
                DragPanSpeed = Controls.DragPanSpeed.Value,
                PanSmoothing = Controls.PanSmoothing.Value,
                ZoomSpeed = Controls.ZoomSpeed.Value,
                ZoomSmoothing = Controls.ZoomSmoothing.Value,
                LockCursorToWindow = Controls.LockCursorToWindow.Value,
                CursorLock = Controls.CursorLock.Value.ToString().ToLower(),
            },
            Interface = new UserInterfaceSettings
            {
                UIScale = Interface.UIScale.Value
            },
            Audio = new UserAudioSettings
            {
                MasterVolume = Audio.MasterVolume.Value,
                MusicVolume = Audio.MusicVolume.Value,
                InterfaceVolume = Audio.InterfaceVolume.Value,
                WorldVolume = Audio.WorldVolume.Value,
            }
        };
    }

    public void ApplyAll()
    {
        Apply(Video);
        Apply(Game);
        Apply(Controls);
        Apply(Interface);
        Apply(Audio);

        Save();
    }

    public void RevertAll()
    {
        Revert(Video);
        Revert(Game);
        Revert(Controls);
        Revert(Interface);
        Revert(Audio);
    }

    private static void Apply(object obj)
    {
        foreach (var prop in obj.GetType().GetProperties())
        {
            if (prop.GetValue(obj) is ISetting s)
                s.Apply();
        }
    }

    private static void Revert(object obj)
    {
        foreach (var prop in obj.GetType().GetProperties())
        {
            if (prop.GetValue(obj) is ISetting s)
                s.Revert();
        }
    }

    private void Save()
    {
        var config = ToConfig();

        string jsonString = System.Text.Json.JsonSerializer.Serialize(config, JsonUtils.SnakeCaseOptions);

        using var file = FileAccess.Open(FilePaths.USER_GAME_SETTINGS, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to save settings file at: {FilePaths.USER_GAME_SETTINGS}");
            return;
        }

        file.StoreString(jsonString);
        file.Flush();
    }
}

public class RuntimeVideoSettings
{
    public Setting<DisplayMode> DisplayMode { get; set; }
    public Setting<Resolution> Resolution { get; set; }
    public Setting<bool> VSync { get; set; }
    public Setting<int> FrameLimit { get; set; }
}

public class RuntimeGameOptions
{
    public Setting<string> Language { get; set; }
}

public class RuntimeControlsSettings
{
    public Setting<float> PanSpeed { get; set; }
    public Setting<float> EdgeScrollSpeed { get; set; }
    public Setting<float> DragPanSpeed { get; set; }
    public Setting<float> PanSmoothing { get; set; }
    public Setting<float> ZoomSpeed { get; set; }
    public Setting<float> ZoomSmoothing { get; set; }
    public Setting<bool> LockCursorToWindow { get; set; }

    public Setting<CursorLockMode> CursorLock { get; set; }
}

public class RuntimeInterfaceSettings
{
    public Setting<float> UIScale { get; set; }
}

public class RuntimeAudioSettings
{
    public Setting<float> MasterVolume { get; set; }
    public Setting<float> MusicVolume { get; set; }
    public Setting<float> InterfaceVolume { get; set; }
    public Setting<float> WorldVolume { get; set; }
}
