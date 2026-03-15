using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class SpawnManager : Node3D
{
    public static SpawnManager Instance;

    public List<SpawnPoint> _playerSpawnPoints = new();

    public static void Initialize()
    {
        Instance = new SpawnManager();
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


        }
        Character spawnedPlayer = LocalSpawnPlayer(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        player.Flags |= PlayerStateFlags.IS_ALIVE_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.ROTATION_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.VELOCITY_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED;

        player.CharacterPublicState.Position = spawnPoint.GlobalPosition;
        player.CharacterPublicState.Look = new Vector2(-spawnPoint.GlobalRotation.Y, 0.0f);

        return spawnedPlayer;
    }


    public Character LocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        GD.Print($"new local spawn player ran on {NetworkManager.Instance.NetworkMode}. NEW PLAYER ID: {playerID} and local player id {NetworkClient.Instance.LocalPlayerID}, spawn position: {spawnPosition}");
        var spawnedPlayer = (Character)GameMode.Instance.DefaultPawnScene.Instantiate();

        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.Character = spawnedPlayer;
        }

        Level.Instance.AddChild(spawnedPlayer);

        spawnedPlayer.HandleSpawn(spawnPosition, yRotation, 0.0f);
        spawnedPlayer.SetIsAuthority(NetworkManager.Instance.IsServer);

        if (playerID == NetworkClient.Instance.LocalPlayerID)
        {
            ClientGame.Instance.LocalPlayerController.Possess(spawnedPlayer);
            GD.Print($"running POSSESS on player id: {playerID} on {NetworkManager.Instance.NetworkMode}");
        }
        else
        {
            GD.Print($"handling remote spawn on {NetworkManager.Instance.NetworkMode}");
            spawnedPlayer.HandleRemoteSpawn(playerID);
        }

        spawnedPlayer.Initialize(playerState);


        return spawnedPlayer;
    }
}
