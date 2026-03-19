using Godot;
using System.Collections.Generic;

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

    public HashSet<ENetPacketPeer> ReadyPeers = new();

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

        return error;
    }


    public override void HandlePeerConnected(ENetPacketPeer peer)
    {
        byte peerID = GetNextAvailablePeerID();
        NetUtils.SetPeerID(peer, GetNextAvailablePeerID());
        PeersByPeerID[peerID] = peer;
    }

    public override void HandlePeerDisconnected(ENetPacketPeer peer)
    {
        ReadyPeers.Remove(peer);
        PeersByPeerID.Remove(NetUtils.GetPeerPlayerID(peer));
        PeersByPlayerID.Remove(NetUtils.GetPeerPlayerID(peer));

        if(!_serverShuttingDown)
        {
            byte playerID = NetUtils.GetPeerPlayerID(peer);
            MatchState.Instance.HandlePlayerLeft(playerID);
            PlayerLeft.Send(playerID);
        }
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

    private bool _serverShuttingDown = false;

    public void StartServerShutdown()
    {
        _serverShuttingDown = true;

        ServerNotification.Broadcast(ServerNotificationType.DISCONNECTION_SERVER_SHUTDOWN);

        Connection?.Flush();

        _advertiser.StopBroadcast();
        _advertiser = null;

        ClientGame.Shutdown();
        ServerGame.Shutdown();

        NetworkManager.Instance.ShutdownServer();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        Connection.Destroy();
    }
}
