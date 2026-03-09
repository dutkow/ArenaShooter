using Godot;
using System;

public class ClientGame
{
    public static ClientGame Instance { get; private set; }

    // Player elements
    public byte LocalPlayerID { get; private set; }
    public PlayerController LocalPlayerController { get; private set; }
    public PlayerState LocalPlayerState { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;


    // Server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;

    public void Initialize(byte localPlayerID)
    {
        Instance = this;

        LocalPlayerID = localPlayerID;
    }

    

}
