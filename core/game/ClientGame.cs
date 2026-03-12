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

    // REFACTOR CODE
    public void AssignPlayerStateNew(PlayerStateNew playerState)
    {
        LocalPlayerStateNew = playerState;
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

        var characterSnapshots = snapshot.GetCharacterSnapshots();

        UnprocessedClientInputs.RemoveAll(cmd => cmd.ClientTick <= LastClientTickProcessedByServer);

        PickupManager.Instance.ApplyPickupMask(snapshot.PickupMask);

        for (int i = 0; i < characterSnapshots.Length; ++i)
        {
            var characterSnapshot = characterSnapshots[i];



            // REFACTOR CODE
            if (MatchState.Instance.NewConnectedPlayers.TryGetValue(characterSnapshot.PlayerID, out var playerStateNew))
            {
                Character character = playerStateNew.PublicState.Character;
                if (character != null)
                {
                    character.ApplySnapshot(characterSnapshot);
                }
                else
                {
                    SpawnManager.Instance.NewLocalSpawnPlayer(characterSnapshot.PlayerID, characterSnapshot.Position, characterSnapshot.Yaw);
                }
            }
            //////////////
        }

        ClientProjectileManager.Instance.ApplyWorldSnapshot(snapshot);
    }
}
