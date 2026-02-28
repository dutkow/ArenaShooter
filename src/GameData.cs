using Godot;
using System;
using System.Text.Json;
using static PascalToSnake;

public partial class GameData : Node
{
    public static GameData Instance { get; private set; }
    public MapCollection Maps { get; private set; }

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
            PropertyNamingPolicy = new SnakeToPascal(),
            AllowTrailingCommas = true,
        };

        Maps = JsonSerializer.Deserialize<MapCollection>(jsonText, options);
        Maps.Initialize();

        GD.Print($"Loaded {Maps.Maps.Count} maps.");
    }

    public MapInfo? GetMapByID(string id)
    {
        return Maps.Maps.Find(m => m.ID == id);
    }
}
