using Godot;
using System;
using System.Threading.Tasks;

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

        // This feels hacky and might not be consistent but if we delay one frame the added child isn't fully initialized, so calling deferred twice to defer two frames, which appears to fix the
        // automatic downward scrolling
        if (_isScrolledToBottom)
        {
            CallDeferred(nameof(DeferredScroll));
        }
    }

    public void DeferredScroll()
    {
        CallDeferred(nameof(ScrollToBottom));
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
