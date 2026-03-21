using Godot;
using System;

public class Player
{
    PlayerState State;
    Character Character;
    WeaponManager WeaponManager;

    public Action<string> NameChanged;
    public Action<int> PingChanged;
    public Action<bool> IsSpawnedChanged;

    public Action<int> KillsChanged;
    public Action<int> DeathsChanged;

    public static void Create(PlayerInfo playerInfo)
    {
        Player player = new();
        player.SetID(playerInfo.PlayerID);
        MatchState.Instance.AddPlayer(playerInfo);
    }

    public void SetID(byte id)
    {
        if (State.ID != id)
        {
            State.ID = id;
        }
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
                State.Flags |= PlayerStateFlags.IS_SPAWNED;
            }
            else
            {
                State.Flags &= ~PlayerStateFlags.IS_SPAWNED;
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

        WeaponManager.OnSpawned();
    }

    public void OnDeath()
    {
        SetIsSpawned(false);
        Character = null;
    }
}