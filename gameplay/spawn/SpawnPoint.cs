using Godot;
using System;
using System.Threading;


public enum SpawnPointType
{
    PLAYER,
    ITEM,
}
public partial class SpawnPoint : Node3D
{
    [Export] Node3D _visualContainer;

    public Vector3 SpawnRotation => new Vector3(0.0f, GlobalRotation.Y, 0.0f);

    public SpawnPointType Type;
    public override void _Ready()
    {
        base._Ready();

        _visualContainer.Visible = false;

        SpawnManager.Instance.RegisterSpawnPoint(this);
    }
}
