using Godot;
using System;
using System.Collections.Generic;

public class PickupManager
{
    public List<Pickup> Pickups = new();
    byte _nextPickupID = 0; // MAX 256

    public ulong PickupMask { get; private set; }

    public static PickupManager Instance { get; private set; }

    public static void Create()
    {
        if (Instance != null)
        {
            throw new Exception("Pickup manager already exists!");
        }

        Instance = new PickupManager();
    }

    public void RegisterPickup(Pickup pickup)
    {
        Pickups.Add(pickup);
        pickup.SetPickupID(_nextPickupID);
        SetPickupState(_nextPickupID, pickup.IsSpawned);
        _nextPickupID++;
    }

    public void SetPickupState(byte id, bool spawned)
    {
        if (spawned)
        {
            PickupMask |= 1UL << id;
        }
        else
        {
            PickupMask &= ~(1UL << id);
        }
    }

    public void ApplyPickupMask(ulong newMask)
    {
        foreach(var pickup in Pickups)
        {
            pickup.SetIsSpawned((newMask & (1UL << pickup.PickupID)) != 0);
        }
    }
}
