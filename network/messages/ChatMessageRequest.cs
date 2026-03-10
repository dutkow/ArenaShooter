using Godot;
using System;

public class ChatMessageRequest : Message
{
    public ChatChannel Channel;
    public string Text;
    public byte TargetPlayerID; // used only for private messages

    public ChatMessageInfo ToInfo()
    {
        return new ChatMessageInfo(Channel, Text, TargetPlayerID);
    }

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Channel);
        Add(Text);
        Add(TargetPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Channel);
        Write(Text);
        Write(TargetPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Channel);
        Read(out Text);
        Read(out TargetPlayerID);
    }

    public static void Send(ChatMessageInfo info)
    {
        if (NetworkSession.Instance.IsServer)
        {
            info.PlayerID = 0;
            if (ChatService.ValidateChatMessageRequest(info))
            {
                if(NetworkSession.Instance.IsListenServer)
                {
                    ChatManager.Instance.BroadcastChatMessageReceived(info);
                }
                ChatMessage.Send(info);
            }
        }
        else
        {
            ChatMessageRequest.Send(info);
        }
    }
}