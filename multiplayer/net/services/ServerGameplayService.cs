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

                    ushort lastProcessedClientComandTick = MatchState.Instance.LastProcessedTickByPlayerID[playerID];

                    if (NetUtils.IsNewerTick(cmd.ClientTick, lastProcessedClientComandTick))
                    {
                        character.ReceiveClientCommand(cmd);
                    }
                }
            }
        }
    }
}