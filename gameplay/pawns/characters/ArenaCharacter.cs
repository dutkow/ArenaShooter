using Godot;
using System;
using System.Collections.Generic;

public enum MovementState
{
    GROUNDED,
    FALLING
}

public partial class ArenaCharacter : Pawn, IDamageable
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

    public HealthComponent HealthComponent { get; private set; } = new HealthComponent();

    // ----------------------
    // State
    // ----------------------
    public bool IsAlive = true;
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

    private double _tickAssumulator = 0f;

    // ----------------------
    // Position Correction (client only)
    // ----------------------
    [Export] public float SnapThreshold = 2f;          // units to snap instantly
    [Export] public float CorrectionThreshold = 0.1f;  // small differences lerp
    [Export] public float CorrectionSpeed = 10f;       // lerp speed per second

    private bool _wasFiringLastTick = false;

    // ----------------------
    // Initialization
    // ----------------------
    public void HandleRemoteSpawn()
    {
        Camera.Current = false;
        SetProcessInput(false);
        ShowThirdPersonView();
        GD.Print($"show third person view ran on {NetworkSession.Instance.NetworkMode} ");

        Role = NetworkRole.REMOTE;
    }
    
    public override void OnPossessed(Controller controller)
    {
        base.OnPossessed(controller);

        Input.MouseMode = Input.MouseModeEnum.Captured;

        GD.Print($"on possessed ran on {NetworkSession.Instance.NetworkMode} ");

        SetProcessInput(true);

        ShowFirstPersonView();

        Camera.Current = true;

        UIRoot.Instance.OnPossessedArenaCharacter(this);
    }

    public void ShowFirstPersonView()
    {
        HideThirdPersonView();

        GD.Print($"show first person view ran on {NetworkSession.Instance.NetworkMode} ");
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
        ThirdPersonWeaponMesh.Visible = false;
        CharacterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
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
    // Interface functions
    // ----------------------
    public void ApplyDamage(int amount)
    {
        HealthComponent.ApplyDamage(amount);
    }


    // ----------------------
    // Replication
    // ----------------------
    public ArenaCharacterSnapshot GetSnapshot()
    {
        return new ArenaCharacterSnapshot(
            State.PlayerID,
            CharacterBody.GlobalPosition,
            CharacterBody.Velocity,
            Yaw,
            AimPitch
        );
    }

    public void ApplyClientCommand(ClientCommand cmd, double delta)
    {
        // --- Apply yaw to character body ---
        var bodyRot = CharacterBody.GlobalRotation;
        bodyRot.Y = cmd.Yaw;
        CharacterBody.GlobalRotation = bodyRot;


        // --- Apply pitch to third-person weapon mesh (for now) ---
        if (ThirdPersonWeaponMesh != null)
        {
            var weaponRot = ThirdPersonWeaponMesh.GlobalRotation;
            weaponRot.X = Mathf.Clamp(cmd.Pitch, -1.5f, 1.5f);
            ThirdPersonWeaponMesh.GlobalRotation = weaponRot;
        }

        // --- Apply movement using your existing ApplyInput ---
        ApplyInput(cmd.InputButtons, delta);
    }

    public void ApplySnapshot(ArenaCharacterSnapshot snapshot, float deltaTime = 0f)
    {
        if (snapshot == null) return;

        LastSnapshot = snapshot;

        if (!IsLocal)
        {
            Vector3 predictedPos = snapshot.Position + snapshot.Velocity * deltaTime;
            CharacterBody.GlobalPosition = predictedPos;

            var rot = CharacterBody.GlobalRotation;
            rot.Y = snapshot.Yaw;
            CharacterBody.GlobalRotation = rot;

            if (ThirdPersonWeaponMesh != null)
            {
                var camRot = ThirdPersonWeaponMesh.GlobalRotation;
                camRot.X = Mathf.Clamp(snapshot.AimPitch, -1.5f, 1.5f);
                ThirdPersonWeaponMesh.GlobalRotation = camRot;
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
            InterpolateRemoteSnapshots(delta);
        }

        // Correct local position based on last server snapshot
        if (IsLocal && LastSnapshot != null)
        {
            CorrectClientPosition(LastSnapshot, delta);
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

            Vector3 dir = -Camera.GlobalTransform.Basis.Z;
            Weapon.TickWeapon(delta, Camera.GlobalPosition, dir);
        }


        // Local client sends input to server
        if (IsLocal && !IsAuthority)
        {
            InputCommand cmd = CaptureInput();

            // Apply input locally
            ApplyInput(cmd, delta);

            _tickAssumulator += (float)delta;
            if (_tickAssumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
            {
                _tickAssumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
                SendClientCommand(cmd);
            }
        }

        CharacterBody.MoveAndSlide();
        HandleFallAcceleration(delta);
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
        {
            TryJump();
        }

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

        Weapon.HandleInput(cmd);

        CharacterBody.Velocity = _targetVelocity;
    }

    public void HandleFallAcceleration(double delta)
    {
        if (!CharacterBody.IsOnFloor())
        {
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        }
        else if (_targetVelocity.Y < 0)
        {
            _targetVelocity.Y = 0;
        }
    }

    private void InterpolateRemoteSnapshots(double delta)
    {
        if (SnapshotBuffer.Count < 2) return;

        var prev = SnapshotBuffer[0];
        var next = SnapshotBuffer[1];

        // Interpolate position
        CharacterBody.GlobalPosition = prev.Position.Lerp(next.Position, 0.5f);

        // Interpolate yaw
        var rot = CharacterBody.GlobalRotation;
        rot.Y = Mathf.LerpAngle(prev.Yaw, next.Yaw, 0.5f);
        CharacterBody.GlobalRotation = rot;

        // Interpolate pitch
        if (ThirdPersonWeaponMesh != null)
        {
            var camRot = ThirdPersonWeaponMesh.GlobalRotation;
            camRot.X = Mathf.Lerp(prev.AimPitch, next.AimPitch, 0.5f);
            ThirdPersonWeaponMesh.GlobalRotation = camRot;
        }
    }

    private void SendClientCommand(InputCommand cmd)
    {
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
        //Vector3 dir = -Camera.GlobalTransform.Basis.Z;
        //Weapon.TryFire(Camera.GlobalPosition, dir);
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

    public void CorrectClientPosition(ArenaCharacterSnapshot serverSnapshot, double delta)
    {
        if (!IsLocal) return; // only correct local player

        Vector3 serverPos = serverSnapshot.Position;
        Vector3 localPos = CharacterBody.GlobalPosition;
        float distance = serverPos.DistanceTo(localPos);

        if (distance >= SnapThreshold)
        {
            // Big difference? Snap immediately
            CharacterBody.GlobalPosition = serverPos;
        }
        else if (distance >= CorrectionThreshold)
        {
            // Small difference? Smoothly lerp
            CharacterBody.GlobalPosition = localPos.Lerp(
                serverPos,
                (float)(CorrectionSpeed * delta)
            );
        }
    }
}