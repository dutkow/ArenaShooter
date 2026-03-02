using Godot;
using System;
using System.Collections.Generic;

public partial class SpawnManager
{
    public static SpawnManager Instance;

    public List<SpawnPoint> _playerSpawnPoints = new();

    public SpawnManager()
    {
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

}
