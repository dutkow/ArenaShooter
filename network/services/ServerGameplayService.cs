using Godot;

public static class ServerGameplayService
{
    public static void HandleClientCommand(ENetPacketPeer peer, byte[] data)
    {
        int peerID = (int)peer.GetMeta("id");


        // REFACTOR CODE
        if (NetworkSession.Instance.PeerIDsToPlayerIDs.TryGetValue(peerID, out var playerID))
        {
            if (MatchState.Instance.NewConnectedPlayers.TryGetValue(playerID, out var playerState))
            {
                var character = playerState.Character; // COULD USE IS ALIVE CHECK
                if (character != null)
                {
                    var cmd = new ClientCommand();
                    cmd.ReadMessage(data);

                    ServerGame.Instance.ReceiveClientCommand(cmd, playerID);
                }
            }
        }
    }
}