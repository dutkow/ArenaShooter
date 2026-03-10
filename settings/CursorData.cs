using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class CursorData
{
    [JsonPropertyName("mode")]
    public string CursorModeString { get; set; }

    public CursorMode CursorMode
    {
        get
        {
            if (Enum.TryParse<CursorMode>(CursorModeString, ignoreCase: true, out var mode))
            {
                return mode;
            }
            return CursorMode.DEFAULT;
        }
    }

    [JsonPropertyName("texture")]
    public string TexturePath { get; set; }

    [JsonPropertyName("hotspot")]
    public float[] HotspotArray { get; set; }

    [JsonIgnore]
    public Vector2 Hotspot
    {
        get
        {
            if (HotspotArray != null && HotspotArray.Length >= 2)
            {
                return new Vector2(HotspotArray[0], HotspotArray[1]);
            }
            return Vector2.Zero;
        }
    }
}
