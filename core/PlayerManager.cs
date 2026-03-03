using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
    public static PlayerManager Instance { get; private set; }

    private readonly List<PlayerState> _playerStates = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public void RegisterPlayer(PlayerState state)
    {
        if (!_playerStates.Contains(state))
        {
            _playerStates.Add(state);
        }
    }

    public void UnregisterPlayer(PlayerState state)
    {
        _playerStates.Remove(state);
    }

    public IReadOnlyList<PlayerState> GetAllPlayers() => _playerStates;

    public IReadOnlyList<PlayerState> GetActivePlayers()
    {
        return _playerStates.FindAll(p => p.Character != null);
    }

    public IReadOnlyList<ArenaCharacter> GetPlayerCharacters()
    {
        return _playerStates.Select(p => p.Character).OfType<ArenaCharacter>().ToList();
    }
}