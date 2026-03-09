using Godot;

public partial class Pawn : Actor
{
    public Controller Controller { get; private set; }

    public bool IsPossessed => Controller != null;

    private bool _inputEnabled = false;

    public bool InputActive => IsLocal && !_inputEnabled;

    public PlayerState PlayerState { get; private set; }

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

    public virtual void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
    }

    public virtual void TeleportTo(Transform3D t) { GlobalTransform = t; }
    public virtual void SetWeaponsEnabled(bool enabled) { }

    public virtual void HandleRemoteSpawn() { }

    public virtual void Initialize(PlayerState playerState)
    {
        PlayerState = playerState;
    }

    public virtual void OnDeath() { }

    public virtual ClientInputCommand AddInput(ClientInputCommand cmd)
    {
        return cmd;
    }

    public virtual void ApplyInput(ClientInputCommand cmd) { }
    public virtual void ProcessClientInput(ClientInputCommand cmd) { }

}