using Godot;
using System;

public partial class LevelUI : CanvasLayer
{
    public static LevelUI Instance { get; private set; }

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

    }
}
