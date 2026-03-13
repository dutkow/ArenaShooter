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

    public Character ServerSpawnPlayer(byte playerID)
    {
        var spawnPoint = GetSpawnPoint();
        if(MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var player))
        {
            player.IsAlive = true;
            GD.Print($"setting player is alive to true for player id {playerID}");
            player.Flags |= PlayerStateFlags.IS_ALIVE_CHANGED;
        }
        Character spawnedPlayer = LocalSpawnPlayer(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        return spawnedPlayer;
    }


    public Character LocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        GD.Print($"new local spawn player ran on {NetworkSession.Instance.NetworkMode}. NEW PLAYER ID: {playerID} and local player id {ClientGame.Instance.LocalPlayerID}, spawn position: {spawnPosition}");
        var spawnedPlayer = (Character)GameMode.Instance.DefaultPawnScene.Instantiate();

        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.Character = spawnedPlayer;
        }

        AddChild(spawnedPlayer);

        spawnedPlayer.HandleSpawn(spawnPosition, yRotation, 0.0f);

        spawnedPlayer.SetIsAuthority(NetworkSession.Instance.IsServer);

        if (playerID == ClientGame.Instance.LocalPlayerID)
        {
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
}
