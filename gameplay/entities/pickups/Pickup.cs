using Godot;
using System;

public partial class Pickup : Entity,  ICharacterCollidable
{
    public byte PickupID { get; private set; }

    [Export] Area3D _area;
    [Export] CollisionShape3D _collisionShape;


    [Export] float rotationSpeed = 3.0f;

    [Export] float pulseSpeed = 5.0f;       // How fast it bobs up and down
    [Export] float pulseMagnitude = 0.2f;  // How high/low it moves

    private float _accumulatedTime = 0.0f;

    private float _respawnTime = 6.0f;

    private float _timeUntilSpawn = 0.0f;

    [Export] MeshInstance3D _mesh;
    private Vector3 _baseMeshPosition;

    [Export] private bool _startSpawned = true;
    public bool IsSpawned { get; private set; } = true;

    public override void _Ready()
    {
        base._Ready();

        if(!_startSpawned)
        {
            IsSpawned = false;
            _mesh.Visible = false;
        }

        PickupManager.Instance.RegisterPickup(this);

        _baseMeshPosition = _mesh.Position;

        _area.AreaEntered += OnAreaEntered;

    }

    public void SetPickupID(byte pickupID)
    {
        PickupID = pickupID;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Tick((float)delta);
    }

    public void OnAreaEntered(Area3D area)
    {
        GD.Print($"Area entered. area: {area}");
    }

    public void Tick(float delta)
    {
        if(IsSpawned)
        {
            _mesh.Rotation = new Vector3(0.0f, _mesh.Rotation.Y + rotationSpeed * delta, 0.0f);

            _accumulatedTime += delta;
            float yOffset = (float)Math.Sin(_accumulatedTime * pulseSpeed) * pulseMagnitude;
            _mesh.Position = _baseMeshPosition + new Vector3(0, yOffset, 0);
        }
        else if(IsAuthority)
        {
            _timeUntilSpawn -= delta;
            if(_timeUntilSpawn <= 0.0f)
            {
                HandleSpawn();
            }
        }
    }

    public void HandlePickup()
    {
        OnTaken();
        PickupManager.Instance.SetPickupState(PickupID, IsSpawned);
    }

    public void HandleSpawn()
    {
        _timeUntilSpawn = _respawnTime;
        OnSpawned();
        PickupManager.Instance.SetPickupState(PickupID, IsSpawned);
    }

    public virtual void OnCollidedWith(Character character, CharacterPublicState state, bool isSimulating)
    {
        HandlePickup();
    }

    public void OnTaken()
    {
        _mesh.Visible = false;
        IsSpawned = false;
        PickupManager.Instance.SetPickupState(PickupID, IsSpawned);
    }

    public void OnSpawned()
    {
        _mesh.Visible = true;
        IsSpawned = true;
        PickupManager.Instance.SetPickupState(PickupID, IsSpawned);
    }

    public void SetIsSpawned(bool isSpawned)
    {
        if(IsSpawned == isSpawned)
        {
            return;
        }

        IsSpawned = isSpawned;

        if(IsSpawned)
        {
            OnSpawned();
        }
        else
        {
            OnTaken();
        }
    }
}
