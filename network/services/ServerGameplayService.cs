using Godot;

public static class ServerGameplayService
{
    public static void HandleClientCommand(ENetPacketPeer peer, byte[] data)
    {
        int peerID = (int)peer.GetMeta("id");

        if (NetworkSession.Instance.PeerIDsToPlayerIDs.TryGetValue(peerID, out var playerID))
        {
            if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
            {
                var pawn = playerState.Pawn;
                if (pawn != null && pawn is Character character)
                {
                    var cmd = new ClientCommand();
                    cmd.ReadMessage(data);

                    ServerGame.Instance.ReceiveClientCommand(cmd, playerID);
                }
            }
        }

        // REFACTOR CODE
        if (NetworkSession.Instance.PeerIDsToPlayerIDs.TryGetValue(peerID, out var playerIDNew))
        {
            if (MatchState.Instance.NewConnectedPlayers.TryGetValue(playerIDNew, out var playerState))
            {
                var character = playerState.PublicState.Character; // COULD USE IS ALIVE CHECK
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