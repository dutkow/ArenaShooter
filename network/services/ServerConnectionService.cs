using Godot;
using System;

/// <summary>
/// Handles server-side logic when receiving connection-related messages from a client.
/// </summary>
public static class ServerConnectionService
{
    public static void HandleConnectionRequest(ENetPacketPeer peer, byte[] data)
    {
        var msg = new ConnectionRequest();
        msg.ReadMessage(data);

        NetworkManager.Instance.HandleConnectionRequest(peer, msg.PlayerName);

        GD.Print($"Server received: {msg.MessageType}");
    }

    public static void HandleClientLoaded(ENetPacketPeer peer, byte[] data)
    {
        var msg = new InitialMatchStateRequest();
        msg.ReadMessage(data);

        int peerID = (int)peer.GetMeta("id");

        byte playerID = NetworkManager.Instance.PeerIDsToPlayerIDs[peerID];
        string playerName = NetworkManager.Instance.PlayerIDsToPlayerStates[playerID].PlayerInfo.PlayerName;

        NetworkPeer.Instance.ReadyPeers.Add(peer);

        ServerGame.Instance.LastProcessedServerTicksByPlayerID[playerID] = 0;
        ServerGame.Instance.LastProcessedClientTicksByPlayerID[playerID] = 0;

        var joiningPlayerInfo = new PlayerInfo(playerID, playerName);

        MatchState.Instance.AddPlayer(joiningPlayerInfo);

        PlayerJoined.Send(playerID, joiningPlayerInfo.PlayerName);

        var spawnedPlayer = SpawnManager.Instance.ServerSpawnPlayer(playerID); // spawn the joining player



        GD.Print($"Server received: {msg.MessageType}");

        InitialMatchState.Send(peer);
    }
}
