using Godot;
using System;

public partial class Actor : Node3D
{
    public NetworkRole Role = NetworkRole.NONE;

    public bool IsLocal => Role == NetworkRole.LOCAL;
    public bool IsRemote => Role == NetworkRole.REMOTE;
    public bool IsAuthority;

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
