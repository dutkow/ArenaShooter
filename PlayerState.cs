using Godot;
using System;

public enum PlayerStatus
{
    CONNECTED,   // Player has connected, but has not spawned yet
    ACTIVE,      // Player is spawned in the world and playing
    SPECTATOR,   // Player is watching the match
    DISCONNECTED, // Player has left or timed out
}

public class PlayerState
{
    public string PlayerName;
    public int Score;
    public int Health;
    public int Shields;
    public int Ammo;
    public int TeamId;
    public PlayerStatus Status;
    public PlayerCharacter Pawn;
}