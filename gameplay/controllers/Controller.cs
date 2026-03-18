using Godot;
using System;

public partial class Controller : Node
{
    public Pawn PossessedPawn { get; private set; }

    public int PlayerID = -1;

    public virtual void Possess(Pawn pawn)
    {
        if (pawn == null || pawn == PossessedPawn)
        {
            return;
        }

        UnPossess();

        PossessedPawn = pawn;

        PossessedPawn.OnPossessed(this);
    }

    public virtual void UnPossess()
    {
        if (PossessedPawn == null)
        {
            return;
        }

        PossessedPawn.OnUnpossessed();
        PossessedPawn = null;
    }

}
