using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static PascalToSnake;

public partial class GameData : Node
{
    [Export] public Json _multiplayerMapsJson; // assign your maps.json in the editor
    public static GameData Instance { get; private set; }
    public MultiplayerMapCollection MultiplayerMaps { get; private set; }

    public Dictionary<string, MapInfo> MultiplayerMapsByID = new();

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        LoadMaps();
    }

    private void LoadMaps()
    {
        if (_multiplayerMapsJson == null)
        {
            GD.PrintErr("MapsJson not assigned!");
            return;
        }

        var jsonText = (string)_multiplayerMapsJson.GetData();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new PascalToSnake(),
            AllowTrailingCommas = true,
        };

        try
        {
            MultiplayerMaps = JsonSerializer.Deserialize<MultiplayerMapCollection>(jsonText, options);
            if (MultiplayerMaps == null)
            {
                GD.PrintErr("Failed to deserialize MapCollection!");
                return;
            }

            MultiplayerMaps.Initialize();

            // Populate dictionary for quick access
            foreach (var map in MultiplayerMaps.Maps)
            {
                MultiplayerMapsByID[map.ID] = map;
            }

            GD.Print($"Loaded {MultiplayerMaps.Maps.Count} maps.");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error parsing maps JSON: {e.Message}");
        }
    }

    public MapInfo? GetMapByID(string id)
    {
        return MultiplayerMapsByID.TryGetValue(id, out var map) ? map : null;
    }
}