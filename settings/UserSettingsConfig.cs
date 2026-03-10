using System.Text.Json.Serialization;

// Root class: only the actual user values
public class UserSettingsConfig
{
    public UserVideoSettings Video { get; set; } = new();
    public UserGameOptions Game { get; set; } = new();
    public UserControlsSettings Controls { get; set; } = new();
    public UserInterfaceSettings Interface { get; set; } = new();
    public UserAudioSettings Audio { get; set; } = new();


    public static UserSettingsConfig FromGameSettingsConfig(GameSettingsConfig config)
    {
        var settings = new UserSettingsConfig();

        settings.Video.DisplayMode = config.Video.DisplayMode.Value.ToLower();
        settings.Video.Resolution = config.Video.Resolution.Value;
        settings.Video.VSync = config.Video.VSync.Value;
        settings.Video.FrameLimit = config.Video.FrameLimit.Value;

        settings.Game.Language = config.Game.Language.Value;

        settings.Controls.PanSpeed = config.Controls.PanSpeed.Value;
        settings.Controls.DragPanSpeed = config.Controls.DragPanSpeed.Value;
        settings.Controls.EdgeScrollSpeed = config.Controls.EdgeScrollSpeed.Value;
        settings.Controls.PanSmoothing = config.Controls.PanSmoothing.Value;
        settings.Controls.ZoomSpeed = config.Controls.ZoomSpeed.Value;
        settings.Controls.ZoomSmoothing = config.Controls.ZoomSmoothing.Value;
        settings.Controls.CursorLock = config.Controls.CursorLock.Value.ToLower();

        settings.Interface.UIScale = config.Interface.UIScale.Value;

        settings.Audio.MasterVolume = config.Audio.MasterVolume.Value;
        settings.Audio.MusicVolume = config.Audio.MusicVolume.Value;
        settings.Audio.InterfaceVolume = config.Audio.InterfaceVolume.Value;
        settings.Audio.WorldVolume = config.Audio.WorldVolume.Value;

        return settings;
    }

    public void SaveToDisk()
    {
        string userJsonText = JsonUtils.ToJson(this);
        using var file = Godot.FileAccess.Open(FilePaths.USER_GAME_SETTINGS, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(userJsonText);
    }
}

// Video
public class UserVideoSettings
{
    public string DisplayMode { get; set; }
    public Resolution Resolution { get; set; }
    public bool VSync { get; set; }
    public int FrameLimit { get; set; }
}

// Game
public class UserGameOptions
{
    public string Language { get; set; }
}

// Controls
public class UserControlsSettings
{
    public float PanSpeed { get; set; }
    public float EdgeScrollSpeed { get; set; }
    public float DragPanSpeed { get; set; }
    public float PanSmoothing { get; set; }
    public float ZoomSpeed { get; set; }
    public float ZoomSmoothing { get; set; }
    public bool LockCursorToWindow { get; set; }
    public string CursorLock { get; set; }
}

// Interface
public class UserInterfaceSettings
{
    [JsonPropertyName("ui_scale")]
    public float UIScale { get; set; }
}

// Audio
public class UserAudioSettings
{
    public float MasterVolume { get; set; }
    public float MusicVolume { get; set; }
    public float InterfaceVolume { get; set; }
    public float WorldVolume { get; set; }
}
