using Godot;
using System;

public partial class Main : Node
{
    public static Main Instance { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
}
