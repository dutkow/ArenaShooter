using Godot;
using System;
using System.Runtime.ExceptionServices;

public enum WeaponType : byte
{
    WEAPON_1,
    WEAPON_2,
    WEAPON_3,
    WEAPON_4,
    WEAPON_5,
    WEAPON_6,
    WEAPON_7,
    WEAPON_8,
    WEAPON_9,
    WEAPON_10,
}


public partial class PlayerStateNew
{
    public PublicPlayerState PublicState;
    public PrivatePlayerState PrivateState;

    public void TickPlayerState()
    {
        if ((PublicState.IsAlive && PublicState.Character != null))
        {
            PublicState.Position = PublicState.Character.MovementComp.State.Position;
            PublicState.Yaw = PublicState.Character.MovementComp.State.Yaw;
            PublicState.Pitch = PublicState.Character.MovementComp.State.Pitch;
        }
    }
}

public partial class PublicPlayerState
{
    // Not replicated
    public Character Character;

    public byte PlayerID;
    public string PlayerName = "UNNAMED_PLAYER";

    public PublicPlayerFlags Flags;

    public ushort Kills;
    public ushort Deaths;

    public bool IsAlive;

    public Vector3 Position;
    public float Yaw;
    public float Pitch;

    public WeaponType EquippedWeapon;

    public void ApplyState(PublicPlayerState state)
    {
        if (state.Flags.HasFlag(PublicPlayerFlags.KILLS)) Kills = state.Kills;
        if (state.Flags.HasFlag(PublicPlayerFlags.DEATHS)) Deaths = state.Deaths;
        if (state.Flags.HasFlag(PublicPlayerFlags.IS_ALIVE)) IsAlive = state.IsAlive;
        if (state.Flags.HasFlag(PublicPlayerFlags.POSITION)) Position = state.Position;
        if (state.Flags.HasFlag(PublicPlayerFlags.YAW)) Yaw = state.Yaw;
        if (state.Flags.HasFlag(PublicPlayerFlags.PITCH)) Pitch = state.Pitch;
        if (state.Flags.HasFlag(PublicPlayerFlags.EQUIPPED_WEAPON)) EquippedWeapon = state.EquippedWeapon;
    }

    public CharacterMoveState GetMoveState()
    {
        CharacterMoveState moveState = new();
        moveState.Position = Position;
        //state.Velocity = Velocity;
        moveState.Yaw = Yaw;
        moveState.Pitch = Pitch;
        //state.MoveMode = MoveMode;
        return moveState;
    }
}

public partial class PrivatePlayerState()
{
    public PrivatePlayerFlags Flags;

    public byte Health;
    public byte MaxHealth;

    public byte Armor;
    public byte MaxArmor;

    public byte[] Ammo = new byte[10];

    public void ApplyState(PrivatePlayerState state)
    {

    }
}