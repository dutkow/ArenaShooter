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
        // REFACTOR CODE
        if (MatchState.Instance.NewConnectedPlayers.TryGetValue(chatMessageInfo.PlayerID, out var newPlayerState))
        {
            _senderNameLabel.Text = newPlayerState.PublicState.PlayerName + ":";
        }
        else
        {
            GD.PushError($"Player state not found for Player ID: {chatMessageInfo.PlayerID}");
        }

        _messageTextLabel.Text = chatMessageInfo.Text;

    }
}
