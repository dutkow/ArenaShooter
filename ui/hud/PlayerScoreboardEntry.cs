using Godot;
using System;

public partial class PlayerScoreboardEntry : Control
{
    [Export] Label _playerNameLabel;

    [Export] ColorRect _playerOwnerHighlightRect;

    [Export] Color _playerOwnerColor = Colors.Gold;

    public void Initialize(PlayerStateOld playerState)
    {
        _playerNameLabel.Text = playerState.PlayerInfo.PlayerName;

        playerState.PlayerNameChanged += OnPlayerNameChanged;
        playerState.PlayerLeft += OnPlayerLeft;

        if(playerState.PlayerInfo.PlayerID == ClientGame.Instance.LocalPlayerID)
        {
            Highlight();
        }
    }

    public void OnPlayerNameChanged(string playerName)
    {
        _playerNameLabel.Text = playerName;
    }

    public void OnPlayerLeft()
    {
        QueueFree();
    }

    public void Highlight()
    {
        _playerOwnerHighlightRect.Visible = true;
        _playerNameLabel.AddThemeColorOverride("font_color", _playerOwnerColor);
    }
}
