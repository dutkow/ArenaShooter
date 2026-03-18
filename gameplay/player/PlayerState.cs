using Godot;
using System;


public class PlayerState()
{
    public PlayerInfo PlayerInfo;
 
    public Character Character; // instance, not replicated

    public CharacterPublicState CharacterPublicState = new();
    public CharacterPrivateState CharacterPrivateState = new();

    public PlayerStateFlags Flags;

    public byte Kills;
    public byte Deaths;
    public ushort Ping;
    public bool IsAlive; // Used so clients know who they need to spawn

    public Action<string> PlayerNameChanged;

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

            msg.Add(Kills);
            msg.Add(Deaths);
            msg.Add(Ping);
            msg.Add(IsAlive);


        }
        else
        {
            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Add(Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Add(Deaths);
            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Add(Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Add(IsAlive);
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

            msg.Write(Kills);
            msg.Write(Deaths);
            msg.Write(Ping);
            msg.Write(IsAlive);
        }
        else
        {
            msg.WriteEnum(Flags);

            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Write(Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Write(Deaths);
            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Write(Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Write(IsAlive);
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

            msg.Read(out Kills);
            msg.Read(out Deaths);
            msg.Read(out Ping);
            msg.Read(out IsAlive);

        }
        else
        {
            msg.ReadEnum(out Flags);

            if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Read(out Kills);
            if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Read(out Deaths);
            if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Read(out Ping);
            if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Read(out IsAlive);
        }
        CharacterPublicState.Read(msg, forceFull);
        CharacterPrivateState.Read(msg, forceFull);
    }
}