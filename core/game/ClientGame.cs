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
    public PlayerStateNew LocalPlayerStateNew { get; private set; }

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
        Game.Instance.AddChild(LocalPlayerController);

        GD.Print($"Starting client. NetworkMode = {NetworkSession.Instance.NetworkMode}");

        ClientProjectileManager.Create();
    }

    public void Tick()
    {
        SendCommand();
    }

    public void AssignPlayerState(PlayerState playerState)
    {
        LocalPlayerState = playerState;
    }

    public void SendCommand()
    {
        var inputCommand = GetClientInputCommand();

        if(NetworkSession.Instance.IsListenServer)
        {
            SendListenServerCommand(inputCommand);
        }
        else
        {
            SendClientCommand(inputCommand);
        }
    }

    public void SendListenServerCommand(ClientInputCommand inputCommand)
    {
        ClientCommand clientCommand = new();
        clientCommand.LastServerTickProcessedByClient = LastServerTickProcessedByClient;
        clientCommand.ClientTick = MatchState.Instance.CurrentTick;
        clientCommand.Commands = new ClientInputCommand[1];
        clientCommand.Commands[0] = inputCommand;
        ServerGame.Instance.ReceiveClientCommand(clientCommand, LocalPlayerID);
    }

    public void SendClientInput(ClientInputCommand inputCommand)
    {
        UnprocessedClientInputs.Add(inputCommand);
        var commandsToSend = UnprocessedClientInputs.Skip(Math.Max(0, UnprocessedClientInputs.Count - REDUNDANT_INPUTS)).ToArray();
        ClientCommand.Send(commandsToSend);
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

        if (LocalPlayerPawn != null)
        {
            cmd = LocalPlayerPawn.CaptureInput(cmd);
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

        UnprocessedClientInputs.RemoveAll(cmd => cmd.ClientTick <= LastClientTickProcessedByServer);


        PickupManager.Instance.ApplyPickupMask(snapshot.PickupMask);

        foreach (var playerState in snapshot.PlayerStates)
        {
            if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerState.PlayerID, out var foundPlayerState))
            {
                //GD.Print($"num unacked inputs: {ClientGame.Instance.UnprocessedClientInputs.Count}");
                // Client already has an instance of this character, apply snapshot, which could also kill it if it's not alive
                Character character = foundPlayerState.Character;
                if (character != null)
                {
                    character.ApplyAuthoritativePublicState(playerState.CharacterPublicState);

                    if(playerState.PlayerID == ClientGame.Instance.LocalPlayerID)
                    {
                        character.ApplyAuthoritativePrivateState(playerState.CharacterPrivateState);
                    }
                }
                // Client doesn't know about this character but it's alive, spawn it
                else if(playerState.IsAlive)
                {
                    SpawnManager.Instance.LocalSpawnPlayer(playerState.PlayerID, playerState.CharacterPublicState.Position, playerState.CharacterPublicState.Rotation.X);
                }
            }
        }

        ClientProjectileManager.Instance.ApplyWorldSnapshot(snapshot);
    }
}
