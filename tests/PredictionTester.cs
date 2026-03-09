using Godot;
using System;
using System.ComponentModel;

public partial class PredictionTester : Node3D
{
    [Export] SpawnPoint _spawnPoint1;
    [Export] SpawnPoint _spawnPoint2;

    [Export] PackedScene _characterScene;

    private Character _character1;
    private Character _character2;

    public override void _EnterTree()
    {
        base._EnterTree();


        Initialize();
    }

    public void Initialize()
    {
        MatchState matchState = new();
        AddChild(matchState);
        matchState.Initialize();

        ServerTickManager serverTickManager = new();
        AddChild(serverTickManager);

        SpawnManager spawnManager = new();
        AddChild(spawnManager);

        ServerGame serverGame = new();

        ClientGame clientGame1 = new();
        ClientGame clientGame2 = new();

        NetworkSession networkSession1 = new();
        NetworkSession networkSession2 = new();

        networkSession1.SetMode(NetworkMode.LISTEN_SERVER);
        networkSession2.SetMode(NetworkMode.CLIENT);

        clientGame1.Initialize(0);
        clientGame2.Initialize(1);

        PlayerState playerState1 = new(0);
        PlayerState playerState2 = new(1);

        clientGame1.AssignPlayerState(playerState1);
        clientGame2.AssignPlayerState(playerState2);

        _character1 = (Character)_characterScene.Instantiate();
        AddChild(_character1);
        _character1.GlobalPosition = _spawnPoint1.GlobalPosition;

        _character2 = (Character)_characterScene.Instantiate();
        AddChild(_character2);
        _character2.GlobalPosition = _spawnPoint2.GlobalPosition;

        MatchState.Instance.ConnectedPlayers.Add(0, playerState1);
        MatchState.Instance.ConnectedPlayers.Add(1, playerState2);

        clientGame1.LocalPlayerController.Possess(_character1);
        clientGame2.LocalPlayerController.Possess(_character2);

        InputCommand testInput = new();
        testInput |= InputCommand.MOVE_FORWARD;

    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        GD.Print($"Character 1 position = {_character1.GlobalPosition}");
    }
}
