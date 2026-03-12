using Godot;
using System;
using System.Collections.Generic;

public partial class SpawnManager : Node3D
{
    public static SpawnManager Instance;

    public List<SpawnPoint> _playerSpawnPoints = new();

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }

    public void RegisterSpawnPoint(SpawnPoint spawnPoint)
    {
        switch(spawnPoint.Type)
        {
            case SpawnPointType.PLAYER:
                _playerSpawnPoints.Add(spawnPoint);
                break;
        }
    }

    public SpawnPoint GetSpawnPoint()
    {
        return ListUtils.RandomElement(_playerSpawnPoints);
    }

    public Pawn ServerSpawnPlayer(byte playerID)
    {
        var spawnPoint = GetSpawnPoint();
        Pawn spawnedPlayer = LocalSpawnPlayer(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        PlayerSpawned.Send(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        return spawnedPlayer;
    }

    public Pawn LocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        GD.Print($"Spawning player locally. PlayerID = {playerID}. Position = {spawnPosition}. Y rotation = {yRotation}");
        var spawnedPlayer = (Pawn)GameMode.Instance.DefaultPawnScene.Instantiate();

        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.AssignPawn(spawnedPlayer);
        }

        AddChild(spawnedPlayer);

        if(spawnedPlayer is Character character)
        {
            character.HandleSpawn(spawnPosition, yRotation, 0.0f);
        }

        spawnedPlayer.SetIsAuthority(NetworkSession.Instance.IsServer);

        if(playerID == ClientGame.Instance.LocalPlayerID)
        {
            GD.Print($"running possess when spawning player on {NetworkSession.Instance.NetworkMode}. spawned player ID = {playerID} and local player ID = {NetworkSession.Instance.LocalPlayerID}");
            ClientGame.Instance.LocalPlayerController.Possess(spawnedPlayer);
        }
        else
        {
            GD.Print($"handling remote spawn on {NetworkSession.Instance.NetworkMode}");
            spawnedPlayer.HandleRemoteSpawn(playerID);
        }
        
        spawnedPlayer.Initialize(playerState);




        return spawnedPlayer;
    }

    // REFACTOR CODE
    public Pawn NewLocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        var spawnedPlayer = (Character)GameMode.Instance.DefaultPawnScene.Instantiate();

        if (MatchState.Instance.NewConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.PublicState.Character = spawnedPlayer;
        }

        AddChild(spawnedPlayer);

        spawnedPlayer.HandleSpawn(spawnPosition, yRotation, 0.0f);

        spawnedPlayer.SetIsAuthority(NetworkSession.Instance.IsServer);

        if (playerID == ClientGame.Instance.LocalPlayerID)
        {
            GD.Print($"running possess when spawning player on {NetworkSession.Instance.NetworkMode}. spawned player ID = {playerID} and local player ID = {NetworkSession.Instance.LocalPlayerID}");
            ClientGame.Instance.LocalPlayerController.Possess(spawnedPlayer);
        }
        else
        {
            GD.Print($"handling remote spawn on {NetworkSession.Instance.NetworkMode}");
            spawnedPlayer.HandleRemoteSpawn(playerID);
        }

        spawnedPlayer.InitializeNew(playerState);


        return spawnedPlayer;
    }
}
