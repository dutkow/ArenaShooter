using Godot;

public partial class Pawn : Actor
{
    public Controller Controller { get; private set; }

    public bool IsPossessed => Controller != null;

    private bool _inputEnabled = false;

    public bool InputActive => IsLocal && !_inputEnabled;

    public override void _Ready()
    {
        base._Ready();

        SetProcessInput(false);
    }

    public virtual void OnPossessed(Controller controller)
    {
        Controller = controller;

        IsAuthority = NetworkSession.Instance.IsServer;

        if (controller is PlayerController pc && pc.PlayerID == NetworkSession.Instance.LocalPlayerID)
        {
            SetProcessInput(true);
            Role = NetworkRole.LOCAL;
        }
        else
        {
            SetProcessInput(false);
            Role = NetworkRole.REMOTE;
        }
    }

    public virtual void OnUnpossessed()
    {
        Controller = null;
        Role = NetworkRole.NONE;
    }

    public void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
    }
}