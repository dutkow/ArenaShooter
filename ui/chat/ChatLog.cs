using Godot;
using System;

public partial class ChatLog : ScrollContainer
{
    const int MAX_CHAT_MESSAGES_IN_LOG = 100;

    [Export] private PackedScene _chatMessageEntryScene;
    [Export] private VBoxContainer _chatMessagesContainer;

    private bool _isScrolledToBottom = true;

    public override void _Ready()
    {
        base._Ready();

        Clear();

        ChatManager.Instance.ChatMessageReceived += OnChatMessageReceived;
    }

    public void OnChatMessageReceived(ChatMessageInfo info)
    {
        _chatMessagesContainer.AddChild(ChatMessageEntry.Create(_chatMessageEntryScene, info));

        if (_chatMessagesContainer.GetChildCount() > MAX_CHAT_MESSAGES_IN_LOG)
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
        _chatMessagesContainer.GetChild(0).QueueFree();
    }

    public void ScrollToBottom()
    {
        ScrollVertical = (int)GetVScrollBar().MaxValue;
    }

    public void Clear()
    {
        var children = _chatMessagesContainer.GetChildren();
        foreach(var child in children)
        {
            child.QueueFree();
        }
    }
}
