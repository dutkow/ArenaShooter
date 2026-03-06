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
                var character = playerState.Character;
                if (character != null)
                {
                    // Deserialize byte[] into ClientCommand
                    var cmd = new ClientCommand();
                    cmd.ReadMessage(data);

                    // Apply the input immediately on the server
                    double delta = NetworkConstants.SERVER_TICK_INTERVAL; // Or your fixed server tick
                    character.HandleClientCommandBatch(cmd, delta);
                }
            }
        }
    }
}