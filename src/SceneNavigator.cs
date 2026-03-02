using Godot;

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
        OpenMultiplayerMap(serverInfo.MapID);
    }

    private void OnConnectedToServer()
    {
        OpenMultiplayerMap(NetworkSession.Instance.ServerInfo.MapID);
    }

    public void OpenMultiplayerMap(string mapID)
    {
        PackedScene levelToLoad = GameData.Instance.MultiplayerMapsByID[mapID].Scene;
        if(levelToLoad == null)
        {
            GD.PushError($"Attempted to load map ID: {mapID}. No corresponding scene was found");
        }
        GetTree().ChangeSceneToPacked(levelToLoad);
    }
}