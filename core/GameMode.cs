using Godot;
using System;

public partial class GameMode : Node
{
    public static GameMode Instance { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        if (!Multiplayer.IsServer())
        {
            return;
        }

        MatchState.Instance.MatchPhaseChanged += OnMatchPhaseChanged;
    }

    public void Initialize()
    {
        MatchState.Instance.Initialize();
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
        // Optional: show countdown, play music, etc.
    }

    private void HandlePreMatch()
    {
        GD.Print("Pre-match: teleport and freeze players");

        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            var playerCharacter = kvp.Value.Character;

            if(playerCharacter != null)
            {
                playerCharacter.TeleportTo(SpawnManager.Instance.GetSpawnPoint().Transform);
                playerCharacter.SetInputEnabled(false);
                playerCharacter.SetWeaponsEnabled(false);
            }
        }
    }

    private void HandleMatchStart()
    {
        GD.Print("Match started: unfreeze players");

        foreach (var playerCharacter in PlayerManager.Instance.GetPlayerCharacters())
        {
            playerCharacter.SetInputEnabled(true);
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
}