using Godot;
using System;

public partial class ChatInputField : LineEdit
{
    public override void _Ready()
    {
        base._Ready();

        TextSubmitted += SendMessageRequest;
    }

    public void SendMessageRequest(string text)
    {
        ChatMessageRequest.Send(new ChatMessageInfo(ChatChannel.ALL, text)); // no channels yet
    }
}
