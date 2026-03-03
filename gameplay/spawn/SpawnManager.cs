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


    public void SpawnPlayer(PlayerController playerController)
    {
        if(playerController == null)
        {
            GD.PushError("Attempted to spawn player but player controller is null");
            return;
        }

        var playerCharacter = (ArenaCharacter)GameMode.Instance.PlayerCharacterScene.Instantiate();
        AddChild(playerCharacter);

        var spawnPoint = GetSpawnPoint();
        playerCharacter.GlobalPosition = spawnPoint.GlobalPosition;
        playerCharacter.GlobalRotation = spawnPoint.SpawnRotation;

        playerController.Possess(playerCharacter);

        LevelUI.Instance.ShowPlayerHud();
    }
}
