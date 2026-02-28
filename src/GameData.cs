using Godot;
using Godot.Collections;
using System;
using System.Text.Json;
using static PascalToSnake;

public partial class GameData : Node
{
    public static GameData Instance { get; private set; }
    public MapCollection MapCollection { get; private set; }

    public System.Collections.Generic.Dictionary<string, MapInfo> MapsByID = new();

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        LoadMaps();
    }

    private void LoadMaps()
    {
        var path = "res://scenes/maps/maps.json";

        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr("Map JSON not found!");
            return;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var jsonText = file.GetAsText();


        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new PascalToSnake(),
            AllowTrailingCommas = true,
        };

        MapCollection = JsonSerializer.Deserialize<MapCollection>(jsonText, options);
        MapCollection.Initialize();

        foreach(var map in MapCollection.Maps)
        {
            MapsByID[map.ID] = map;
        }

        GD.Print($"Loaded {MapCollection.Maps.Count} maps.");
    }

    public MapInfo? GetMapByID(string id)
    {
        return MapCollection.Maps.Find(m => m.ID == id);
    }
}
