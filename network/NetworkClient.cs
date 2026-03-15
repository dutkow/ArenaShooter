using Godot;
using System;

public class NetworkClient : NetworkPeer
{
    public static NetworkClient Instance { get; private set; }

    public byte LocalPlayerID;

    public ENetPacketPeer ServerPeer { get; private set; }

    public static void Initialize()
    {
        Instance = new NetworkClient();

    }

    public void SetLocalPlayerID(byte localPlayerID)
    {
        LocalPlayerID = localPlayerID;
    }

    public override void HandlePeerConnected(ENetPacketPeer peer)
    {

    }

    public override void HandlePeerDisconnected(ENetPacketPeer peer)
    {

    }


    public override void HandleReceivedPacketFromPeer(ENetPacketPeer peer, byte[] packet)
    {

    }

    public Error JoinServer(string ipAddress, int port)
    {
        Connection = new ENetConnection();
        var error = Connection.CreateHost(1); // 1 client
        if (error != Error.Ok)
        {
            Connection = null;
            return error;
        }

        ServerPeer = Connection.ConnectToHost(ipAddress, port);

        if(ServerPeer == null )
        {
            return Error.Failed;
        }

        GD.Print($"server peer is {ServerPeer}");

        return error;
    }

    public static void Send(Message message)
    {
        NetworkSender.ToServer(message);
    }
}
