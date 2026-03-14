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
        AddEnum(Channel);
        Add(Text);
        Add(TargetPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        WriteEnum(Channel);
        Write(Text);
        Write(TargetPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        ReadEnum(out Channel);
        Read(out Text);
        Read(out TargetPlayerID);
    }

    public static void Send(ChatMessageInfo info)
    {
        if (NetworkManager.Instance.IsServer)
        {
            info.PlayerID = 0;
            if (ChatService.ValidateChatMessageRequest(info))
            {
                if(NetworkManager.Instance.IsListenServer)
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