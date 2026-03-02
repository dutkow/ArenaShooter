using Godot;
using System;
using System.Threading.Tasks;

public partial class SceneNavigator : Node
{
    public static SceneNavigator Instance { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        NetworkSession.Instance.OnSessionStarted += OnSessionStarted;
        NetworkSession.Instance.OnConnectedToServer += OnConnectedToServer;
    }

    private void OnSessionStarted(ServerInfo serverInfo)
    {
        MatchState.Instance.StartPhase(MatchPhase.WARMUP);
        PlayerState playerState = new(0);
        PlayerManager.Instance.RegisterPlayer(playerState);

        OpenMultiplayerMap(serverInfo.MapID, OnMapLoaded, 1.0f); // optional 1-second delay
    }

    private void OnConnectedToServer()
    {
        OpenMultiplayerMap(NetworkSession.Instance.ServerInfo.MapID, OnMapLoaded, 1.0f);
    }

    private void OnMapLoaded()
    {
        GD.Print("Multiplayer map fully loaded!");
        // Any additional post-load setup here
    }

    // Added optional delay in seconds
    public async void OpenMultiplayerMap(string mapID, Action onLoaded = null, float delayBeforeLoad = 0f)
    {
        if (!GameData.Instance.MultiplayerMapsByID.TryGetValue(mapID, out var mapInfo))
        {
            GD.PushError($"Attempted to load map ID: {mapID}. No corresponding scene was found");
            return;
        }

        var mapScenePath = mapInfo.Scene.ResourcePath;

        UIRoot.Instance.ShowLoadingScreen();

        if (delayBeforeLoad > 0f)
        {
            await ToSignal(GetTree().CreateTimer(delayBeforeLoad), "timeout");
        }

        ResourceLoader.LoadThreadedRequest(mapScenePath);

        var packedScene = (PackedScene)ResourceLoader.LoadThreadedGet(mapScenePath);
        if (packedScene == null)
        {
            GD.PushError($"Failed to load scene {mapScenePath}");
            UIRoot.Instance.HideLoadingScreen();
            return;
        }

        var newScene = packedScene.Instantiate<Node>();

        // Swap scenes
        var currentScene = GetTree().CurrentScene;
        GetTree().Root.AddChild(newScene);
        GetTree().CurrentScene = newScene;
        currentScene?.QueueFree();

        // Hide loading screen
        UIRoot.Instance.HideLoadingScreen();

        onLoaded?.Invoke();

        GD.Print("Level fully loaded");
    }
}