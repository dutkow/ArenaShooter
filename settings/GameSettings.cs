using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Root class: configuration including defaults, min/max, options
public class GameSettingsConfig
{
    public VideoSettingsConfig Video { get; set; }
    public GameOptionsConfig Game { get; set; }
    public ControlsSettingsConfig Controls { get; set; }
    public InterfaceSettingsConfig Interface { get; set; }
    public AudioSettingsConfig Audio { get; set; }

}

public abstract class SettingsConfig
{
    [JsonIgnore]
    public string LocalizationKey { get; set; }

    public string ApplyMode { get; set; }

    [JsonIgnore]
    public SettingApplyMode SettingApplyMode => TextUtils.EnumFromString<SettingApplyMode>(ApplyMode);
}

// Video
public class VideoSettingsConfig
{
    public EnumSettingConfig DisplayMode { get; set; }

    public ResolutionSettingConfig Resolution { get; set; }

    [JsonPropertyName("vsync")]
    public BoolSettingConfig VSync { get; set; }
    public IntSettingConfig FrameLimit { get; set; }
}

public class EnumSettingConfig : SettingsConfig
{
    public string Value { get; set; }
    public List<string> Options { get; set; }
}

public class ResolutionSettingConfig : SettingsConfig
{
    public Resolution Value { get; set; }
    public List<Resolution> Options { get; set; }
}

public class Resolution
{
    public int Width { get; set; }
    public int Height { get; set; }

    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static Resolution FromVector2I(Vector2I vec)
    {
        return new Resolution(vec.X, vec.Y);
    }
}

public class SerializableVector2
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class SerializableVector2I
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class BoolSettingConfig : SettingsConfig
{
    public bool Value { get; set; }
}

public class IntSettingConfig : SettingsConfig
{
    public int Value { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}

// Game
public class GameOptionsConfig
{
    public EnumSettingConfig Language { get; set; }
}

// Controls
public class ControlsSettingsConfig
{
    public FloatSettingConfig PanSpeed { get; set; }
    public FloatSettingConfig EdgeScrollSpeed { get; set; }
    public FloatSettingConfig DragPanSpeed { get; set; }
    public FloatSettingConfig PanSmoothing { get; set; }
    public FloatSettingConfig ZoomSpeed { get; set; }
    public FloatSettingConfig ZoomSmoothing { get; set; }
    public BoolSettingConfig LockCursorToWindow { get; set; }
    public EnumSettingConfig CursorLock { get; set; }
}

public class FloatSettingConfig : SettingsConfig
{
    public float Value { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }

    [JsonIgnore]
    public float Range => Max - Min;

    [JsonIgnore]
    public float IsEqualTolerance => Range * 0.005f;
}

public class DropdownSettingConfig : SettingsConfig
{
    public float Selected { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
}


// Interface
public class InterfaceSettingsConfig
{
    [JsonPropertyName("ui_scale")]
    public FloatSettingConfig UIScale { get; set; }
}

// Audio
public class AudioSettingsConfig
{
    public FloatSettingConfig MasterVolume { get; set; }
    public FloatSettingConfig MusicVolume { get; set; }
    public FloatSettingConfig InterfaceVolume { get; set; }
    public FloatSettingConfig WorldVolume { get; set; }
}
