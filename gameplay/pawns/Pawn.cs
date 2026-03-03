using Godot;

public partial class Pawn : Node3D
{
    public Controller Controller { get; private set; }

    public bool IsPossessed => Controller != null;

    private bool _inputLockedByServer = false;

    public bool IsPossessedLocally;
    public bool InputEnabled => IsPossessedLocally && !_inputLockedByServer;

    public override void _Ready()
    {
        base._Ready();

        SetProcessInput(false);
    }

    public virtual void OnPossessed(Controller controller)
    {
        Controller = controller;

        if (controller is PlayerController pc && pc.PlayerID == NetworkSession.Instance.LocalPlayerID)
        {
            SetProcessInput(true);
            IsPossessedLocally = true;
        }
        else
        {
            SetProcessInput(false);
            IsPossessedLocally = false;
        }
    }

    public virtual void OnUnpossessed()
    {
        Controller = null;
        IsPossessedLocally = false;
    }

    public void SetServerInputLock(bool locked)
    {
        _inputLockedByServer = locked;
    }
}