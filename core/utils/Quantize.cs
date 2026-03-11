using Godot;
using System;

public static class Quantize
{
    // --- POSITION ---
    public const float POSITION_PRECISION = 0.02f;
    private const float POSITION_SCALE = 1f / POSITION_PRECISION;

    public static short Position(float v)
    {
        float max = short.MaxValue * POSITION_PRECISION;
        v = Mathf.Clamp(v, -max, max);
        return (short)Mathf.RoundToInt(v * POSITION_SCALE);
    }

    public static float Position(short v)
    {
        return v * POSITION_PRECISION;
    }

    // --- VELOCITY ---
    public const float MAX_VELOCITY = 50.0f;
    private const float VELOCITY_SCALE = short.MaxValue / MAX_VELOCITY;

    public static short Velocity(float v)
    {
        v = Mathf.Clamp(v, -MAX_VELOCITY, MAX_VELOCITY);
        return (short)Mathf.RoundToInt(v * VELOCITY_SCALE);
    }

    public static float Velocity(short v)
    {
        return v / VELOCITY_SCALE;
    }

    // --- ANGLES (radians) ---
    private const float PI = Mathf.Pi;
    private const float ANGLE_SCALE = short.MaxValue / PI;

    public static short Angle(float radians)
    {
        radians = Mathf.Wrap(radians, -PI, PI);
        return (short)Mathf.RoundToInt(radians * ANGLE_SCALE);
    }

    public static float Angle(short v)
    {
        return v / ANGLE_SCALE;
    }
}