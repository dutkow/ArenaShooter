using Godot;
using System;



[Flags]
public enum PlayerFlags : byte // 8 values
{
    NONE = 0,

    PLAYER_STATE_CHANGED = 1 << 0,
    CHARACTER_STATE_CHANGED = 1 << 1,
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
    public PlayerState State;
    public Character Character;

    public Action<string> NameChanged;
    public Action<int> PingChanged;
    public Action<bool> IsSpawnedChanged;

    public Action<int> KillsChanged;
    public Action<int> DeathsChanged;

    public Action Joined;
    public Action Left;

    public static Player Create(PlayerInfo playerInfo)
    {
        Player player = new();
        player.State.ID = playerInfo.PlayerID;
        player.SetID(playerInfo.PlayerID);
        player.SetName(playerInfo.PlayerName);

        return player;
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

            State.Flags |= PlayerStateFlags.IS_SPAWNED_CHANGED;
        }

        GD.Print($" set is spawned ran, and now is spawned = {isSpawned}");
    }

    public void AddKill()
    {
        SetKills(State.Stats.Kills + 1);
    }

    public void SubtractKill() // i.e., penalty for suicide or team kill
    {
        SetKills(State.Stats.Kills - 1);
    }

    public void SetKills(int kills, bool markDirty = true)
    {
        if(State.Stats.Kills != (byte)kills)
        {
            State.Stats.Kills = (byte)kills;
            KillsChanged?.Invoke(State.Stats.Kills);

            if(markDirty)
            {
                State.Flags |= PlayerStateFlags.STATS_CHANGED;
                State.StatFlags |= PlayerStatFlags.KILLS_CHANGED;
            }
        }
    }

    public void AddDeath()
    {
        SetDeaths(State.Stats.Deaths + 1);
    }

    public void SetDeaths(int deaths, bool markDirty = true)
    {
        if (State.Stats.Deaths != (byte)deaths)
        {
            State.Stats.Deaths = (byte)deaths;
            DeathsChanged?.Invoke(State.Stats.Deaths);

            if (markDirty)
            {
                State.Flags |= PlayerStateFlags.STATS_CHANGED;
                State.StatFlags |= PlayerStatFlags.DEATHS_CHANGED;
            }
        }
    }

    public void HandleSpawn(Character character)
    {
        SetIsSpawned(true);

        Character = character;
        Character.OnSpawned();

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
            snapshot.Flags |= PlayerFlags.PLAYER_STATE_CHANGED;
        }

        if (State.IsSpawned && Character != null && Character.State.Flags != 0)
        {
            snapshot.CharacterState = Character.State;
            snapshot.Flags |= PlayerFlags.CHARACTER_STATE_CHANGED;
        }

        return snapshot;
    }

    public void ApplySnapshot(PlayerSnapshot snapshot, float delta)
    {
        if (snapshot.Flags == 0)
        {
            return;
        }

        // Player State 
        if ((snapshot.Flags & PlayerFlags.PLAYER_STATE_CHANGED) != 0)
        {
            if ((snapshot.PlayerState.Flags & PlayerStateFlags.PING_CHANGED) != 0)
            {
                SetPing(snapshot.PlayerState.Ping);
            }

            if ((snapshot.PlayerState.Flags & PlayerStateFlags.IS_SPAWNED_CHANGED) != 0)
            {
                SetIsSpawned(snapshot.PlayerState.IsSpawned);
            }

            // Stats
            if ((snapshot.PlayerState.Flags & PlayerStateFlags.STATS_CHANGED) != 0)
            {
                if ((snapshot.PlayerState.StatFlags & PlayerStatFlags.KILLS_CHANGED) != 0)
                {
                    SetKills(snapshot.PlayerState.Stats.Kills, false);
                }

                if ((snapshot.PlayerState.StatFlags & PlayerStatFlags.DEATHS_CHANGED) != 0)
                {
                    SetDeaths(snapshot.PlayerState.Stats.Deaths, false);
                }
            }
        }
        // Character state 
        if (Character != null)
        {
            if ((snapshot.Flags & PlayerFlags.CHARACTER_STATE_CHANGED) != 0)
            {
                Character.ApplyState(snapshot.CharacterState, delta);
            }
        }
    }
}