using Godot;
using System;

public partial class GameMode : Node
{
    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        // Listen to match phase changes
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
        // Optional: show countdown, play music, etc.
    }

    private void HandlePreMatch()
    {
        GD.Print("Pre-match: teleport and freeze players");

        foreach (var playerState in PlayerManager.Instance.GetActivePlayers())
        {
            var playerCharacter = playerState.Pawn;
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

        foreach (var playerState in PlayerManager.Instance.GetActivePlayers())
        {
            var playerCharacter = playerState.Pawn;
            if (playerCharacter != null)
            {
                playerCharacter.SetInputEnabled(true);
                playerCharacter.SetWeaponsEnabled(true);
            }
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