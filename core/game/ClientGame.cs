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
    List<ClientInputCommand> _unacknowledgedClientInputs = new();
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

        if(NetworkSession.Instance.IsClient)
        {
            SendClientInput();
        }
        else
        {
            // apply client input, etc.
        }
    }

    public void AssignPlayerState(PlayerState playerState)
    {
        LocalPlayerState = playerState;
    }

    public void SendClientInput()
    {
        var cmd = new ClientInputCommand();
        cmd = LocalPlayerController.AddInput(cmd);

        if(LocalPlayerPawn != null)
        {
            cmd = LocalPlayerPawn.AddInput(cmd);
        }

        cmd.ClientTick = MatchState.Instance.CurrentTick;
        _unacknowledgedClientInputs.Add(cmd);

        var commandsToSend = _unacknowledgedClientInputs.Skip(Math.Max(0, _unacknowledgedClientInputs.Count - REDUNDANT_INPUTS)).ToArray();

        ClientCommand.Send(commandsToSend);
    }
}
