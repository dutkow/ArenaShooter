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

    public ServerInfo ServerInfo;

    private const string _ID = "id";
    public Dictionary<byte, ENetPacketPeer> PeersByPeerID = new();
    public Dictionary<byte, ENetPacketPeer> PeersByPlayerID = new();

    byte _nextAvailablePeerID;

    private ServerAdvertiser _advertiser;

    public static void Initialize()
    {
        Instance = new NetworkServer();

    }

    public void InitializeLanServer(ServerInfo _serverInfo)
    {

    }

    public Error StartLanServer(string IP, int port, ServerInfo serverInfo)
    {
        Connection = new ENetConnection();
        var error = Connection.CreateHostBound(IP, port);
        if (error != Error.Ok)
        {
            Connection = null;
            return error;
        }

        ServerMode = ServerMode.LAN;
        ServerInfo = serverInfo;

        _advertiser = new LanServerAdvertiser();
        _advertiser.StartBroadcast(ServerInfo);

        OnServerStarted?.Invoke();

        return error;
    }


    public override void HandlePeerConnected(ENetPacketPeer peer)
    {
        if (peer == null) return;

        byte peerID = GetNextAvailablePeerID();
        AssignPeerID(peer);
        PeersByPeerID[peerID] = peer;
    }

    public override void HandlePeerDisconnected(ENetPacketPeer peer)
    {
        if (peer != null) return;

        byte peerID = GetPeerID(peer);
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
