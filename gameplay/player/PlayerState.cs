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


        msg.Add(PlayerInfo.PlayerName);

        msg.Add(Stats.Kills);
        msg.Add(Stats.Deaths);

        msg.Add(Ping);
        msg.Add(IsSpawned);

        CharacterPublicState.Add(msg, forceFull);
        CharacterPrivateState.Add(msg, forceFull);
    }

    internal void Write(Message msg, byte clientPlayerID)
    {
        msg.Write(PlayerInfo.PlayerID);

        msg.Write(PlayerInfo.PlayerName);

        msg.Write(Stats.Kills);
        msg.Write(Stats.Deaths);

        msg.Write(Ping);
        msg.Write(IsSpawned);

        CharacterPublicState.Write(msg);
        CharacterPrivateState.Write(msg);
    }

    internal void Read(Message msg, byte clientPlayerID)
    {
        msg.Read(out PlayerInfo.PlayerID);


        msg.Read(out PlayerInfo.PlayerName);

        msg.Read(out Stats.Kills);
        msg.Read(out Stats.Deaths);
        msg.Read(out Ping);
        msg.Read(out IsSpawned);

        CharacterPublicState.Read(msg);
        CharacterPrivateState.Read(msg);
    }
}