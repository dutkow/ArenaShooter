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
        AddChild(spawnedPlayer);

        spawnedPlayer.GlobalPosition = spawnPosition;
        spawnedPlayer.GlobalRotation = new Vector3(0.0f, yRotation, 0.0f);

        if(MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.AssignPawn(spawnedPlayer);
        }
        else
        {
            GD.PushError($"Failed to assign character to player state because player state not found in connected players. PlayerID of character: {playerID}. Net role: {NetworkSession.Instance.NetworkMode}.");
        }

        spawnedPlayer.SetIsAuthority(NetworkSession.Instance.IsServer);

        if(playerID == NetworkSession.Instance.LocalPlayerID)
        {
            GD.Print($"running possess when spawning player on {NetworkSession.Instance.NetworkMode}. spawned player ID = {playerID} and local player ID = {NetworkSession.Instance.LocalPlayerID}");
            GameMode.Instance.LocalPlayerController.Possess(spawnedPlayer);
        }
        else
        {
            GD.Print($"handling remote spawn on {NetworkSession.Instance.NetworkMode}");
            spawnedPlayer.HandleRemoteSpawn();
        }
        
        spawnedPlayer.Initialize(playerState);

        return spawnedPlayer;
    }
}
