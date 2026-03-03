using Godot;
using System;


public class PlayerState (byte playerID)
{
    public byte PlayerID = playerID;
    public string PlayerName;
    public int Score;
    public int Health;
    public int Shields;
    public int Ammo;
    public int TeamId;
    public ArenaCharacter Character;
    public bool IsAlive => Character != null && Character.IsAlive;
}