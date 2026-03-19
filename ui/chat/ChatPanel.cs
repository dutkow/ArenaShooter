using Godot;
using System;

public partial class ChatPanel : Control
{
    [Export] ChatInputField _chatInputField;


    public override void _Ready()
    {
        base._Ready();

        ClientGame.Instance.LocalPlayerController.ChatPanel = this;
    }

    public void Open()
    {
        CallDeferred(nameof(GrabFocus)); // deferred to ignore input bound to open chat
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
        _chatInputField.ReleaseFocus();
    }
}
