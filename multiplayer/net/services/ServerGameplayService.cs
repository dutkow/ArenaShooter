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

                    ushort currentAcked = MatchState.Instance.LastAckedTickByPeerID.TryGetValue(peerID, out var lastTick)
                        ? lastTick
                        : (ushort)0;

                    // Wrap-safe update: only store if cmd.LastAppliedServerTick is newer
                    if (NetUtils.IsNewerTick((ushort)cmd.LastAppliedServerTick, currentAcked))
                    {
                        MatchState.Instance.LastAckedTickByPeerID[peerID] = (ushort)cmd.LastAppliedServerTick;
                    }
                    // Apply the input immediately on the server
                    double delta = NetworkConstants.SERVER_TICK_INTERVAL; // Or your fixed server tick
                    character.HandleClientCommand(cmd);
                }
            }
        }
    }
}