using Godot;
using System;

public enum ChatChannel
{
    ALL,
    TEAM,
    PRIVATE,
    SYSTEM,
}

public struct ChatMessageInfo
{
    public ChatChannel Channel;
    public string Text;
    public byte PlayerID;

    public ChatMessageInfo(ChatChannel channel, string text, byte playerID = 0)
    {
        Channel = channel;
        Text = text;
        PlayerID = playerID;
    }
}
public class ChatManager
{
    public const int MAX_CHAT_MESSAGE_CHARACTERS = 20;

    public static ChatManager Instance { get; private set; }

    public event Action<ChatMessageInfo>? ChatMessageReceived;

    // Server -> client validated chat message that should appear in chat log.

    public static void Create()
    {
        if (Instance != null)
        {
            throw new Exception("ChatManager already exists!");
        }

        Instance = new ChatManager();
    }

    public static void Destroy()
    {
        if(Instance != null)
        {
            Instance = null;
        }
    }

    public void SendChatMessageRequest(ChatMessageInfo info)
    {
        ChatMessageRequest.Send(info);
    }

    public void HandleChatMessage(byte[] data)
    {
        var msg = new ChatMessage();
        msg.ReadMessage(data);

        ChatMessageReceived?.Invoke(msg.ToInfo());
    }

    // Client -> server chat message request
    public void HandleChatMessageRequest(ENetPacketPeer peer, byte[] data)
    {
        var chatMessageRequest = Message.FromData<ChatMessageRequest>(data);
        byte peerID = (byte)peer.GetMeta("id");

        var info = new ChatMessageInfo();
        info.Channel = chatMessageRequest.Channel;
        info.Text = chatMessageRequest.Text;
        info.PlayerID = peerID;

        if(info.Text.Length > MAX_CHAT_MESSAGE_CHARACTERS)
        {
            info.Text = info.Text.Substring(0, MAX_CHAT_MESSAGE_CHARACTERS);
        }

        ChatMessage.Send(info);
    }
}
