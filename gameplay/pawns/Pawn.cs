using Godot;

public partial class Pawn : Node3D
{
    public Controller Controller { get; private set; }

    public bool IsPossessed => Controller != null;

    private bool _inputEnabled = false;

    public bool IsPossessedLocally;
    public bool InputActive => IsPossessedLocally && !_inputEnabled;

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

    public void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
    }
}