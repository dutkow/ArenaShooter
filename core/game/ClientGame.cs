using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ClientGame
{
    public static ClientGame Instance { get; private set; }

    // Player elements
    public PlayerController LocalPlayerController { get; private set; }
    public PlayerState LocalPlayerState { get; private set; }
    public PlayerStateNew LocalPlayerStateNew { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;

    // Client-server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;
    public List<ClientInputCommand> UnprocessedClientInputs = new();
    const int REDUNDANT_INPUTS = 4;

    public static void Initialize()
    {
        if(ClientGame.Instance != null)
        {
            GD.Print($"Client game already exists");
        }
        Instance = new ClientGame();

        Instance.LocalPlayerController = new();

        if(NetworkManager.Instance.IsClient)
        {
            PickupManager.Initialize();
            ClientProjectileManager.Initialize();
        }
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

        if(NetworkManager.Instance.IsListenServer)
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
        ServerGame.Instance.ReceiveClientCommand(clientCommand, NetworkClient.Instance.LocalPlayerID);
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

                    if(playerState.PlayerID == NetworkClient.Instance.LocalPlayerID)
                    {
                        character.ApplyAuthoritativePrivateState(playerState.CharacterPrivateState);
                    }
                }
                // Client doesn't know about this character but it's alive, spawn it
                else if(playerState.IsAlive)
                {
                    SpawnManager.Instance.LocalSpawnPlayer(playerState.PlayerID, playerState.CharacterPublicState.Position, playerState.CharacterPublicState.Look.X);
                }
            }
        }

        ClientProjectileManager.Instance.ApplyWorldSnapshot(snapshot);
    }


    public void HandleServerMessage(Msg type, byte[] payload)
    {
        if(_messageHandlers.TryGetValue(type, out var handler))
        {
            handler(payload);
        }
        else
        {
            GD.Print($"Unknown server message type: {type}");
        }
    }

    public void HandleConnectionAccepted(ConnectionAccepted connectionAccepted)
    {
        NetworkClient.Instance.SetLocalPlayerID(connectionAccepted.AssignedPlayerID);

        GD.Print($"Starting client. NetworkMode = {NetworkManager.Instance.NetworkMode}");

        ClientProjectileManager.Initialize();
    }

    public void HandleConnectionDenied(ConnectionDenied connectionDenied)
    {

    }

    public void HandleInitialMatchState(InitialMatchState initialMatchState)
    {

    }

    public void HandleWorldSnapshot(WorldSnapshot worldSnapshot)
    {

    }

    private void Dispatch<T>(byte[] payload, Action<T> handler) where T : Message, new()
    {
        var msg = Message.FromData<T>(payload);
        handler(msg);
    }

    private readonly Dictionary<Msg, Action<byte[]>> _messageHandlers;

    public virtual void InitMessageHandlers()
    {
        _messageHandlers[Msg.S2C_CONNECTION_ACCEPTED] = payload => Dispatch<ConnectionAccepted>(payload, HandleConnectionAccepted);
        _messageHandlers[Msg.S2C_CONNECTION_DENIED] = payload => Dispatch<ConnectionDenied>(payload, HandleConnectionDenied);
        _messageHandlers[Msg.S2C_INITIAL_MATCH_STATE] = payload => Dispatch<InitialMatchState>(payload, HandleInitialMatchState);
        _messageHandlers[Msg.S2C_WORLD_SNAPSHOT] = payload => Dispatch<WorldSnapshot>(payload, HandleWorldSnapshot);
    }
}
