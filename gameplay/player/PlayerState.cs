using Godot;
using System;


public struct PlayerStats
{
    public ushort Kills;
    public ushort Deaths;
}

public class PlayerState()
{
    public PlayerInfo PlayerInfo;

    public CharacterPublicState CharacterPublicState = new();
    public CharacterPrivateState CharacterPrivateState = new();

    public PlayerStateFlags Flags;

    public PlayerStats Stats;

    public ushort Ping;
    public bool IsSpawned; // Used so clients know who they need to spawn

    public Action<string> PlayerNameChanged;
    public Character Character; // instance, not replicated

    public void SetPlayerName(string playerName)
    {
        PlayerInfo.PlayerName = playerName;
        PlayerNameChanged?.Invoke(playerName);
    }

    public void ClearFlags()
    {
        Flags = 0;
        if (Character != null)
        {
            Character.ClearFlags();
        }
    }

    internal void Add(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Add(PlayerInfo.PlayerID);

        msg.AddEnum(Flags);

        if (forceFull)
        {
            msg.Add(PlayerInfo.PlayerName);

            msg.Add(Stats.Kills);
            msg.Add(Stats.Deaths);

            msg.Add(Ping);
            msg.Add(IsSpawned);


        }
        else
        {
            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Add(Stats.Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Add(Stats.Deaths);
            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Add(Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Add(IsSpawned);
        }

        CharacterPublicState.Add(msg, forceFull);
        CharacterPrivateState.Add(msg, forceFull);
    }

    internal void Write(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Write(PlayerInfo.PlayerID);

        if (forceFull)
        {
            msg.Write(PlayerInfo.PlayerName);

            msg.Write(Stats.Kills);
            msg.Write(Stats.Deaths);

            msg.Write(Ping);
            msg.Write(IsSpawned);
        }
        else
        {
            msg.WriteEnum(Flags);

            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Write(Stats.Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Write(Stats.Deaths);

            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Write(Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Write(IsSpawned);
        }

        CharacterPublicState.Write(msg, forceFull);
        CharacterPrivateState.Write(msg, forceFull);
    }

    internal void Read(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Read(out PlayerInfo.PlayerID);

        if (forceFull)
        {
            msg.Read(out PlayerInfo.PlayerName);

            msg.Read(out Stats.Kills);
            msg.Read(out Stats.Deaths);
            msg.Read(out Ping);
            msg.Read(out IsSpawned);

        }
        else
        {
            msg.ReadEnum(out Flags);

            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Read(out Stats.Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Read(out Stats.Deaths);

            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Read(out Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Read(out IsSpawned);
        }
        CharacterPublicState.Read(msg, forceFull);
        CharacterPrivateState.Read(msg, forceFull);
    }
}