using Godot;

public partial class GameSession : Node
{
    public override void _Ready()
    {
        NetworkSession.Instance.OnSessionStarted += OnSessionStarted;
        NetworkSession.Instance.OnConnectedToServer += OnConnectedToServer;
    }

    private void OnSessionStarted()
    {
        MatchState.Instance.StartPhase(MatchPhase.WARMUP);
        PlayerState playerState = new();
        PlayerManager.Instance.RegisterPlayer(playerState);
        LoadGame(NetworkSession.Instance.ServerInfo.MapID);
    }

    private void OnConnectedToServer()
    {

    }

    public void StartSinglePlayer(string mapId)
    {
        LoadGame(mapId);
    }

    private void LoadGame(string mapID)
    {
        PackedScene levelToLoad = GameData.Instance.MapsByID[mapID].Scene;
        if(levelToLoad == null)
        {
            GD.PushError($"Attempted to load map ID: {mapID}. No corresponding scene was found");
        }
        GetTree().ChangeSceneToPacked(levelToLoad);
    }
}