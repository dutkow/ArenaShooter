using Godot;
using System;

/// <summary>
/// Sent from Client → Server to request joining the game.
/// </summary>
public class ConnectionRequest : Message
{
    public string PlayerName;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerName);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerName);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerName);
    }

    public static void Send(ENetPacketPeer server, string playerName)
    {
        var msg = new ConnectionRequest
        {
            MessageType = Msg.C2S_CONNECTION_REQUEST,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerName = playerName
        };
        NetworkMessenger.ToServer(server, msg);
    }
}

/// <summary>
/// Sent from Server → Client when the connection request is accepted.
/// Includes assigned player ID and a list of currently connected player names.
/// </summary>
public class ConnectionAccepted : Message
{
    public byte AssignedPlayerID;
    public string[] PlayerNames;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(AssignedPlayerID);
        Add(PlayerNames);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(AssignedPlayerID);
        Write(PlayerNames);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out AssignedPlayerID);
        Read(out PlayerNames);
    }

    public static void Send(ENetPacketPeer client, byte assignedID, string[] playerNames)
    {
        var msg = new ConnectionAccepted
        {
            MessageType = Msg.S2C_CONNECTION_ACCEPTED,
            ENetFlags = ENetPacketFlags.Reliable,
            AssignedPlayerID = assignedID,
            PlayerNames = playerNames
        };
        NetworkMessenger.ToClient(client, msg);
    }
}

/// <summary>
/// Sent from Server → Client when the connection request is denied.
/// Includes a reason for denial.
/// </summary>
public class ConnectionDenied : Message
{
    public string Reason;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Reason);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Reason);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Reason);
    }

    public static void Send(ENetPacketPeer client, string reason)
    {
        var msg = new ConnectionDenied
        {
            MessageType = Msg.S2C_CONNECTION_DENIED,
            ENetFlags = ENetPacketFlags.Reliable,
            Reason = reason
        };
        NetworkMessenger.ToClient(client, msg);
    }
}

/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class ClientLoaded : Message
{
    public byte PlayerID;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerID);
    }

    public static void Send(ENetPacketPeer server, byte playerID)
    {
        var msg = new ClientLoaded
        {
            MessageType = Msg.C2S_CLIENT_LOADED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID
        };
        NetworkMessenger.ToServer(server, msg);
    }
}

/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, health, and any other relevant starting state.
/// </summary>
public class InitialMatchState : Message
{
    public byte[] PlayerIDs;
    public Vector3[] Positions;
    public int[] Health;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerIDs.Length);
        foreach (var id in PlayerIDs)
        {
            Add(id);
        }

        Add(Positions.Length);
        foreach (var pos in Positions)
        {
            Add(pos);
        }

        Add(Health.Length);
        foreach (var hp in Health)
        {
            Add(hp);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(PlayerIDs.Length);
        foreach (var id in PlayerIDs)
        {
            Write(id);
        }

        Write(Positions.Length);
        foreach (var pos in Positions)
        {
            Write(pos);
        }

        Write(Health.Length);
        foreach (var hp in Health)
        {
            Write(hp);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        int count;
        Read(out count);
        PlayerIDs = new byte[count];
        for (int i = 0; i < count; i++)
        {
            Read(out PlayerIDs[i]);
        }

        Read(out count);
        Positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Positions[i]);
        }

        Read(out count);
        Health = new int[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Health[i]);
        }
    }

    public static void Send(ENetPacketPeer client, byte[] playerIDs, Vector3[] positions, int[] health)
    {
        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            Positions = positions,
            Health = health
        };
        NetworkMessenger.ToClient(client, msg);
    }
}

/// <summary>
/// Handler for all connection related messages.
/// </summary>

public class ServerConnectionMessageHandler : MessageHandler
{
    public ServerConnectionMessageHandler(MessageRouter router) : base(router) { }

    public override void Initialize(NetRole role)
    {
        if (role != NetRole.SERVER) return;

        // Register handlers for messages coming from clients
        Router.RegisterFromClient(ClientMsg.CONNECTION_REQUEST, HandleConnectionRequest);
        Router.RegisterFromClient(ClientMsg.CLIENT_LOADED, HandleClientLoaded);
        // Add more as needed:
        // Router.RegisterFromClient(ClientMsg.PLAYER_INFO, HandlePlayerInfo);
    }

    private void HandleConnectionRequest(ENetPacketPeer sender, byte[] data)
    {
        // Parse ConnectionRequest and handle new client
    }

    private void HandleClientLoaded(ENetPacketPeer sender, byte[] data)
    {
        // Handle client signaling that it's loaded
    }
}

public class ClientConnectionMessageHandler : MessageHandler
{
    public ClientConnectionMessageHandler(MessageRouter router) : base(router) { }

    public override void Initialize(NetRole role)
    {
        if (role != NetRole.CLIENT) return;

        // Register handlers for messages coming from server
        Router.RegisterFromServer(ServerMsg.CONNECTION_ACCEPTED, HandleConnectionAccepted);
        Router.RegisterFromServer(ServerMsg.CONNECTION_DENIED, HandleConnectionDenied);
        Router.RegisterFromServer(ServerMsg.INITIAL_MATCH_STATE, HandleInitialMatchState);
        // Add more as needed:
        // Router.RegisterFromServer(ServerMsg.PLAYER_JOINED, HandlePlayerJoined);
    }

    private void HandleConnectionAccepted(byte[] data)
    {
        // Parse and handle server accepting connection
    }

    private void HandleConnectionDenied(byte[] data)
    {
        // Parse and handle server denying connection
    }

    private void HandleInitialMatchState(byte[] data)
    {
        // Parse initial match state
    }
}