using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

public enum ServerMode
{
    LAN,
    INTERNET,
}

public class NetworkServer : NetworkPeer
{
    public static NetworkServer Instance { get; private set; }

    public ServerMode ServerMode;


    private const string _ID = "id";
    public Dictionary<byte, ENetPacketPeer> PeersByPeerID = new();
    byte _nextAvailablePeerID;

    private ServerAdvertiser _advertiser;

    public static void Initialize()
    {
        Instance = new NetworkServer();

    }

    public void InitializeLanServer(ServerInfo _serverInfo)
    {
        ServerMode = ServerMode.LAN;

        _advertiser = new LanServerAdvertiser();
        _advertiser.StartBroadcast(_serverInfo);
    }


    public override void OnPeerConnected(ENetPacketPeer peer)
    {
        if (peer == null) return;

        byte peerID = GetNextAvailablePeerID();
        AssignPeerID(peer);
        PeersByPeerID[peerID] = peer;
    }

    public override void OnPeerDisconnected(ENetPacketPeer peer)
    {
        if (peer != null) return;

        byte peerID = (byte)peer.GetMeta(_ID);
    }


    public virtual void OnReceivedPacketFromPeer(ENetPacketPeer peer, Msg type, byte[] packet)
    {
        if(peer == null) return;

        ServerGame.Instance?.HandleClientMessage(peer, type, packet);
    }

    public static byte GetPeerID(ENetPacketPeer peer)
    {
        if(peer == null)
        {
            GD.PushError("Peer is null!");
            return byte.MaxValue;
        }

        return (byte)peer.GetMeta(_ID);
    }

    public void AssignPeerID(ENetPacketPeer peer)
    {
        peer.SetMeta("id", GetNextAvailablePeerID());
    }

    public byte GetNextAvailablePeerID()
    {
        for(byte b = 0; b < byte.MaxValue; ++b)
        {
            if(!PeersByPeerID.ContainsKey(b))
            {
                return b;
            }
        }
        return byte.MaxValue;
    }

}
