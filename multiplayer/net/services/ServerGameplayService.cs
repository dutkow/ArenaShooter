using Godot;
using System;

public static class ServerGameplayService
{
    public static void HandleClientCommand(ENetPacketPeer peer, byte[] data)
    {
        int peerID = (int)peer.GetMeta("id");

        if(NetworkSession.Instance.PeerIDsToPlayerIDs.TryGetValue(peerID, out var playerID))
        {
            if(MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
            {
                if(playerState.Character != null)
                {
                    var msg = new ClientCommand();
                    msg.ReadMessage(data);

                    playerState.Character.ApplyClientCommand(msg, 1.0f/60.0f); // TODO: manage actual tick deltas in a smart way
                }
            }
        }
    }
}
