using Godot;
using System;

/// <summary>
/// Base class for all objects that exist in the game world that have state synchronization properties.
/// </summary>
public partial class Entity : Node3D
{
    public bool IsAuthority { get; private set; }

    public NetworkRole Role = NetworkRole.NONE;

    public bool IsLocal => Role == NetworkRole.LOCAL;
    public bool IsRemote => Role == NetworkRole.REMOTE;

    public override void _Ready()
    {
        base._Ready();

        IsAuthority = NetworkManager.Instance.NetworkMode != NetworkMode.CLIENT;
    }
    public void SetRole(NetworkRole role)
    {
        Role = role;
    }

    public void SetIsAuthority(bool isAuthority)
    {
        IsAuthority = isAuthority;
    }

    public virtual void Tick(float delta)
    {

    }
}
