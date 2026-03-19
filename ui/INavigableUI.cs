using Godot;
using System;

public partial class NavigableUI : Control
{
    public virtual void OnPushed()
    {
        Visible = true;
    }

    public virtual void OnPopped()
    {
        Visible = false;
    }

    public virtual void OnCovered()
    {
        Visible = false;
    }

    public virtual void OnUncovered()
    {
        Visible = true;
    }

    public virtual bool HandleBack()
    {
        return false;
    }
}