using Godot;
using System;

public static class InputUtils
{
    public static bool AreInputEventsEqual(InputEventWithModifiers a, InputEventWithModifiers b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if(!AreModifiersEqual(a,b))
        {
            return false;
        }

        if(a is InputEventKey eventKeyA && b is InputEventKey eventKeyB)
        {
            return eventKeyA.Keycode == eventKeyB.Keycode;
        }

        if(a is InputEventMouseButton eventMouseButtonA && b is InputEventMouseButton eventMouseButtonB)
        {
            return eventMouseButtonA.ButtonIndex == eventMouseButtonB.ButtonIndex;
        }

        return false;
    }

    public static bool AreModifiersEqual(InputEventWithModifiers a, InputEventWithModifiers b)
    {
        return a.CtrlPressed == b.CtrlPressed && a.AltPressed == b.AltPressed && a.ShiftPressed == b.ShiftPressed;
    }
}
