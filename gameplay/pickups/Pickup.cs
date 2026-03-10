using Godot;
using System;

public partial class Pickup : Node3D
{
    [Export] Area3D _area;
    [Export] CollisionShape3D _collisionShape;


    [Export] float rotationSpeed = 3.0f;

    [Export] float pulseSpeed = 5.0f;       // How fast it bobs up and down
    [Export] float pulseMagnitude = 0.2f;  // How high/low it moves

    private float _accumulatedTime = 0.0f;

    private float _respawnTime = 5.0f;

    private float _respawnAccumulatedTime = 0.0f;

    [Export] MeshInstance3D _mesh;
    private Vector3 _baseMeshPosition;

    [Export] private bool _startSpawned = true;
    public bool _isSpawned = true;


    public override void _Ready()
    {
        base._Ready();

        if(!_startSpawned)
        {
            _isSpawned = false;
            _mesh.Visible = false;
        }

        _baseMeshPosition = _mesh.Position;

    }
    public override void _Process(double delta)
    {
        base._Process(delta);

        Tick((float)delta);

        foreach(var kvp in MatchState.Instance.ConnectedPlayers)
        {

        }
    }

    public void Tick(float delta)
    {
        if(_isSpawned)
        {
            _mesh.Rotation = new Vector3(0.0f, _mesh.Rotation.Y + rotationSpeed * delta, 0.0f);

            _accumulatedTime += delta;
            float yOffset = (float)Math.Sin(_accumulatedTime * pulseSpeed) * pulseMagnitude;
            _mesh.Position = _baseMeshPosition + new Vector3(0, yOffset, 0);
        }
        else
        {
            _respawnAccumulatedTime += delta;
            if(_respawnAccumulatedTime > _respawnTime)
            {
                Respawn();
            }
        }
    }

    public void OnPickedUp()
    {
        _mesh.Visible = false;
        _isSpawned = false;
    }

    public void Respawn()
    {
        _respawnAccumulatedTime = 0.0f;
        _mesh.Visible = true;
        _isSpawned = true;
    }
}
