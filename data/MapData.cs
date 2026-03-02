using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum GameModeType
{
    SLAYER,
    TEAM_SLAYER,
}

// Base class for any map/level
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

    // Base path is overridden in subclasses
    public virtual void Initialize(string basePath = "res://levels/")
    {
        var scenePath = $"{basePath}{Folder}/{SceneName}.tscn";
        Scene = GD.Load<PackedScene>(scenePath);

        if (Scene == null)
            GD.PushError($"Failed to load map/level scene: {scenePath}");
    }
}

// Multiplayer map info
[Serializable]
public class MultiplayerMapInfo : MapInfo
{
    public GameModeType[] AllowedModes { get; set; }
    public int MaxPlayers { get; set; }

    public override void Initialize(string basePath = "res://levels/multiplayer_maps/")
    {
        base.Initialize(basePath);
    }
}


[Serializable]
public class MultiplayerMapCollection
{
    public List<MultiplayerMapInfo> Maps { get; set; } = new List<MultiplayerMapInfo>();

    public void Initialize()
    {
        foreach (var map in Maps)
        {
            map.Initialize();
        }
    }
}