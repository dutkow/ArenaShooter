using Godot;
using System;


public static class NetUtils
{
    public static void SetPeerPlayerID(ENetPacketPeer peer, byte ID)
    {
        peer.SetMeta("player_id", ID);
    }

    public static byte GetPeerPlayerID(ENetPacketPeer peer)
    {
        return (byte)peer.GetMeta("player_id");
    }


    public static bool IsNewerTick(ushort a, ushort b)
    {
        return (short)(a - b) > 0;
    }

    public static long IPStringToLong(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4) throw new FormatException("Invalid IPv4 address");

        return (long)(
            (byte.Parse(parts[0]) << 24) |
            (byte.Parse(parts[1]) << 16) |
            (byte.Parse(parts[2]) << 8) |
            byte.Parse(parts[3])
        );
    }

}



public static class Quantize
{
    const float POSITION_SCALE = 100f; // 1cm

    public static short Pos(float v)
    {
        return (short)Mathf.RoundToInt(v * POSITION_SCALE);
    }

    public static float Pos(short v)
    {
        return v / POSITION_SCALE;
    }

    public static byte Angle(float degrees)
    {
        return (byte)Mathf.RoundToInt((degrees % 360f) / 360f * 255f);
    }

    public static float Angle(byte b)
    {
        return b / 255f * 360f;
    }
}