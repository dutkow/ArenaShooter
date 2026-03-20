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
        var info = new ChatMessageInfo(ChatChannel.ALL, text);

        if(NetworkManager.Instance.IsClient)
        {
            ChatMessageRequest.Send(info);
        }
        else
        {
            info.Text = NetUtils.ValidateChatMessage(text);
            ClientGame.Instance?.ApplyChatMessage(info);
        }
    }
}
