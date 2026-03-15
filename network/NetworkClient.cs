using Godot;
using System;
using System.Threading.Tasks.Dataflow;

public class NetworkClient : NetworkPeer
{
    public static NetworkClient Instance { get; private set; }

    public ENetPacketPeer ServerPeer { get; private set; }

    public static NetworkClient Initialize()
    {
        Instance = new NetworkClient();

        return Instance;
    }

    public override void HandlePeerConnected(ENetPacketPeer peer)
    {

    }

    public override void HandlePeerDisconnected(ENetPacketPeer peer)
    {

    }


    public override void HandleReceivedPacketFromPeer(ENetPacketPeer peer, byte[] packet)
    {
        var type = Message.GetType(packet);
        ClientGame.Instance.HandleServerMessage(type, packet);
    }

    public void JoinServer(string ipAddress, int port)
    {
        Connection = new ENetConnection();
        var error = Connection.CreateHost(1);
        if (error != Error.Ok)
        {
            Connection = null;
            return;
        }

        ServerPeer = Connection.ConnectToHost(ipAddress, port);

        if(ServerPeer == null )
        {
            return;
        }

        GD.Print($"server peer is {ServerPeer}");

        ConnectionRequest.Send(Settings.Instance.PlayerName);
        GD.Print($"sending connection request");
    }

    public static void Send(Message message)
    {
        NetworkSender.ToServer(message);
    }
}
