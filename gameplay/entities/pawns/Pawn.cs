using Godot;

public partial class Pawn : Entity
{
    public Controller Controller { get; private set; }

    public bool IsPossessed => Controller != null;

    protected bool _inputEnabled = false;

    public bool InputActive => IsLocal && !_inputEnabled;

    public PlayerState PlayerState;

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

        SetInputEnabled(true);
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

    public virtual void HandleRemoteSpawn(byte playerID) { }

    public virtual void Initialize(PlayerState playerState)
    {
        PlayerState = playerState;
    }

    public virtual void OnDeath() { }

    public virtual ClientPredictionTick GetClientPredictionTick(ClientPredictionTick clientPredictionTick)
    {
        return clientPredictionTick;
    }

    public virtual void ServerProcessNextClientInput(ClientInputCommand cmd, float delta) { }

    public virtual void ApplySnapshot(CharacterSnapshot snapshot) { }

}