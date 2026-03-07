using Godot;
using System;

public partial class Actor : Node3D
{
    public NetworkRole Role = NetworkRole.NONE;

    public bool IsLocal => Role == NetworkRole.LOCAL;
    public bool IsRemote => Role == NetworkRole.REMOTE;
    public bool IsAuthority;

    public virtual void Tick(float delta)
    {

    }
}
