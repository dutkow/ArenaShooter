using Godot;
using System;

public partial class ChatLog : ScrollContainer
{
    const int MAX_CHAT_MESSAGES_IN_LOG = 100;

    [Export] private PackedScene _chatEntryScene;
    [Export] private VBoxContainer _vBoxContainer;

    private bool _isScrolledToBottom = true;

    public override void _Ready()
    {
        base._Ready();

        ChatManager.Instance.NewChatMessage += AddChatMessage;
    }

    public void AddChatMessage(ChatMessageInfo info)
    {
        _vBoxContainer.AddChild(ChatMessageEntry.Create(_chatEntryScene, info));

        if (_vBoxContainer.GetChildCount() > MAX_CHAT_MESSAGES_IN_LOG)
        {
            RemoveOldestMessage();
        }

        if(_isScrolledToBottom)
        {
            ScrollToBottom();
        }
    }

    public void RemoveOldestMessage()
    {
        _vBoxContainer.GetChild(0).QueueFree();
    }

    public void ScrollToBottom()
    {
        ScrollVertical = (int)GetVScrollBar().MaxValue;
    }
}
