using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ClientGame
{
    public static ClientGame Instance { get; private set; }

    // Player elements
    public byte LocalPlayerID { get; private set; }
    public PlayerController LocalPlayerController { get; private set; }
    public PlayerState LocalPlayerState { get; private set; }
    public PlayerStateNew LocalPlayerStateNew { get; private set; }

    public Pawn LocalPlayerPawn => LocalPlayerController.PossessedPawn;

    // Client-server synchronization
    public ushort LastServerTickProcessedByClient;
    public ushort LastClientTickProcessedByServer;
    public List<ClientInputCommand> UnprocessedClientInputs = new();
    const int REDUNDANT_INPUTS = 4;


    public void Initialize(byte localPlayerID)
    {
        Instance = this;

        LocalPlayerID = localPlayerID;
        LocalPlayerController = new();
        Game.Instance.AddChild(LocalPlayerController);

        GD.Print($"Starting client. NetworkMode = {NetworkSession.Instance.NetworkMode}");

        ClientProjectileManager.Create();
    }

    public void Tick()
    {
        var cmd = GetClientInputCommand(); // client generates input and applies it locally

        LocalPlayerController?.ApplyInput(cmd);
        LocalPlayerPawn?.ApplyInput(cmd);

        if(!NetworkSession.Instance.IsListenServer)
        {
            SendClientCommand(cmd);
        }
    }

    public void AssignPlayerState(PlayerState playerState)
    {
        LocalPlayerState = playerState;
    }


    public void SendClientCommand(ClientInputCommand cmd)
    {
        UnprocessedClientInputs.Add(cmd);
        var commandsToSend = UnprocessedClientInputs.Skip(Math.Max(0, UnprocessedClientInputs.Count - REDUNDANT_INPUTS)).ToArray();
        ClientCommand.Send(commandsToSend);
    }

    public ClientInputCommand GetClientInputCommand()
    {
        var cmd = new ClientInputCommand();

        //cmd = LocalPlayerController.AddInput(cmd);

        if (LocalPlayerPawn != null)
        {
            cmd = LocalPlayerPawn.AddInput(cmd);
        }

        cmd.ClientTick = MatchState.Instance.CurrentTick;

        return cmd;
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        if (!NetUtils.IsNewerTick(snapshot.ServerTick, LastServerTickProcessedByClient))
        {
            return;
        }

        LastClientTickProcessedByServer = snapshot.LastProcessedClientTick;
        LastServerTickProcessedByClient = snapshot.ServerTick;

        UnprocessedClientInputs.RemoveAll(cmd => cmd.ClientTick <= LastClientTickProcessedByServer);

        PickupManager.Instance.ApplyPickupMask(snapshot.PickupMask);

        foreach (var playerState in snapshot.PlayerStates)
        {
            if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerState.PlayerID, out var foundPlayerState))
            {

                // Client already has an instance of this character, apply snapshot, which could also kill it if it's not alive
                Character character = foundPlayerState.Character;
                if (character != null)
                {
                    character.ApplyPublicState(foundPlayerState.CharacterPublicState);

                    if(foundPlayerState.PlayerID == ClientGame.Instance.LocalPlayerID)
                    {
                        character.ApplyPrivateState(foundPlayerState.CharacterPrivateState);
                    }
                }
                // Client doesn't know about this character but it's alive, spawn it
                else if(playerState.IsAlive)
                {
                    GD.Print($"CLIENT SNAPSHOT:client wants to spawn player. player ID: {playerState.PlayerID}. position:  {playerState.CharacterPublicState.Position}");
                    SpawnManager.Instance.LocalSpawnPlayer(playerState.PlayerID, playerState.CharacterPublicState.Position, playerState.CharacterPublicState.Rotation.X);
                }
            }
        }

        ClientProjectileManager.Instance.ApplyWorldSnapshot(snapshot);
    }
}
