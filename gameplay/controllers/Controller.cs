using Godot;
using System;

public class Controller
{
    public IPossessable PossessedEntity { get; private set; }

    public int PlayerID = -1;

    public void Possess(IPossessable possessable)
    {
        if (possessable == null || possessable == PossessedEntity)
        {
            return;
        }

        UnPossess();

        PossessedEntity = possessable;

        PossessedEntity.OnPossessed(this);
    }

    public void UnPossess()
    {
        if (PossessedEntity == null)
        {
            return;
        }

        PossessedEntity.OnUnpossessed();
        PossessedEntity = null;
    }

}
