using Godot;
using System;
using System.Net;
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
        ConnectionRequest.Send(UserSettings.Instance.PlayerName);
        CommandConsole.Instance.AddConsoleLogEntry($"Connection to server established. Sending connection request.");

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
            CommandConsole.Instance.AddConsoleLogEntry($"Failed to join server");
            return;
        }

        ServerPeer = Connection.ConnectToHost(ipAddress, port);

        CommandConsole.Instance.AddConsoleLogEntry($"Attempting to join server. IP: {ipAddress}. Port: {port}");

        if (ServerPeer == null )
        {
            return;
        }
    }

    public static void Send(Message message)
    {
        NetworkSender.ToServer(message);
    }

    public void DisconnectFromServer()
    {
        Connection?.Flush();
        ServerPeer.PeerDisconnect();

        NetworkManager.Instance.DisconnectFromServer();
    }
}
