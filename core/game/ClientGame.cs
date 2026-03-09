using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ClientGame
{
    public static ClientGame Instance { get; private set; }

    // Player elements
    public byte LocalPlayerID { get; private set; }
    public PlayerController LocalPlayerController { get; private set; }
    public PlayerState LocalPlayerState { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;

    // Client-server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;
    public List<ClientInputCommand> UnprocessedClientInputs = new();
    const int REDUNDANT_INPUTS = 4;


    public void Initialize(byte localPlayerID)
    {
        Instance = this;

        LocalPlayerID = localPlayerID;
        LocalPlayerController = new();

        GD.Print($"Starting client. NetworkMode = {NetworkSession.Instance.NetworkMode}");

    }

    public void Tick()
    {
        var cmd = GetClientInputCommand(); // client generates input and applies it locally

        LocalPlayerController?.ApplyInput(cmd);
        LocalPlayerPawn?.ApplyInput(cmd);

        if(!NetworkSession.Instance.IsListenServer)
        {
            SendClientCommand(cmd);
        }
    }

    public void AssignPlayerState(PlayerState playerState)
    {
        LocalPlayerState = playerState;
    }

    public void SendClientCommand(ClientInputCommand cmd)
    {
        UnprocessedClientInputs.Add(cmd);
        var commandsToSend = UnprocessedClientInputs.Skip(Math.Max(0, UnprocessedClientInputs.Count - REDUNDANT_INPUTS)).ToArray();
        ClientCommand.Send(commandsToSend);
    }

    public ClientInputCommand GetClientInputCommand()
    {
        var cmd = new ClientInputCommand();

        //cmd = LocalPlayerController.AddInput(cmd);

        if (LocalPlayerPawn != null)
        {
            cmd = LocalPlayerPawn.AddInput(cmd);
        }

        cmd.ClientTick = MatchState.Instance.CurrentTick;

        return cmd;
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        if (!NetUtils.IsNewerTick(snapshot.ServerTick, LastServerTickProcessedByClient))
        {
            return;
        }

        LastClientTickProcessedByServer = snapshot.LastProcessedClientTick;
        LastServerTickProcessedByClient = snapshot.ServerTick;

        var characterSnapshots = snapshot.GetCharacterSnapshots();

        UnprocessedClientInputs.RemoveAll(cmd => cmd.ClientTick <= LastClientTickProcessedByServer);


        for (int i = 0; i < characterSnapshots.Length; ++i)
        {
            var characterSnapshot = characterSnapshots[i];

            if (MatchState.Instance.ConnectedPlayers.TryGetValue(characterSnapshot.PlayerID, out var playerState))
            {
                Pawn pawn = playerState.Pawn;
                if (pawn != null)
                {
                    pawn.ApplySnapshot(characterSnapshot);
                }
                else
                {
                    SpawnManager.Instance.LocalSpawnPlayer(characterSnapshot.PlayerID, characterSnapshot.Position, characterSnapshot.Yaw);
                    GD.Print($"Player not found so spawning player at position {characterSnapshot.Position}");
                }
            }
            else
            {
                GD.Print($"player not found in ConnectedPlayers: {characterSnapshot.PlayerID}");
            }
        }

    }
}
