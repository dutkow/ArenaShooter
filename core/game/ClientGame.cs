using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class ClientGame
{
    public static ClientGame Instance { get; private set; }

    // Player elements
    public PlayerController LocalPlayerController { get; private set; }
    public PlayerState LocalPlayerState { get; private set; }
    public PlayerStateNew LocalPlayerStateNew { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;

    public byte LocalPlayerID;

    // Events
    public event Action<PlayerState>? PlayerJoined;

    // Client-server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;
    public List<ClientPredictionTick> UnprocessedPredictionTicks = new();
    const int REDUNDANT_INPUTS = 4;

    private bool _hasReceivedInitialState;


    public static void Initialize()
    {
        if(ClientGame.Instance != null)
        {
            GD.Print($"Client game already exists");
        }

        CommandConsole.Instance.AddConsoleLogEntry($"=== Initializing Client Game ===");

        Instance = new ClientGame();
        Instance.InitMessageHandlers();
    }

    public void PostLoad()
    {
        if (NetworkManager.Instance.IsClient)
        {
            CommandConsole.Instance.AddConsoleLogEntry($"Level fully loaded. Requesting initial match state.");
            InitialMatchStateRequest.Send();
        }
        else
        {
            _hasReceivedInitialState = true;
            ServerGame.Instance.ApplyClientLoaded(new PlayerInfo(ClientGame.Instance.LocalPlayerID, UserSettings.Instance.PlayerName));
        }

        LocalPlayerController = new();
        Game.Instance.AddChild(LocalPlayerController);

        LocalPlayerController.Initialize();

        ClientProjectileManager.Initialize();
    }

    public void SetLocalPlayerID(byte localPlayerID)
    {
        LocalPlayerID = localPlayerID;
    }

    public void Tick(double delta)
    {
        if(_hasReceivedInitialState)
        {
            SendCommand();
        }
    }

    public void AssignPlayerState(PlayerState playerState)
    {
        LocalPlayerState = playerState;
    }

    public void SendCommand()
    {
        var predictionTick = GetClientPredictionTick();

        if(NetworkManager.Instance.IsListenServer)
        {
            SendListenServerCommand(predictionTick.InputCommand);
        }
        else
        {
            UnprocessedPredictionTicks.Add(predictionTick);

            if(predictionTick.CollisionEnteredCollidables.Count > 0)
            {
                GD.Print($"added a prediction tick w/ collison entered collidables");
            }
            SendClientCommand(predictionTick.InputCommand);
        }
    }

    public void SendListenServerCommand(ClientInputCommand inputCommand)
    {
        ClientCommand clientCommand = new();
        clientCommand.LastServerTickProcessedByClient = LastServerTickProcessedByClient;
        clientCommand.ClientTick = MatchState.Instance.CurrentTick;
        clientCommand.Commands = new ClientInputCommand[1];
        clientCommand.Commands[0] = inputCommand;

        ServerGame.Instance.ApplyClientCommand(ClientGame.Instance.LocalPlayerID, clientCommand);
    }


    public void SendClientCommand(ClientInputCommand cmd)
    {
        var commandsToSend = UnprocessedPredictionTicks.Skip(Math.Max(0, UnprocessedPredictionTicks.Count - REDUNDANT_INPUTS)).Select(t => t.InputCommand).ToArray();
        ClientCommand.Send(commandsToSend);
    }

    public ClientPredictionTick GetClientPredictionTick()
    {
        var clientPredictionTick = new ClientPredictionTick();

        if (LocalPlayerPawn != null)
        {
            clientPredictionTick = LocalPlayerPawn.GetClientPredictionTick(clientPredictionTick);
        }
        else
        {
            GD.Print($"local player pawn is null");
        }

        clientPredictionTick.InputCommand.ClientTick = MatchState.Instance.CurrentTick;

        return clientPredictionTick;
    }

    public void HandleWorldSnapshot(WorldSnapshot snapshot)
    {
        if (!NetUtils.IsNewerTick(snapshot.ServerTick, LastServerTickProcessedByClient))
        {
            return;
        }

        LastClientTickProcessedByServer = snapshot.LastProcessedClientTick;
        LastServerTickProcessedByClient = snapshot.ServerTick;

        UnprocessedPredictionTicks.RemoveAll(cmd => cmd.InputCommand.ClientTick <= LastClientTickProcessedByServer);


        PickupManager.Instance.ApplyPickupMask(snapshot.PickupMask);

        foreach (var playerState in snapshot.PlayerStates)
        {
            if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerState.PlayerInfo.PlayerID, out var foundPlayerState))
            {
                //GD.Print($"num unacked inputs: {ClientGame.Instance.UnprocessedClientInputs.Count}");
                // Client already has an instance of this character, apply snapshot, which could also kill it if it's not alive
                Character character = foundPlayerState.Character;
                if (character != null)
                {
                    character.ApplyAuthoritativePublicState(playerState.CharacterPublicState, (float)TickManager.Instance.ServerTickInterval);

                    if(playerState.PlayerInfo.PlayerID == ClientGame.Instance.LocalPlayerID)
                    {
                        character.ApplyAuthoritativePrivateState(playerState.CharacterPrivateState);
                    }
                }
                // Client doesn't know about this character but it's alive, spawn it
                else if(playerState.IsSpawned)
                {
                    SpawnManager.Instance.LocalSpawnPlayer(playerState.PlayerInfo.PlayerID, playerState.CharacterPublicState.Position, playerState.CharacterPublicState.Yaw);

                    GD.Print($"spawning player on client w/ yaw: {playerState.CharacterPublicState.Yaw}");
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
        SetLocalPlayerID(connectionAccepted.AssignedPlayerID);

        GD.Print($"Starting client. NetworkMode = {NetworkManager.Instance.NetworkMode}");

        NetworkManager.Instance.BroadcastServerJoined();
    }

    public void HandleConnectionDenied(ConnectionDenied connectionDenied)
    {

    }

    public void HandleInitialMatchState(InitialMatchState initialMatchState)
    {
        _hasReceivedInitialState = true;

        MatchState.Instance.OnReceivedInitialMatchState(initialMatchState);

        var tickManager = TickManager.Create();
        tickManager.SetServerTickRate(initialMatchState.ServerTickRate);
        Level.Instance.Tickables.Add(tickManager);

        CommandConsole.Instance.AddConsoleLogEntry($"Received initial match state from server.");

    }

    public void HandlePlayerJoined(PlayerJoined playerJoined)
    {
        MatchState.Instance.AddPlayer(playerJoined.PlayerInfo);
    }

    public void HandlePlayerLeft(PlayerLeft playerLeft)
    {
        MatchState.Instance.HandlePlayerLeft(playerLeft.PlayerID);
    }

    public void HandlePlayerNameChanged(PlayerNameChanged playerNameChange)
    {
        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerNameChange.PlayerID, out var playerState))
        {
            playerState.SetPlayerName(playerNameChange.Name);
        }
    }

    public void HandleTickRateChanged(TickRateChanged tickRateChanged)
    {
        TickManager.Instance.SetServerTickRate(tickRateChanged.TickRate);
    }

    public void HandleServerNotification(ServerNotification notification)
    {
        switch(notification.Type)
        {
            case ServerNotificationType.DISCONNECTION_SERVER_SHUTDOWN:
                GD.Print($"RECEIVED SERVER SHUTDOWN NOTICE");
                SceneNavigator.OpenMainMenu();
                break;
        }
    }


    private void Dispatch<T>(byte[] payload, Action<T> handler) where T : Message, new()
    {
        var msg = Message.FromData<T>(payload);
        handler(msg);
    }

    private readonly Dictionary<Msg, Action<byte[]>> _messageHandlers = new();

    public virtual void InitMessageHandlers()
    {
        _messageHandlers[Msg.S2C_PLAYER_JOINED] = payload => Dispatch<PlayerJoined>(payload, HandlePlayerJoined);

        _messageHandlers[Msg.S2C_CONNECTION_ACCEPTED] = payload => Dispatch<ConnectionAccepted>(payload, HandleConnectionAccepted);
        _messageHandlers[Msg.S2C_CONNECTION_DENIED] = payload => Dispatch<ConnectionDenied>(payload, HandleConnectionDenied);
        _messageHandlers[Msg.S2C_INITIAL_MATCH_STATE] = payload => Dispatch<InitialMatchState>(payload, HandleInitialMatchState);
        _messageHandlers[Msg.S2C_WORLD_SNAPSHOT] = payload => Dispatch<WorldSnapshot>(payload, HandleWorldSnapshot);
        _messageHandlers[Msg.S2C_PLAYER_NAME_CHANGED] = payload => Dispatch<PlayerNameChanged>(payload, HandlePlayerNameChanged);
        _messageHandlers[Msg.S2C_TICK_RATE_CHANGED] = payload => Dispatch<TickRateChanged>(payload, HandleTickRateChanged);
        _messageHandlers[Msg.S2C_PLAYER_JOINED] = payload => Dispatch<PlayerJoined>(payload, HandlePlayerJoined);
        _messageHandlers[Msg.S2C_PLAYER_LEFT] = payload => Dispatch<PlayerLeft>(payload, HandlePlayerLeft);
        _messageHandlers[Msg.S2C_SERVER_NOTIFICATION] = payload => Dispatch<ServerNotification>(payload, HandleServerNotification);


    }
}
