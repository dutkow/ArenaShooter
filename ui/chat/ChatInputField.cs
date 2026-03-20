using Godot;
using System;

public partial class ChatInputField : LineEdit
{
    private bool _justTrimmedText;

    public override void _Ready()
    {
        base._Ready();

        MaxLength = NetworkConstants.MAX_CHAT_MESSAGE_CHARACTERS;
    }

    public void ConfirmChat()
    {
        if(Text.Length == 0)
        {
            return;
        }

        var info = new ChatMessageInfo(ChatChannel.ALL, Text);

        if(NetworkManager.Instance.IsClient)
        {
            ChatMessageRequest.Send(info);
        }
        else
        {
            ServerGame.Instance.ApplyChatMessageRequest(ClientGame.Instance.LocalPlayerID, info);
        }

        Clear();
    }
}
