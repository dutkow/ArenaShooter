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
    public Pawn Pawn;
    public bool IsAlive => Pawn is IDamageable damageable && damageable.IsAlive();

    public void AssignPawn(Pawn pawn)
    {
        GD.Print($"Assigning player state to character");

        Pawn = pawn;
        pawn.PlayerState = this;
    }
}