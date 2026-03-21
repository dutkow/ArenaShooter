using Godot;
using System;


[Flags]
public enum PlayerFlags : byte // 8 values
{
    NONE = 0,

    PLAYER_STATE_CHANGED = 1 << 0,
    MOVE_STATE_CHANGED = 1 << 1,
    HEALTH_STATE_CHANGED = 1 << 2,
    INVENTORY_STATE_CHANGED = 1 << 3,
}

public struct PlayerSnapshot
{
    public PlayerState PlayerState;
    public CharacterMoveState CharacterMoveState;
    public HealthState HealthState;
    public InventoryState InventoryState;
}

[Flags]
public enum PlayerStateFlags : byte // 8 values
{
    NONE = 0,

    PING_CHANGED = 1 << 0,
    IS_SPAWNED_CHANGED = 1 << 1,
    STATS_CHANGED = 1 << 2,
}

[Flags]
public enum PlayerStatFlags : byte // 8 values
{
    NONE = 0,

    KILLS_CHANGED = 1 << 0,
    DEATHS_CHANGED = 1 << 1,
}

public struct PlayerState
{
    public byte ID; // not changed
    public string Name; // only changed via reliable updates outside of state transmission

    public PlayerStateFlags Flags;
    public ushort Ping;
    public bool IsSpawned;

    public PlayerStatFlags StatFlags;
    public PlayerStats Stats;
}

public class Player
{
    PlayerState State;
    Character Character;
    InventoryManager InventoryManager;

    public Action<string> NameChanged;
    public Action<int> PingChanged;
    public Action<bool> IsSpawnedChanged;

    public Action<int> KillsChanged;
    public Action<int> DeathsChanged;

    public static void Create(PlayerInfo playerInfo)
    {
        Player player = new();
        player.State.ID = playerInfo.PlayerID;
        player.SetID(playerInfo.PlayerID);
        MatchState.Instance.AddPlayer(playerInfo);
    }

    public void SetID(byte id)
    {
        State.ID = id; // not dynamic, no != check needed
    }

    public void SetName(string name)
    {
        if (State.Name != name)
        {
            State.Name = name;
            NameChanged?.Invoke(name);
        }
    }

    public void SetPing(int ping)
    {
        if (State.Ping != (ushort)ping)
        {
            State.Ping = (ushort)ping;
            PingChanged?.Invoke(ping);

            State.Flags |= PlayerStateFlags.PING_CHANGED;
        }
    }

    public void SetIsSpawned(bool isSpawned)
    {
        if (State.IsSpawned != isSpawned)
        {
            State.IsSpawned = isSpawned;
            IsSpawnedChanged?.Invoke(isSpawned);

            if (isSpawned)
            {
                State.Flags |= PlayerStateFlags.IS_SPAWNED_CHANGED;
            }
            else
            {
                State.Flags &= ~PlayerStateFlags.IS_SPAWNED_CHANGED;
            }
        }
    }

    public void AddKill()
    {
        State.Stats.Kills++;

        KillsChanged?.Invoke(State.Stats.Kills);
    }

    public void SubtractKill() // i.e., penalty for suicide or team kill
    {
        State.Stats.Kills--;

        KillsChanged?.Invoke(State.Stats.Kills);
    }

    public void AddDeath()
    {
        State.Stats.Deaths++;

        DeathsChanged?.Invoke(State.Stats.Deaths);
    }

    public void HandleSpawn(Character character)
    {
        SetIsSpawned(true);

        Character = character;
        Character.OnSpawned();

        InventoryManager.OnSpawned();
    }

    public void OnDeath()
    {
        SetIsSpawned(false);
        Character = null;
    }

    public PlayerSnapshot GetPlayerSnapshot()
    {
        PlayerSnapshot snapshot = new();

        if(State.Flags != 0)
        {
            snapshot.PlayerState = State;
        }

        if(Character != null)
        {
        }

        return default;
    }
}