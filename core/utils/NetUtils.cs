using Godot;
using System;


public static class NetUtils
{
    private const string _playerIDString = "player_id";
    private const string _peerIDString = "player_id";

    public static void SetPeerPlayerID(ENetPacketPeer peer, byte playerID)
    {
        peer.SetMeta(_playerIDString, playerID);
    }

    public static byte GetPeerPlayerID(ENetPacketPeer peer)
    {
        return (byte)peer.GetMeta(_playerIDString);
    }

    public static void SetPeerID(ENetPacketPeer peer, byte peerID)
    {
        peer.SetMeta(_playerIDString, peerID);
    }

    public static byte GetPeerID(ENetPacketPeer peer)
    {
        return (byte)peer.GetMeta(_playerIDString);
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
