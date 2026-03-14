using Godot;
using System;
using System.Threading.Tasks;

public class SceneNavigator
{
    // This class only holds logic and data — no Godot-specific functions.
    // Main.Instance will be used to actually manipulate the scene tree.

    public SceneNavigator()
    {
        // Subscribe to network events (logic only)
        NetworkManager.Instance.OnSessionStarted += OnSessionStarted;
        NetworkManager.Instance.OnConnectedToServer += OnConnectedToServer;
    }

    private void OnSessionStarted(ServerInfo serverInfo)
    {
        OpenMultiplayerMap(serverInfo.MapID);
    }

    private void OnConnectedToServer()
    {
        OpenMultiplayerMap(NetworkManager.Instance.ServerInfo.MapID);
    }

    // Delay optional, purely logic
    public async void OpenMultiplayerMap(string mapID, float delayBeforeLoad = 0.5f)
    {
        GD.Print($"open multiplayer map ran on {NetworkManager.Instance.NetworkMode}");
        if (!GameData.Instance.MultiplayerMapsByID.TryGetValue(mapID, out var mapInfo))
        {
            Console.WriteLine($"[SceneNavigator] Map ID {mapID} not found");
            return;
        }

        var mapScenePath = mapInfo.Scene.ResourcePath;

        // Use Main.Instance to manipulate Godot-specific things
        Main.Instance.OpenLoadingScreen();

        if (delayBeforeLoad > 0f)
        {
            // Pure C# delay instead of ToSignal
            await Task.Delay((int)(delayBeforeLoad * 1000));
        }

        var packedScene = (Godot.PackedScene)Godot.ResourceLoader.Load(mapScenePath);
        if (packedScene == null)
        {
            Console.WriteLine($"[SceneNavigator] Failed to load scene {mapScenePath}");
            return;
        }

        var newScene = packedScene.Instantiate<Godot.Node>();

        // Swap scenes via Main
        Main.Instance.SetMainScene(newScene);
        GD.Print($"set main scene ran on {NetworkManager.Instance.NetworkMode}");

        Console.WriteLine("[SceneNavigator] Multiplayer map fully loaded!");
    }
}