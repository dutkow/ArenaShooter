using Godot;
using System;
using System.Net.NetworkInformation;

public struct PlayerSnapshot
{
    public PlayerFlags Flags;
    public PlayerState PlayerState;
    public CharacterState CharacterState;

    internal void Add(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Add(PlayerState.ID);

        msg.AddEnum(Flags);

        if (forceFull)
        {
            msg.Add(PlayerState.Ping);
            msg.Add(PlayerState.IsSpawned);
            msg.Add(PlayerState.Stats.Kills);
            msg.Add(PlayerState.Stats.Deaths);

            msg.Add(CharacterState.MoveState.Position);
            msg.Add(CharacterState.MoveState.Velocity);
            msg.Add(CharacterState.MoveState.Yaw);
            msg.Add(CharacterState.MoveState.Pitch);

            msg.Add(CharacterState.HealthState.Health);
            msg.Add(CharacterState.HealthState.MaxHealth);
            msg.Add(CharacterState.HealthState.Armor);
            msg.Add(CharacterState.HealthState.MaxArmor);

            msg.Add(CharacterState.InventoryState.EquippedWeaponIndex);
            msg.AddEnum(CharacterState.InventoryState.HeldWeaponsFlags);
            msg.AddEnum(CharacterState.InventoryState.AmmoChangedFlags);

            return;
        }

        if (Flags != PlayerFlags.NONE)
        {
            if((Flags & PlayerFlags.PLAYER_STATE_CHANGED) != 0)
            {
                if((PlayerState.Flags & PlayerStateFlags.PING_CHANGED) != 0)
                {
                    msg.Add(PlayerState.Ping);
                }

                if ((PlayerState.Flags & PlayerStateFlags.IS_SPAWNED_CHANGED) != 0)
                {
                    msg.Add(PlayerState.IsSpawned);
                }

                if ((PlayerState.Flags & PlayerStateFlags.STATS_CHANGED) != 0)
                {
                    if((PlayerState.StatFlags & PlayerStatFlags.KILLS_CHANGED) != 0)
                    {
                        msg.Add(PlayerState.Stats.Kills);
                    }

                    if ((PlayerState.StatFlags & PlayerStatFlags.DEATHS_CHANGED) != 0)
                    {
                        msg.Add(PlayerState.Stats.Deaths);
                    }
                }
            }

            if ((Flags & PlayerFlags.CHARACTER_STATE_CHANGED) != 0)
            {
                if ((CharacterState.Flags & CharacterStateFlags.MOVE_STATE_CHANGED) != 0)
                {
                    if((CharacterState.MoveState.Flags & CharacterMoveStateFlags.POSITION_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.MoveState.Position);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.VELOCITY_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.MoveState.Velocity);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.ROTATION_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.MoveState.Yaw);
                        msg.Add(CharacterState.MoveState.Pitch);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.HEALTH_STATE_CHANGED) != 0)
                {
                    if((CharacterState.HealthState.Flags & HealthStateFlags.HEALTH_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.HealthState.Health);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_HEALTH_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.HealthState.MaxHealth);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.ARMOR_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.HealthState.Armor);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_ARMOR_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.HealthState.MaxArmor);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.INVENTORY_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.EQUIPPED_WEAPON_CHANGED) != 0)
                    {
                        msg.Add(CharacterState.InventoryState.EquippedWeaponIndex);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.HELD_WEAPONS_CHANGED) != 0)
                    {
                        msg.AddEnum(CharacterState.InventoryState.HeldWeaponsFlags);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.AMMO_CHANGED) != 0)
                    {
                        msg.AddEnum(CharacterState.InventoryState.AmmoChangedFlags);
                    }
                }
            }
        }
    }

    internal void Write(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Write(PlayerState.ID);

        msg.WriteEnum(Flags);

        if (forceFull)
        {
            msg.Write(PlayerState.Ping);
            msg.Write(PlayerState.IsSpawned);
            msg.Write(PlayerState.Stats.Kills);
            msg.Write(PlayerState.Stats.Deaths);

            msg.Write(CharacterState.MoveState.Position);
            msg.Write(CharacterState.MoveState.Velocity);
            msg.Write(CharacterState.MoveState.Yaw);
            msg.Write(CharacterState.MoveState.Pitch);

            msg.Write(CharacterState.HealthState.Health);
            msg.Write(CharacterState.HealthState.MaxHealth);
            msg.Write(CharacterState.HealthState.Armor);
            msg.Write(CharacterState.HealthState.MaxArmor);

            msg.Write(CharacterState.InventoryState.EquippedWeaponIndex);
            msg.WriteEnum(CharacterState.InventoryState.HeldWeaponsFlags);
            msg.WriteEnum(CharacterState.InventoryState.AmmoChangedFlags);

            return;
        }

        if (Flags != PlayerFlags.NONE)
        {
            if ((Flags & PlayerFlags.PLAYER_STATE_CHANGED) != 0)
            {
                if ((PlayerState.Flags & PlayerStateFlags.PING_CHANGED) != 0)
                {
                    msg.Write(PlayerState.Ping);
                }

                if ((PlayerState.Flags & PlayerStateFlags.IS_SPAWNED_CHANGED) != 0)
                {
                    msg.Write(PlayerState.IsSpawned);
                }

                if ((PlayerState.Flags & PlayerStateFlags.STATS_CHANGED) != 0)
                {
                    if ((PlayerState.StatFlags & PlayerStatFlags.KILLS_CHANGED) != 0)
                    {
                        msg.Write(PlayerState.Stats.Kills);
                    }

                    if ((PlayerState.StatFlags & PlayerStatFlags.DEATHS_CHANGED) != 0)
                    {
                        msg.Write(PlayerState.Stats.Deaths);
                    }
                }
            }

            if ((Flags & PlayerFlags.CHARACTER_STATE_CHANGED) != 0)
            {
                if ((CharacterState.Flags & CharacterStateFlags.MOVE_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.POSITION_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.MoveState.Position);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.VELOCITY_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.MoveState.Velocity);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.ROTATION_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.MoveState.Yaw);
                        msg.Write(CharacterState.MoveState.Pitch);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.HEALTH_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.HealthState.Flags & HealthStateFlags.HEALTH_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.HealthState.Health);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_HEALTH_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.HealthState.MaxHealth);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.ARMOR_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.HealthState.Armor);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_ARMOR_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.HealthState.MaxArmor);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.INVENTORY_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.EQUIPPED_WEAPON_CHANGED) != 0)
                    {
                        msg.Write(CharacterState.InventoryState.EquippedWeaponIndex);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.HELD_WEAPONS_CHANGED) != 0)
                    {
                        msg.WriteEnum(CharacterState.InventoryState.HeldWeaponsFlags);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.AMMO_CHANGED) != 0)
                    {
                        msg.WriteEnum(CharacterState.InventoryState.AmmoChangedFlags);
                    }
                }
            }
        }
    }

    internal void Read(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Read(out PlayerState.ID);

        msg.ReadEnum(out Flags);

        if (forceFull)
        {
            // Player State
            msg.Read(out PlayerState.Ping);
            msg.Read(out PlayerState.IsSpawned);
            msg.Read(out PlayerState.Stats.Kills);
            msg.Read(out PlayerState.Stats.Deaths);

            // Character State
            msg.Read(out CharacterState.MoveState.Position);
            msg.Read(out CharacterState.MoveState.Velocity);
            msg.Read(out CharacterState.MoveState.Yaw);
            msg.Read(out CharacterState.MoveState.Pitch);

            msg.Read(out CharacterState.HealthState.Health);
            msg.Read(out CharacterState.HealthState.MaxHealth);
            msg.Read(out CharacterState.HealthState.Armor);
            msg.Read(out CharacterState.HealthState.MaxArmor);

            msg.Read(out CharacterState.InventoryState.EquippedWeaponIndex);
            msg.ReadEnum(out CharacterState.InventoryState.HeldWeaponsFlags);
            msg.ReadEnum(out CharacterState.InventoryState.AmmoChangedFlags);

            return;
        }

        if (Flags != PlayerFlags.NONE)
        {
            if ((Flags & PlayerFlags.PLAYER_STATE_CHANGED) != 0)
            {
                if ((PlayerState.Flags & PlayerStateFlags.PING_CHANGED) != 0)
                {
                    msg.Read(out PlayerState.Ping);
                }

                if ((PlayerState.Flags & PlayerStateFlags.IS_SPAWNED_CHANGED) != 0)
                {
                    msg.Read(out PlayerState.IsSpawned);
                }

                if ((PlayerState.Flags & PlayerStateFlags.STATS_CHANGED) != 0)
                {
                    if ((PlayerState.StatFlags & PlayerStatFlags.KILLS_CHANGED) != 0)
                    {
                        msg.Read(out PlayerState.Stats.Kills);
                    }

                    if ((PlayerState.StatFlags & PlayerStatFlags.DEATHS_CHANGED) != 0)
                    {
                        msg.Read(out PlayerState.Stats.Deaths);
                    }
                }
            }

            if ((Flags & PlayerFlags.CHARACTER_STATE_CHANGED) != 0)
            {
                if ((CharacterState.Flags & CharacterStateFlags.MOVE_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.POSITION_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.MoveState.Position);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.VELOCITY_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.MoveState.Velocity);
                    }

                    if ((CharacterState.MoveState.Flags & CharacterMoveStateFlags.ROTATION_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.MoveState.Yaw);
                        msg.Read(out CharacterState.MoveState.Pitch);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.HEALTH_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.HealthState.Flags & HealthStateFlags.HEALTH_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.HealthState.Health);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_HEALTH_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.HealthState.MaxHealth);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.ARMOR_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.HealthState.Armor);
                    }

                    if ((CharacterState.HealthState.Flags & HealthStateFlags.MAX_ARMOR_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.HealthState.MaxArmor);
                    }
                }

                if ((CharacterState.Flags & CharacterStateFlags.INVENTORY_STATE_CHANGED) != 0)
                {
                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.EQUIPPED_WEAPON_CHANGED) != 0)
                    {
                        msg.Read(out CharacterState.InventoryState.EquippedWeaponIndex);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.HELD_WEAPONS_CHANGED) != 0)
                    {
                        msg.ReadEnum(out CharacterState.InventoryState.HeldWeaponsFlags);
                    }

                    if ((CharacterState.InventoryState.Flags & InventoryStateFlags.AMMO_CHANGED) != 0)
                    {
                        msg.ReadEnum(out CharacterState.InventoryState.AmmoChangedFlags);
                    }
                }
            }
        }
    }
}