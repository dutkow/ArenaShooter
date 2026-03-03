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

        OpenMultiplayerMap(serverInfo.MapID);
    }

    private void OnConnectedToServer()
    {
        OpenMultiplayerMap(NetworkSession.Instance.ServerInfo.MapID);
    }

    // Added optional delay in seconds
    public async void OpenMultiplayerMap(string mapID, float delayBeforeLoad = 0.5f)
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

        GD.Print("Multiplayer map fully loaded!");

        if(NetworkSession.Instance.IsServer)
        {
            MatchState.Instance.Initialize();
        }
        else if (NetworkSession.Instance.IsClient)
        {
            ClientLoaded.Send();
        }
    }
}