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


                    // Handle client tick
                    if (MatchState.Instance.LastProcessedTickByPlayerID.TryGetValue(playerID, out var lastProcessedTick))
                    {
                        if (NetUtils.IsNewerTick(cmd.ClientTick, lastProcessedTick))
                        {
                            character.ReceiveClientCommand(cmd);
                            MatchState.Instance.LastProcessedTickByPlayerID[playerID] = cmd.ClientTick;
                        }
                    }
                    else
                    {
                        character.ReceiveClientCommand(cmd);
                        MatchState.Instance.LastProcessedTickByPlayerID[playerID] = cmd.ClientTick;
                    }

                    if (MatchState.Instance.LastReceivedServerTickByPlayerID.TryGetValue(playerID, out var lastReceivedServerTick))
                    {
                        if (NetUtils.IsNewerTick(cmd.LastReceivedServerTick, lastReceivedServerTick))
                        {
                            MatchState.Instance.LastReceivedServerTickByPlayerID[playerID] = cmd.LastReceivedServerTick;
                        }
                    }
                    else
                    {
                        MatchState.Instance.LastReceivedServerTickByPlayerID[playerID] = cmd.LastReceivedServerTick;
                    }
                }
            }
        }
    }
}