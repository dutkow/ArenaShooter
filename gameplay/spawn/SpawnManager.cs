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


    public void SpawnPlayer(PlayerState playerState)
    {
        var playerCharacter = (PlayerCharacter)GameMode.Instance.PlayerCharacterScene.Instantiate();
        AddChild(playerCharacter);

        //var spawnPoint = GetSpawnPoint();
        //playerCharacter.GlobalPosition = spawnPoint.GlobalPosition;
        //playerCharacter.GlobalRotation = spawnPoint.SpawnRotation;

        playerCharacter.GlobalPosition = Vector3.Zero;
        playerCharacter.GlobalRotation = Vector3.Zero;

        LevelUI.Instance.ShowPlayerHud();
    }
}
