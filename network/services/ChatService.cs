using Godot;
using System;

public static class ChatService
{
    public const int MAX_CHAT_MESSAGE_CHARACTERS = 20;

    public static void HandleChatMessage(byte[] data)
    {
        var msg = new ChatMessage();
        msg.ReadMessage(data);

        ChatManager.Instance.BroadcastChatMessageReceived(msg.ToInfo());
    }

    // Client -> server chat message request
    public static void HandleChatMessageRequest(ENetPacketPeer peer, byte[] data)
    {
        var chatMessageRequest = Message.FromData<ChatMessageRequest>(data);
        byte peerID = (byte)peer.GetMeta("id");

        var info = new ChatMessageInfo();
        info.Channel = chatMessageRequest.Channel;
        info.Text = chatMessageRequest.Text;
        info.PlayerID = peerID;

        if (info.Text.Length > MAX_CHAT_MESSAGE_CHARACTERS)
        {
            info.Text = info.Text.Substring(0, MAX_CHAT_MESSAGE_CHARACTERS);
        }

        ChatMessage.Send(info);
    }
}
