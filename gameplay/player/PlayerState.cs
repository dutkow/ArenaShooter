using Godot;
using System;


public class PlayerState()
{
    public byte PlayerID;
    public string PlayerName;
    public Character Character; // instance, not replicated

    public PlayerStateFlags Flags;

    public byte Kills;
    public byte Deaths;
    public ushort Ping;
    public bool IsAlive; // Used so clients know who they need to spawn

    public void ClearFlags()
    {
        Flags = 0;
        if (Character != null)
        {
            Character.ClearFlags();
        }
    }

    internal void Add(Message msg)
    {
        msg.Add(PlayerID);

        msg.AddEnum(Flags);

        if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Add(Kills);
        if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Add(Deaths);
        if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Add(Ping);
        if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Add(IsAlive);
    }

    internal void Write(Message msg)
    {
        msg.Write(PlayerID);

        msg.WriteEnum(Flags);

        if ((Flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Write(Kills);
        if ((Flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Write(Deaths);
        if ((Flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Write(Ping);
        if ((Flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Write(IsAlive);
    }
    internal static PlayerState Read(Message msg)
    {
        var state = new PlayerState();
        msg.Read(out state.PlayerID);

        msg.ReadEnum(out state.Flags);

        var flags = state.Flags;
        if ((flags & PlayerStateFlags.KILLS_CHANGED) != 0) msg.Read(out state.Kills);
        if ((flags & PlayerStateFlags.DEATHS_CHANGED) != 0) msg.Read(out state.Deaths);
        if ((flags & PlayerStateFlags.PING_CHANGED) != 0) msg.Read(out state.Ping);
        if ((flags & PlayerStateFlags.IS_ALIVE_CHANGED) != 0) msg.Read(out state.IsAlive);

        return state;
    }
}