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

        if(controller is PlayerController)
        {
            Role = NetworkRole.LOCAL;
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