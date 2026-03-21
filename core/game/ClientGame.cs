using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class ClientGame : Singleton<ClientGame>
{
    public Player LocalPlayer;

    // Player elements
    public PlayerController LocalPlayerController { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;

    public byte LocalPlayerID;

    // Events
    public event Action<Player>? PlayerJoined;

    // Client-server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;
    public List<ClientInputCommand> UnprocessedClientInputCommands = new();
    const int REDUNDANT_INPUTS = 4;

    private bool _hasReceivedInitialState;




    protected override void OnInitialize()
    {
        CommandConsole.Instance.AddConsoleLogEntry($"=== Initializing Client Game ===");

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
            ServerGame.Instance.ApplyClientLoaded(new PlayerInfo(ClientGame.Instance.LocalPlayerID, NetUtils.ValidatePlayerName(UserSettings.Instance.PlayerName)));
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

    public void AssignPlayer(Player player)
    {
        LocalPlayer = player;
    }

    public void SendCommand()
    {
        var clientInputCommand = GetClientInputCommand();

        if(NetworkManager.Instance.IsListenServer)
        {
            SendListenServerCommand(clientInputCommand);
        }
        else
        {
            UnprocessedClientInputCommands.Add(clientInputCommand);

            SendClientCommand(clientInputCommand);
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
        var commandsToSend = UnprocessedClientInputCommands.Skip(Math.Max(0, UnprocessedClientInputCommands.Count - REDUNDANT_INPUTS)).ToArray();
        ClientCommand.Send(commandsToSend);
    }

    public ClientInputCommand GetClientInputCommand()
    {
        ClientInputCommand clientInputCommand = new();

        if (LocalPlayerPawn != null)
        {
            clientInputCommand = LocalPlayerPawn.GetClientInputCommand(clientInputCommand);
        }

        clientInputCommand.ClientTick = MatchState.Instance.CurrentTick;

        return clientInputCommand;
    }

    public void HandleWorldSnapshot(WorldSnapshot snapshot)
    {
        if (!NetUtils.IsNewerTick(snapshot.ServerTick, LastServerTickProcessedByClient))
        {
            return;
        }



        LastClientTickProcessedByServer = snapshot.LastProcessedClientTick;
        LastServerTickProcessedByClient = snapshot.ServerTick;

        UnprocessedClientInputCommands.RemoveAll(cmd => cmd.ClientTick <= LastClientTickProcessedByServer);


        PickupManager.Instance.ApplyPickupMask(snapshot.PickupMask);

        float delta = (float)TickManager.Instance.ServerTickInterval;

        foreach (var playerSnapshot in snapshot.PlayerSnapshots)
        {
            if (MatchState.Instance.Players.TryGetValue(playerSnapshot.PlayerState.ID, out var player))
            {
                player.ApplySnapshot(playerSnapshot, delta);
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
        GameUI.Instance.PopulateInitialPlayerList();
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
        if (MatchState.Instance.Players.TryGetValue(playerNameChange.PlayerID, out var player))
        {
            player.SetName(playerNameChange.Name);
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
                GameUI.Instance.ShowPopup("SERVER SHUTDOWN", "The server was shut down", "Return to Main Menu", () => SceneNavigator.OpenMainMenu());
                NetworkClient.Instance.DisconnectFromServer();
                break;
        }
    }

    public void HandleChatMessage(ChatMessage chatMessage)
    {
        ApplyChatMessage(chatMessage.Info);
    }

    public void ApplyChatMessage(ChatMessageInfo info)
    {
        ChatManager.Instance.BroadcastChatMessageReceived(info);
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
        _messageHandlers[Msg.S2C_CHAT_MESSAGE] = payload => Dispatch<ChatMessage>(payload, HandleChatMessage);


    }
}
