using Godot;
using System;
using System.Diagnostics.Metrics;

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

    public static void Send(string playerName)
    {
        var msg = new ConnectionRequest
        {
            MessageType = Msg.C2S_CONNECTION_REQUEST,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerName = playerName
        };
        NetworkSend.ToServer(msg);
                GD.Print($"Sending connection request to server w/ player name: {Settings.Instance.PlayerName}");

    }
}

/// <summary>
/// Sent from Server → Client when the connection request is accepted.
/// Includes assigned player ID and a list of currently connected player names.
/// </summary>
public class ConnectionAccepted : Message
{
    public byte AssignedPlayerID;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(AssignedPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(AssignedPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out AssignedPlayerID);
    }

    public static void Send(ENetPacketPeer client, byte assignedID)
    {
        var msg = new ConnectionAccepted
        {
            MessageType = Msg.S2C_CONNECTION_ACCEPTED,
            ENetFlags = ENetPacketFlags.Reliable,
            AssignedPlayerID = assignedID,
        };
        NetworkSend.ToClient(client, msg);
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
        NetworkSend.ToClient(client, msg);
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
        NetworkSend.ToServer(msg);
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
        NetworkSend.ToClient(client, msg);
    }
}

/// <summary>
/// Handler for all connection related messages.
/// </summary>

public class ConnectionMessageHandler : MessageHandler
{
    public override void Initialize(MessageRouter router, NetRole role)
    {
        GD.Print($"initializing connection message handler. Role = {role}");

        if (role == NetRole.SERVER)
        {
            // Peer → Server messages
            router.RegisterFromClient(Msg.C2S_CONNECTION_REQUEST, HandleConnectionRequest);
            router.RegisterFromClient(Msg.C2S_CLIENT_LOADED, HandleClientLoaded);
        }
        else if (role == NetRole.CLIENT)
        {
            // Server → Peer messages
            router.RegisterFromServer(Msg.S2C_CONNECTION_ACCEPTED, HandleConnectionAccepted);
            router.RegisterFromServer(Msg.S2C_CONNECTION_DENIED, HandleConnectionDenied);
            router.RegisterFromServer(Msg.S2C_INITIAL_MATCH_STATE, HandleInitialMatchState);
        }
    }

    // ----------------------------
    // Server-side handlers
    // ----------------------------

    private void HandleConnectionRequest(ENetPacketPeer peer, byte[] data)
    {
        var msg = new ConnectionRequest();
        msg.ReadMessage(data);

        NetworkSession.Instance.HandleConnectionRequest(peer, msg.PlayerName);

        GD.Print("Server received client connection request");
    }

    private void HandleClientLoaded(ENetPacketPeer peer, byte[] data)
    {
        var msg = new ClientLoaded();
        msg.ReadMessage(data);
        GD.Print($"Client loaded: PlayerID {msg.PlayerID}");

        // Send initial match state to the client
        //byte[] playerIDs = NetworkSession.Instance.GetAllPlayerIDs();
        //Vector3[] positions = NetworkSession.Instance.GetAllPlayerPositions();
        //int[] health = NetworkSession.Instance.GetAllPlayerHealth();

        //InitialMatchState.Send(peer, playerIDs, positions, health);
    }

    // ----------------------------
    // Client-side handlers
    // ----------------------------

    private void HandleConnectionAccepted(byte[] data)
    {
        var msg = new ConnectionAccepted();
        msg.ReadMessage(data);

        GD.Print("Client received connection accepted from server");
        // Save player ID locally
        //Settings.Instance.PlayerID = msg.AssignedPlayerID;
    }

    private void HandleConnectionDenied(byte[] data)
    {
        var msg = new ConnectionDenied();
        msg.ReadMessage(data);

        GD.Print("Client received connection denial from server");
    }

    private void HandleInitialMatchState(byte[] data)
    {
        var msg = new InitialMatchState();
        msg.ReadMessage(data);

        GD.Print("Client received initial match state from server");

        // Initialize local game state with received data
        //GameSession.Instance.InitializeMatchState(msg.PlayerIDs, msg.Positions, msg.Health);
    }
}
