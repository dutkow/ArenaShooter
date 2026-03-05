using System;

public class Component
{
    public Object Owner;

    public virtual void SetOwner(Object owner)
    {
        Owner = owner;
    }
}
