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


    public ArenaCharacter SpawnPlayer(byte playerID)
    {
        var spawnedPlayer = (ArenaCharacter)GameMode.Instance.DefaultPawnScene.Instantiate();
        AddChild(spawnedPlayer);

        var spawnPoint = GetSpawnPoint();
        spawnedPlayer.GlobalPosition = spawnPoint.GlobalPosition;
        spawnedPlayer.GlobalRotation = spawnPoint.SpawnRotation;


        LevelUI.Instance.ShowPlayerHud();

        if(NetworkSession.Instance.IsServer)
        {
            PlayerSpawned.Send(playerID, spawnedPlayer.GlobalPosition, spawnedPlayer.GlobalRotation.Y);
        }

        return spawnedPlayer;
    }
}
