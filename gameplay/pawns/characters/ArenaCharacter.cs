using Godot;
using System;
using System.Collections.Generic;

public enum MovementState
{
    GROUNDED,
    FALLING
}

public class ArenaCharacterSnapshot
{
    public byte PlayerID;
    public Vector3 Position;
    public float Yaw;
    public float AimPitch;

    public ArenaCharacterSnapshot() { }

    public ArenaCharacterSnapshot(byte playerID, Vector3 pos, float yaw, float pitch)
    {
        PlayerID = playerID;
        Position = pos;
        Yaw = yaw;
        AimPitch = pitch;
    }
}

public partial class ArenaCharacter : Pawn
{
    // ----------------------
    // Exports & Components
    // ----------------------
    [Export] public CharacterBody3D CharacterBody;
    [Export] public MeshInstance3D CharacterMesh;
    [Export] public Weapon Weapon;
    [Export] public MeshInstance3D ThirdPersonWeaponMesh;
    [Export] public Camera3D Camera;
    [Export] public Marker3D CameraPivot;

    [Export] public int Speed { get; set; } = 14;
    [Export] public int FallAcceleration { get; set; } = 50;
    [Export] public float JumpVelocity { get; set; } = 20f;
    [Export] public float AirControlAcceleration { get; set; } = 6f;
    [Export] public float MouseSens = 0.09f;
    [Export] public float MouseSmooth = 50f;

    // ----------------------
    // State
    // ----------------------
    public bool IsAlive;
    public PlayerState State { get; private set; }

    private MovementState _movementState;
    private bool _weaponsEnabled = true;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _cameraInput = Vector2.Zero;
    private Vector2 _rotVelocity = Vector2.Zero;

    private bool _canJump => CharacterBody.IsOnFloor();
    public float Yaw => CharacterBody.GlobalRotation.Y;
    public float AimPitch => CameraPivot.Rotation.X;

    // ----------------------
    // Networking
    // ----------------------
    public ArenaCharacterSnapshot LastSnapshot;
    public List<ArenaCharacterSnapshot> SnapshotBuffer = new();
    public InputCommand LastInputCommand;

    private double _inputSendAccumulator = 0f;


    // ----------------------
    // Initialization
    // ----------------------
    public override void _Ready()
    {
        base._Ready();
        Camera.Current = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        SetProcessInput(false);
        ShowThirdPersonView();
    }

    public override void OnPossessed(Controller controller)
    {
        base.OnPossessed(controller);

        SetProcessInput(true);
        ShowFirstPersonView();

        Camera.Current = true;
    }

    public void ShowFirstPersonView()
    {
        HideThirdPersonView();

        Weapon.FirstPersonWeaponMesh.Visible = true;
    }

    public void HideFirstPersonView()
    {
        Weapon.FirstPersonWeaponMesh.Visible = false;
    }

    public void ShowThirdPersonView()
    {
        HideFirstPersonView();

        CharacterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        ThirdPersonWeaponMesh.Visible = true;
    }

    public void HideThirdPersonView()
    {
        CharacterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        ThirdPersonWeaponMesh.Visible = false;
    }

    public void Initialize(PlayerState state)
    {
        if (state == null)
        {
            GD.PushError("State was null on arena character initialization");
            return;
        }

        State = state;
        State.Character = this;
    }

    // ----------------------
    // Replication
    // ----------------------
    public ArenaCharacterSnapshot GetSnapshot()
    {
        return new ArenaCharacterSnapshot(
            State.PlayerID,
            CharacterBody.GlobalPosition,
            Yaw,
            AimPitch
        );
    }

    public void ApplySnapshot(ArenaCharacterSnapshot snapshot)
    {
        if (snapshot == null) return;

        if (!IsLocal)
        {
            CharacterBody.GlobalPosition = snapshot.Position;

            var rot = CharacterBody.Rotation;
            rot.Y = snapshot.Yaw;
            CharacterBody.Rotation = rot;

            if (CameraPivot != null)
            {
                var camRot = CameraPivot.Rotation;
                camRot.X = Mathf.Clamp(snapshot.AimPitch, -1.5f, 1.5f);
                CameraPivot.Rotation = camRot;
            }
        }
    }

    // ----------------------
    // Input & Mouse
    // ----------------------
    public override void _Input(InputEvent @event)
    {
        if (!InputActive) return;

        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
            _cameraInput = mouseEvent.Relative;

        if (_weaponsEnabled && Input.IsActionPressed("primary_fire"))
            TryPrimaryFire();

        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _Process(double delta)
    {
        _rotVelocity = _rotVelocity.Lerp(_cameraInput * MouseSens, (float)delta * MouseSmooth);

        if (CameraPivot != null)
        {
            CameraPivot.RotateX(-Mathf.DegToRad(_rotVelocity.Y));
            CameraPivot.Rotation = new Vector3(
                Mathf.Clamp(CameraPivot.Rotation.X, -1.5f, 1.5f),
                CameraPivot.Rotation.Y,
                CameraPivot.Rotation.Z
            );
        }

        CharacterBody.RotateY(-Mathf.DegToRad(_rotVelocity.X));
        _cameraInput = Vector2.Zero;


        if (!IsLocal && !IsAuthority)
        {
            InterpolateSnapshots(delta);
        }
    }

    // ----------------------
    // Physics & Movement
    // ----------------------
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Authority (server) simulates movement
        if (IsAuthority)
        {
            InputCommand input = IsLocal ? CaptureInput() : LastInputCommand;
            ApplyInput(input, delta);
        }

        // Local client sends input to server
        if (IsLocal && !IsAuthority)
        {
            _inputSendAccumulator += (float)delta;
            if (_inputSendAccumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
            {
                _inputSendAccumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
                SendClientCommand();
            }
        }
    }


    private InputCommand CaptureInput()
    {
        InputCommand cmd = InputCommand.NONE;

        if (Input.IsActionPressed("move_forward")) cmd |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back")) cmd |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left")) cmd |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionJustPressed("jump")) cmd |= InputCommand.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd |= InputCommand.FIRE_PRIMARY;

        LastInputCommand = cmd; // store locally for authority simulation
        return cmd;
    }

    public void ApplyInput(InputCommand cmd, double delta)
    {
        Vector3 moveDir = Vector3.Zero;

        if (cmd.HasFlag(InputCommand.MOVE_FORWARD)) moveDir.Z -= 1f;
        if (cmd.HasFlag(InputCommand.MOVE_BACK)) moveDir.Z += 1f;
        if (cmd.HasFlag(InputCommand.MOVE_LEFT)) moveDir.X -= 1f;
        if (cmd.HasFlag(InputCommand.MOVE_RIGHT)) moveDir.X += 1f;

        if (moveDir != Vector3.Zero)
        {
            moveDir = moveDir.Normalized();
            if (CameraPivot != null)
                moveDir = moveDir.Rotated(Vector3.Up, CameraPivot.GlobalRotation.Y);
        }

        UpdateMovementState();

        if (cmd.HasFlag(InputCommand.JUMP) && _canJump)
            TryJump();

        if (_movementState == MovementState.GROUNDED)
        {
            _targetVelocity.X = moveDir.X * Speed;
            _targetVelocity.Z = moveDir.Z * Speed;
        }
        else
        {
            _targetVelocity.X += moveDir.X * AirControlAcceleration * (float)delta;
            _targetVelocity.Z += moveDir.Z * AirControlAcceleration * (float)delta;
        }

        if (!CharacterBody.IsOnFloor())
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        else if (_targetVelocity.Y < 0)
            _targetVelocity.Y = 0;

        CharacterBody.Velocity = _targetVelocity;
        CharacterBody.MoveAndSlide();
    }

    private void InterpolateSnapshots(double delta)
    {
        if (SnapshotBuffer.Count < 2) return;

        var prev = SnapshotBuffer[0];
        var next = SnapshotBuffer[1];

        CharacterBody.GlobalPosition = prev.Position.Lerp(next.Position, 0.5f);
        var rot = CharacterBody.Rotation;
        rot.Y = Mathf.LerpAngle(prev.Yaw, next.Yaw, 0.5f);
        CharacterBody.Rotation = rot;
    }

    private void SendClientCommand()
    {
        InputCommand cmd = CaptureInput();

        var clientCmd = new ClientCommand()
        {
            PlayerID = State.PlayerID,
            TickNumber = MatchState.Instance.CurrentTick,
            InputButtons = cmd,
            Yaw = CharacterBody.GlobalRotation.Y,
            Pitch = CameraPivot.GlobalRotation.X
        };

        ClientCommand.Send(clientCmd);
    }

    // ----------------------
    // Helpers
    // ----------------------
    public void UpdateMovementState()
    {
        _movementState = CharacterBody.IsOnFloor() ? MovementState.GROUNDED : MovementState.FALLING;
    }

    public void TryJump()
    {
        if (!_canJump) return;
        _targetVelocity.Y = JumpVelocity;
        _movementState = MovementState.FALLING;
    }

    public void TryPrimaryFire()
    {
        if (Weapon == null) return;
        Vector3 dir = -Camera.GlobalTransform.Basis.Z;
        Weapon.TryPrimaryFire(Camera.GlobalPosition, dir);
    }

    public void TeleportTo(Transform3D t)
    {
        GlobalTransform = t;
        CharacterBody.Velocity = Vector3.Zero;
    }

    public void ResetMovement()
    {
        CharacterBody.Velocity = Vector3.Zero;
    }

    public void SetWeaponsEnabled(bool enabled)
    {
        _weaponsEnabled = enabled;
    }
}