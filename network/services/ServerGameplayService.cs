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
                    // Deserialize byte[] into ClientCommand
                    var cmd = new ClientCommand();
                    cmd.ReadMessage(data);

                    GD.Print($"receiving client command.  client tick {cmd.ClientTick}");
                    ServerGame.Instance.ReceiveClientCommand(cmd, playerID);

                }
            }
        }
    }
}