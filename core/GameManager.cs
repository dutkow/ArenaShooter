using Godot;
using System;

public partial class GameManager : Node
{
    public static GameManager Instance;
    private SpawnManager _spawnManager;

    [Export] PackedScene _playerScene;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _spawnManager = new();

        //CallDeferred(nameof(SpawnPlayer));
    }

    public void SpawnPlayer()
    {
        ArenaCharacterOld spawnedPlayer = (ArenaCharacterOld)_playerScene.Instantiate();
        var spawnPoint = _spawnManager.GetSpawnPoint();

        GetTree().CurrentScene.AddChild(spawnedPlayer);

        spawnedPlayer.GlobalPosition = spawnPoint.GlobalPosition;
        spawnedPlayer.GlobalRotation = spawnPoint.GlobalRotation;

    }
}
