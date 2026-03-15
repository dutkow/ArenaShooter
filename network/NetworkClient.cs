using Godot;
using System;

public class NetworkClient : NetworkPeer
{
    public static NetworkClient Instance { get; private set; }

    public byte LocalPlayerID;

    public static void Initialize()
    {
        Instance = new NetworkClient();

    }

    public void SetLocalPlayerID(byte localPlayerID)
    {
        LocalPlayerID = localPlayerID;
    }

    public override void OnPeerConnected(ENetPacketPeer peer)
    {

    }

    public override void OnPeerDisconnected(ENetPacketPeer peer)
    {

    }


    public override void OnReceivedPacketFromPeer(ENetPacketPeer peer, byte[] packet)
    {

    }
}
