using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum GameModeType
{
    SLAYER,
    TEAM_SLAYER,
}

[Serializable]
public class MapCollection
{
    public List<MapInfo> Maps { get; set; } = new List<MapInfo>();

    public void Initialize()
    {
        foreach (var map in Maps)
        {
            map.Initialize();
        }
    }
}

[Serializable]
public class MapInfo
{
    [JsonPropertyName("id")]
    public string ID { get; set; }

    public string Name { get; set; }
    public string Folder { get; set; }

    [JsonPropertyName("scene")]
    public string SceneName { get; set; }

    [JsonIgnore]
    public PackedScene Scene { get; set; }

    public void Initialize()
    {
        var scenePath = $"res://scenes/maps/{Folder}/{SceneName}.tscn";

        Scene = GD.Load<PackedScene>(scenePath);

        if (Scene == null)
        {
            GD.PushError($"Failed to load map scene: {scenePath}");
        }
    }
}
