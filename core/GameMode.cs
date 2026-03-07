using Godot;
using Godot.Collections;
using System;

public partial class GameMode : Node
{
    public static GameMode Instance { get; private set; }

    [Export] public PackedScene DefaultPawnScene;

    [Export] public PackedScene MatchStateScene;

    [Export] public PackedScene LevelUIScene;

    public System.Collections.Generic.Dictionary<byte, PlayerController> PlayerControllers = new();

    public PlayerController LocalPlayerController => PlayerControllers[NetworkSession.Instance.LocalPlayerID];

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }

    public void Initialize()
    {
        MatchState.Instance.Initialize();
        MatchState.Instance.MatchPhaseChanged += OnMatchPhaseChanged;
    }

    private void OnMatchPhaseChanged(MatchPhase phase)
    {
        switch (phase)
        {
            case MatchPhase.WARMUP:
                HandleWarmup();
                break;

            case MatchPhase.PRE_MATCH:
                HandlePreMatch();
                break;

            case MatchPhase.MATCH:
                HandleMatchStart();
                break;

            case MatchPhase.POST_MATCH:
                HandlePostMatch();
                break;
        }
    }

    // --- Phase Handlers ---

    private void HandleWarmup()
    {
        GD.Print("Warmup started!");
    }

    private void HandlePreMatch()
    {
        GD.Print("Pre-match: teleport and freeze players");

        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            var playerCharacter = kvp.Value.Pawn;

            if(playerCharacter != null)
            {
                playerCharacter.TeleportTo(SpawnManager.Instance.GetSpawnPoint().Transform);
                playerCharacter.SetInputEnabled(true);
                playerCharacter.SetWeaponsEnabled(false);
            }
        }
    }

    private void HandleMatchStart()
    {
        GD.Print("Match started: unfreeze players");

        foreach (var playerCharacter in PlayerManager.Instance.GetPlayerCharacters())
        {
            playerCharacter.SetInputEnabled(false);
            playerCharacter.SetWeaponsEnabled(true);
        }
    }

    private void HandlePostMatch()
    {
        GD.Print("Post-match: freeze players and show results");

        foreach (var player in PlayerManager.Instance.GetActivePlayers())
        {
        }

        // Optional: trigger scoreboard, announce winner, etc.
    }

    public void AddPlayerController(byte playerID)
    {
        if(!PlayerControllers.TryGetValue(playerID, out var controller))
        {
            PlayerController playerController = new();
            playerController.PlayerID = playerID;
            PlayerControllers[playerID] = playerController;
        }
        else
        {
            GD.PushError($"Attempted to add player controller with player ID: {playerID}, but a player controller with this player ID already exists.");
        }
    }
}