using Godot;
using System;

public partial class ChatPanel : Control
{
    [Export] ChatInputField _chatInputField;
    [Export] ColorRect _background;

    public override void _Ready()
    {
        base._Ready();

        ClientGame.Instance.LocalPlayerController.ChatPanel = this;
    }

    public void Open()
    {
        CallDeferred(nameof(GrabFocus)); // deferred to ignore input bound to open chat
        _chatInputField.Visible = true;
        _background.Visible = true;
    }

    public void GrabFocus()
    {
        _chatInputField.GrabFocus();
    }

    public void SendChat()
    {
        if(_chatInputField.Text.Length > 0)
        {
            ChatMessageRequest.Send(new ChatMessageInfo(ChatChannel.ALL, _chatInputField.Text)); // keeping simple for now
            _chatInputField.Clear();
        }
        Close();
    }

    public void Close()
    {
        _chatInputField.Visible = false;
        _background.Visible = false;
        _chatInputField.ReleaseFocus();
    }
}
