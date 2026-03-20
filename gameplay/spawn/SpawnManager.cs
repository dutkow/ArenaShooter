using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class SpawnManager : Singleton<SpawnManager>
{
    public List<SpawnPoint> _playerSpawnPoints = new();


    public void RegisterSpawnPoint(SpawnPoint spawnPoint)
    {
        switch(spawnPoint.Type)
        {
            case SpawnPointType.PLAYER:
                _playerSpawnPoints.Add(spawnPoint);
                break;
        }
    }

    public SpawnPoint GetSpawnPoint()
    {
        return ListUtils.RandomElement(_playerSpawnPoints);
    }

    public Character ServerSpawnPlayer(byte playerID)
    {
        var spawnPoint = GetSpawnPoint();
        if(MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var player))
        {
            player.IsSpawned = true;

            player.CharacterPrivateState.HeldWeaponsFlags = 0;

            player.CharacterPrivateState.HeldWeaponsFlags = WeaponFlags.NONE;

            var startingWeapons = GameRules.Instance.StartingWeapons;
            foreach(var startingWeapon in startingWeapons)
            {
                player.CharacterPrivateState.HeldWeaponsFlags |= (WeaponFlags)(1 << (startingWeapon.WeaponIndex));

                int ammoOverride = startingWeapon.AmmoOverride;
                if (ammoOverride > -1)
                {
                    player.CharacterPrivateState.Ammo[startingWeapon.WeaponIndex] = (byte)ammoOverride;
                }
                else
                {
                    WeaponData weaponData = GameRules.Instance.Weapons[startingWeapon.WeaponIndex];
                    if(weaponData == null)
                    {
                        GD.PushError($"Weapon data is null for weapon index: {startingWeapon.WeaponIndex}");
                        return player.Character;
                    }
                    player.CharacterPrivateState.Ammo[startingWeapon.WeaponIndex] = (byte)weaponData.DefaultStartingAmmo;
                }
            }
        }
        Character spawnedPlayer = LocalSpawnPlayer(playerID, spawnPoint.GlobalPosition, spawnPoint.GlobalRotation.Y);

        player.Flags |= PlayerStateFlags.IS_ALIVE_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.ROTATION_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.VELOCITY_CHANGED;
        player.CharacterPublicState.Flags |= CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED;

        player.CharacterPublicState.Position = spawnPoint.GlobalPosition;
        player.CharacterPublicState.Yaw = spawnPoint.GlobalRotation.Y;

       

        return spawnedPlayer;
    }


    public Character LocalSpawnPlayer(byte playerID, Vector3 spawnPosition, float yRotation)
    {
        if(ClientGame.Instance == null)
        {
            GD.Print($"client game is null");
        }
        var spawnedPlayer = (Character)GameRules.Instance.DefaultPawnScene.Instantiate();

        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.Character = spawnedPlayer;
        }

        Level.Instance.AddChild(spawnedPlayer);

        spawnedPlayer.HandleSpawn(spawnPosition, yRotation, 0.0f);
        spawnedPlayer.SetIsAuthority(NetworkManager.Instance.IsServer);

        if (playerID == ClientGame.Instance.LocalPlayerID)
        {
            ClientGame.Instance.LocalPlayerController.Possess(spawnedPlayer);
            GD.Print($"running POSSESS on player id: {playerID} on {NetworkManager.Instance.NetworkMode}");
        }
        else
        {
            GD.Print($"handling remote spawn on {NetworkManager.Instance.NetworkMode}");
            spawnedPlayer.HandleRemoteSpawn(playerID);
        }

        spawnedPlayer.Initialize(playerState);


        return spawnedPlayer;
    }
}
