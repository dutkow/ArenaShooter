using Godot;
using System;

public partial class ChatMessageEntry : Control
{
    [Export] private Label _senderNameLabel;
    [Export] private Label _messageTextLabel;

    public static ChatMessageEntry Create(PackedScene scene, ChatMessageInfo info)
    {
        ChatMessageEntry entry = (ChatMessageEntry)scene.Instantiate();
        entry.Initialize(info);

        if(entry == null)
        {
            GD.Print($"Chat message entry was null upon creation!");
        }
        return entry;
    }
    public void Initialize(ChatMessageInfo chatMessageInfo)
    {
        if(chatMessageInfo.Channel == ChatChannel.SYSTEM)
        {
            return; // handle later if and when this exists
        }

        if(MatchState.Instance.ConnectedPlayers.TryGetValue(chatMessageInfo.PlayerID, out var playerState))
        {
            _senderNameLabel.Text = playerState.PlayerName + ":";
        }
        else
        {
            GD.PushError($"Player state not found for Player ID: {chatMessageInfo.PlayerID}");
        }

        _messageTextLabel.Text = chatMessageInfo.Text;

    }
}
