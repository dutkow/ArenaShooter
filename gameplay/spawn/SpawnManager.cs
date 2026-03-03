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

    public ArenaCharacter ServerSpawnPlayer(byte playerID)
    {
        var spawnPoint = GetSpawnPoint();
        LevelUI.Instance.ShowPlayerHud();

        ArenaCharacter spawnedPlayer = LocalSpawnPlayer(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);
        PlayerSpawned.Send(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        return spawnedPlayer;
    }

    public ArenaCharacter LocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        var spawnedPlayer = (ArenaCharacter)GameMode.Instance.DefaultPawnScene.Instantiate();
        AddChild(spawnedPlayer);

        spawnedPlayer.GlobalPosition = spawnPosition;
        spawnedPlayer.GlobalRotation = new Vector3(0.0f, yRotation, 0.0f);

        LevelUI.Instance.ShowPlayerHud();

        if(playerID == NetworkSession.Instance.LocalPlayerID)
        {
            GameMode.Instance.LocalPlayerController.Possess(spawnedPlayer);
        }

        return spawnedPlayer;
    }
}
