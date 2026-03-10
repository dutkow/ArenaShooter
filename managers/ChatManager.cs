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

    public void BroadcastChatMessageReceived(ChatMessageInfo info)
    {
        ChatMessageReceived?.Invoke(info);
    }
}
