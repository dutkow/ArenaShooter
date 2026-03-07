using Godot;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;


public partial class Character : Pawn
{
    CharacterMoveMode _mode;


    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;


    [Export] public Camera3D Camera; // assign in editor
    [Export] public float MouseSensitivity = 0.1f;

    List<ClientInputCommand> _unacknowledgedInputs = new();


    const int REDUNDANT_INPUTS = 4;

    public float Yaw => GlobalRotation.Y;
    public float Pitch { get; private set; }


    private uint _lastProcessedClientTick;

    private CharacterMoveState _predictedMoveState;


    // Components
    [Export] MeshInstance3D _characterMesh;
    [Export] MeshInstance3D _thirdPersonWeaponMesh;

    [Export] Weapon _weapon;

    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();

    public override void _Ready()
    {
        base._Ready();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        MovementComp.Initialize(this);
    }
    
    // Ticking using physics process for now for simplicity, will move to server tick
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Tick((float)delta);
    }

    public virtual void Tick(float delta)
    {
        base.Tick(delta);

        if(IsLocal)
        {
            var input = CaptureInput();
            HandleInput(input, delta);

            if(!IsAuthority)
            {
                SendClientInput(input);
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        InterpolateMovement((float)delta);
    }

    /// <summary>
    /// Interface functions
    /// </summary>
    public void OnPossessed(Controller controller)
    {
        base.OnPossessed(controller);

        //_healthComp.Death += OnDeath;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        SetProcessInput(true);
        ShowFirstPersonView();

        Camera.Current = true;

        SetRole(NetworkRole.LOCAL);
        UIRoot.Instance.OnPossessedCharacter(this);
    }

    public void OnUnpossessed()
    {
        base.OnUnpossessed();
    }

    public bool IsAlive()
    {
        return HealthComp.IsAlive;
    }



    public void ShowFirstPersonView()
    {
        HideThirdPersonView();
        _weapon.FirstPersonWeaponMesh.Visible = true;
    }

    public void HideFirstPersonView()
    {
        _weapon.FirstPersonWeaponMesh.Visible = false;
    }

    public void ShowThirdPersonView()
    {
        HideFirstPersonView();

        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        _thirdPersonWeaponMesh.Visible = true;
    }

    public void HideThirdPersonView()
    {
        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        _thirdPersonWeaponMesh.Visible = false;
    }

    float LOCAL_SV_INTERP_RATE = 0.5f;
    float LOCAL_CL_INTERP_RATE = 0.5f;
    float REMOTE_CL_INTERP_RATE = 0.5f;

    public void InterpolateMovement(float delta)
    {
        if(IsAuthority)
        {
            if(IsLocal)
            {
                GlobalPosition.Lerp(_predictedMoveState.Position, LOCAL_SV_INTERP_RATE);
            }
        }
        else
        {
            if(IsLocal)
            {
                GlobalPosition.Lerp(_predictedMoveState.Position, LOCAL_CL_INTERP_RATE);
            }
            else
            {
                GlobalPosition.Lerp(_predictedMoveState.Position, REMOTE_CL_INTERP_RATE);
                GlobalRotation.Lerp(new Vector3(0.0f, _predictedMoveState.Yaw, REMOTE_CL_INTERP_RATE), REMOTE_CL_INTERP_RATE);
            }
        }
    }

    public void ApplyServerSnapshot(ArenaCharacterSnapshot snapshot, ushort lastProcessedClientTick)
    {
        _unacknowledgedInputs.RemoveAll(cmd => cmd.TickNumber <= lastProcessedClientTick);

        if (IsLocal)
        {
            var reconciledState = snapshot.GetMoveState();

            foreach (var cmd in _unacknowledgedInputs)
            {
                reconciledState = MovementComp.Step(reconciledState, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);
            }

            ReconcileMoveState(reconciledState);
        }
    }

    public void HandleClientCommand(ClientCommand command)
    {

    }

    public void ReconcileMoveState(CharacterMoveState newPredictedState)
    {
        float positionDelta = (_predictedMoveState.Position - newPredictedState.Position).Length();

        const float SNAP_THRESHOLD = 1.0f;
        const float INTERP_THRESHOLD = 0.05f;

        if (positionDelta > SNAP_THRESHOLD)
        {
            _predictedMoveState.Position = newPredictedState.Position;
            _predictedMoveState.Velocity = newPredictedState.Velocity;
        }
        else if (positionDelta > INTERP_THRESHOLD)
        {
            _predictedMoveState.Position = _predictedMoveState.Position.Lerp(newPredictedState.Position, 0.5f);
            _predictedMoveState.Velocity = newPredictedState.Velocity;
        }
    }

    // predicting on the server so the server can also interpolate to this, just without snapshot based reconciliation
    public void HandleInput(InputCommand input, float delta)
    {
        _predictedMoveState = MovementComp.Step(_predictedMoveState, input, delta);
    }

    public void SendClientInput(InputCommand newInput)
    {
        var inputCommand = new ClientInputCommand();

        inputCommand.TickNumber = MatchState.Instance.CurrentTick;
        inputCommand.Input = newInput;
        inputCommand.Yaw = GlobalRotation.Y;
        inputCommand.Pitch = Camera.GlobalRotation.Y;

        _unacknowledgedInputs.Add(inputCommand);
    }


    public InputCommand CaptureInput()
    {
        InputCommand cmd = InputCommand.NONE;

        if (Input.IsActionPressed("move_forward")) cmd |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back")) cmd |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left")) cmd |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionJustPressed("jump")) cmd |= InputCommand.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd |= InputCommand.FIRE_PRIMARY;

        //LastInputCommand = cmd;

        //Weapon.HandleInput(cmd);

        return cmd;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if(!IsLocal)
        {
            return;
        }

        HandleMouseLook(@event);
    }


    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * MouseSensitivity));

            Pitch += -mouseEvent.Relative.Y * MouseSensitivity;
            Pitch = Mathf.Clamp(Pitch, -90, 90);

            if (Camera != null)
            {
                Camera.RotationDegrees = new Vector3(Pitch, 0, 0);
            }
        }
    }
}