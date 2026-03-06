using Godot;

public static class NetUtils
{
    public static bool IsNewerTick(ushort a, ushort b)
    {
        return (short)(a - b) > 0;
    }

    /// <summary>
    /// Converts a Vector3 to Vector3i with rounding.
    /// Optionally applies a scale factor for sub-unit precision.
    /// </summary>
    public static Vector3I ToVector3i(Vector3 vec, float scale = 1f)
    {
        return new Vector3I(
            Mathf.RoundToInt(vec.X * scale),
            Mathf.RoundToInt(vec.Y * scale),
            Mathf.RoundToInt(vec.Z * scale)
        );
    }

    /// <summary>
    /// Converts a Vector3i back to Vector3 with optional inverse scaling.
    /// </summary>
    public static Vector3 ToVector3(Vector3I vec, float scale = 1f)
    {
        return new Vector3(
            vec.X / scale,
            vec.Y / scale,
            vec.Z / scale
        );
    }

    public static byte FloatToByteAngle(float degrees)
    {
        // wrap to 0-360 just in case
        degrees = degrees % 360f;
        if (degrees < 0) degrees += 360f;

        // scale 0..360 -> 0..255
        return (byte)(degrees / 360f * 255f);
    }

    public static float ByteToFloatAngle(byte b)
    {
        return (b / 255f) * 360f;
    }
}