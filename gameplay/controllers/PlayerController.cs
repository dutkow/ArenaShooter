using Godot;
using System;

public partial class PlayerController : Controller
{
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }
}
