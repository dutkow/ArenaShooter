using Godot;
using System;

public static class UnitConversion
{
    const float QUAKE_UNIT_MULTIPLIER = 32.0F;

    public static float ToQuake(float value)
    {
        return value * QUAKE_UNIT_MULTIPLIER;
    }
}
