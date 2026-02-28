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
            //map.ParseGameModes();
        }
    }
}

[Serializable]
public class MapInfo
{
    [JsonPropertyName("ID")]
    public string ID { get; set; }

    public string Name { get; set; }

}
