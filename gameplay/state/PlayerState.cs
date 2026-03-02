using Godot;
using System;


public class PlayerState (int playerID)
{
    public int PlayerID = playerID;
    public string PlayerName;
    public int Score;
    public int Health;
    public int Shields;
    public int Ammo;
    public int TeamId;
    public PlayerCharacter Character;
    public bool IsAlive => Character != null && Character.IsAlive;
}