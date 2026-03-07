using Godot;
using System;

public partial class NetManager : Node
{
    public static NetManager Instance;

    public Rid PhysicsSpace { get; private set; }


    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        PhysicsSpace = GetViewport().World3D.Space;
        PhysicsServer3D.SpaceSetActive(PhysicsSpace, false);
    }
}
