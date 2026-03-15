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

    public Dictionary<byte, ENetPacketPeer> PeersByPeerID = new();
    public Dictionary<byte, ENetPacketPeer> PeersByPlayerID = new();

    byte _nextAvailablePeerID;

    private ServerAdvertiser _advertiser;

    public static NetworkServer Initialize()
    {
        Instance = new NetworkServer();

        return Instance;
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
        GD.Print($"peer connected to network server");
        byte peerID = GetNextAvailablePeerID();
        NetUtils.SetPeerID(peer, GetNextAvailablePeerID());
        PeersByPeerID[peerID] = peer;
    }

    public override void HandlePeerDisconnected(ENetPacketPeer peer)
    {
        byte peerID = NetUtils.GetPeerID(peer);
    }


    public override void HandleReceivedPacketFromPeer(ENetPacketPeer peer, byte[] packet)
    {
        var type = Message.GetType(packet);

        ServerGame.Instance?.HandleClientMessage(peer, type, packet);
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
