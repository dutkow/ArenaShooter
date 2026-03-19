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

        NetworkManager.Instance.JoinedServer += OnJoinedServer;
    }

    private void OnSessionStarted(ServerInfo serverInfo)
    {
        OpenMultiplayerMap(serverInfo.MapID);
    }

    private void OnConnectedToServer()
    {
        OpenMultiplayerMap(NetworkManager.Instance.ServerInfo.MapID);
    }

    private void OnJoinedServer(ServerInfo serverInfo)
    {
        OpenMultiplayerMap(serverInfo.MapID);
    }

    // Delay optional, purely logic
    public async void OpenMultiplayerMap(string mapID, float delayBeforeLoad = 0.5f)
    {
        GD.Print($"open mp map ran. {mapID}. network mode: {NetworkManager.Instance.NetworkMode}");
        if (!GameData.Instance.MultiplayerMapsByID.TryGetValue(mapID, out var mapInfo))
        {
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
            return;
        }

        var newScene = packedScene.Instantiate<Godot.Node>();

        Main.Instance.SetMainScene(newScene);
    }

    public static async void OpenMainMenu(float delayBeforeLoad = 0.5f)
    {

        Main.Instance.OpenLoadingScreen();

        if (delayBeforeLoad > 0f)
        {
            // Pure C# delay instead of ToSignal
            await Task.Delay((int)(delayBeforeLoad * 1000));
        }

        var packedScene = (Godot.PackedScene)Godot.ResourceLoader.Load("res://ui/menus/main_menu/main_menu.tscn");
        if (packedScene == null)
        {
            return;
        }

        var newScene = packedScene.Instantiate<Godot.Node>();

        Main.Instance.SetMainScene(newScene);
    }

    public static void QuitToDesktop()
    {
        Main.Instance.GetTree().Quit();
    }
}