using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public enum MovementState
{
    GROUNDED,
    FALLING
}

public partial class ArenaCharacter : CharacterBody3D, IPossessable, INetworkedObject, IDamageable, IPlayerEntity
{
    // ----------------------
    // Exports & Components
    // ----------------------
    [Export] public MeshInstance3D CharacterMesh;
    [Export] public Weapon Weapon;
    [Export] public MeshInstance3D ThirdPersonWeaponMesh;
    [Export] public Camera3D Camera;
    [Export] public Marker3D CameraPivot;

    public Pawn pawn;

    [Export] public float MouseSens = 0.09f;
    [Export] public float MouseSmooth = 50f;

    [Export] Node3D _deathCamPivot;

    public CharacterMovementComponent MovementComp { get; private set; } = new();
    public HealthComponent HealthComponent { get; private set; } = new();
    public PossessableComponent PossessableComponent { get; private set; } = new();
    public NetworkedComponent NetworkedComponent { get; private set; } = new();

    // ----------------------
    // State
    // ----------------------
    public PlayerState State { get; private set; }

    private bool _weaponsEnabled = true;

    private Vector2 _cameraInput = Vector2.Zero;
    private Vector2 _rotVelocity = Vector2.Zero;

    public float Yaw => GlobalRotation.Y;
    public float AimPitch => CameraPivot.Rotation.X;

    // ----------------------
    // Networking
    // ----------------------
    public ArenaCharacterSnapshot LastSnapshot;
    public InputCommand LastInputCommand;

    private double _tickAccumulator = 0f;

    // ----------------------
    // Position Correction
    // ----------------------
    [Export] public float SnapThreshold = 2f;
    [Export] public float CorrectionThreshold = 0.1f;
    [Export] public float CorrectionSpeed = 10f;


    // ----------------------
    // Command history
    // ----------------------
    private SortedDictionary<uint, ClientInputCommand> _pendingCommands = new SortedDictionary<uint, ClientInputCommand>();

    public uint LastAppliedTick;

    private const int CommandHistorySize = 64;
    private Queue<ClientInputCommand> _commandHistory = new Queue<ClientInputCommand>();

    public void AddToHistory(ClientInputCommand cmd)
    {
        _commandHistory.Enqueue(cmd);
        if (_commandHistory.Count > CommandHistorySize)
            _commandHistory.Dequeue();
    }


    private Vector3 _predictedPosition;

    // ----------------------
    // Initialization
    // ----------------------

    public void Initialize(PlayerState state)
    {
        if (state == null)
        {
            GD.PushError("State was null on arena character initialization");
            return;
        }

        State = state;
        State.Pawn = pawn;

        MovementComp.SetCharacter(this);

        HealthComponent.SetOwner(this);
    }


    public void HandleRemoteSpawn()
    {
        Camera.Current = false;
        SetProcessInput(false);
        ShowThirdPersonView();

        NetworkedComponent.SetRole(NetworkRole.REMOTE);
    }

    public void SetInputEnabled(bool enabled)
    {
        PossessableComponent.SetInputEnabled(enabled);
    }

    public void OnPossessed(Controller controller)
    {
        PossessableComponent.OnPossessed(controller);

        HealthComponent.Death += OnDeath;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        SetProcessInput(true);
        ShowFirstPersonView();

        Camera.Current = true;

        NetworkedComponent.SetRole(NetworkRole.LOCAL);
        UIRoot.Instance.OnPossessedArenaCharacter(this);
    }

    public void OnUnpossessed()
    {
        PossessableComponent.OnUnpossessed();
    }

    public NetworkRole GetNetworkRole() => NetworkedComponent.Role;
    public bool IsAuthority() => NetworkedComponent.IsAuthority;
    public byte GetPlayerID() => State.PlayerID;
    public bool IsPlayerControlled() => true;

    // ----------------------
    // Views
    // ----------------------

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
        ThirdPersonWeaponMesh.Visible = false;
        CharacterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
    }

    // ----------------------
    // Damage
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
        var snapshot = new ArenaCharacterSnapshot();
        snapshot.PlayerID = State.PlayerID;
        snapshot.Position = GlobalPosition;
        snapshot.Velocity = MovementComp.Velocity;
        snapshot.Yaw = Yaw;
        snapshot.AimPitch = AimPitch;
        snapshot.Health = (byte)HealthComponent.Health;
        snapshot.Shield = (byte)HealthComponent.Shield;

        return snapshot;
    }

    public void HandleClientCommand(ClientCommand clientCommand)
    {
        foreach (var cmd in clientCommand.Commands)
        {
            if (cmd.TickNumber <= LastAppliedTick)
            {
                continue; // ignore already-applied ticks
            }

            if (!_pendingCommands.ContainsKey(cmd.TickNumber))
            {
                _pendingCommands.Add(cmd.TickNumber, cmd);
            }
        }
    }

    private ClientInputCommand _currentServerCommand;

    private void SetNextClientCommand()
    {
        if (_pendingCommands.Count == 0)
        {
            return;
        }

        uint nextTick = _pendingCommands.Keys.Min();
        ClientInputCommand cmd = _pendingCommands[nextTick];

        _currentServerCommand = cmd;

        _pendingCommands.Remove(nextTick);
        LastAppliedTick = nextTick;

        AddToHistory(cmd);
    }

    public void ApplySnapshot(ArenaCharacterSnapshot snapshot, float deltaTime = 0f)
    {
        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Position))
            LastSnapshot.Position = snapshot.Position;

        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Velocity))
            LastSnapshot.Velocity = snapshot.Velocity;

        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Yaw))
            LastSnapshot.Yaw = snapshot.Yaw;

        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.AimPitch))
            LastSnapshot.AimPitch = snapshot.AimPitch;

        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Health))
        {
            LastSnapshot.Health = snapshot.Health;
            HealthComponent.SetHealth(snapshot.Health);
        }

        if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Shield))
        {
            LastSnapshot.Shield = snapshot.Shield;
            HealthComponent.SetShield(snapshot.Shield);
        }


        if (!NetworkedComponent.IsLocal)
        {
            Vector3 targetPos = GlobalPosition;
            Vector3 targetVel = MovementComp.Velocity;
            bool predictedPositionChanged = false;

            if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Position))
            {
                targetPos = LastSnapshot.Position;
                predictedPositionChanged = true;
            }

            if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Velocity))
            {
                targetVel = LastSnapshot.Velocity;
                predictedPositionChanged = true;
            }

            if (predictedPositionChanged)
            {
                // #TODO. this should be changed.

                //Vector3 predicted = targetPos + targetVel * deltaTime;
                //GlobalPosition = predicted;


                //it should instead simulate all input since the last server tick and predict a new position.
                // i.e., iterate over command history
                // so like do this on every input, then check position, 
                _predictedPosition = LastSnapshot.Position;

                Vector3 startingPosition = GlobalPosition;
                GlobalPosition = _predictedPosition;
                foreach (var cmd in _commandHistory.Where(c => c.TickNumber > MatchState.Instance.LastAppliedServerTick))
                {
                    // Apply movement inputs to your movement component manually
                    MovementComp.Tick(cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL, CameraPivot);
                    GlobalPosition = _predictedPosition;
                }

                _predictedPosition = GlobalPosition;
                GlobalPosition = startingPosition;
            }

            if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.Yaw))
            {
                var rot = GlobalRotation;
                rot.Y = LastSnapshot.Yaw;
                GlobalRotation = rot;
            }

            if (snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.AimPitch) && ThirdPersonWeaponMesh != null)
            {
                var camRot = ThirdPersonWeaponMesh.GlobalRotation;
                camRot.X = Mathf.Clamp(LastSnapshot.AimPitch, -1.5f, 1.5f);
                ThirdPersonWeaponMesh.GlobalRotation = camRot;
            }
        }
    }

    // ----------------------
    // Input
    // ----------------------

    public override void _Input(InputEvent @event)
    {
        if (!PossessableComponent.InputActive) return;

        if (@event is InputEventMouseMotion mouseEvent &&
            Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _cameraInput = mouseEvent.Relative;
        }

        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

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

        RotateY(-Mathf.DegToRad(_rotVelocity.X));
        _cameraInput = Vector2.Zero;


        if (!NetworkedComponent.IsAuthority)
        {
            if(NetworkedComponent.IsLocal)
            {
                CorrectClientPosition(LastSnapshot, delta);
            }
            else
            {
                InterpolateRemoteSnapshot(delta);
            }
        }
    }

    // ----------------------
    // Physics
    // ----------------------

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (NetworkedComponent.IsAuthority)
        {
            if(NetworkedComponent.IsLocal)
            {
                MovementComp.Tick(CaptureInput(), delta, CameraPivot);

            }
            else
            {
                SetNextClientCommand();

                GlobalRotation = new Vector3(0.0f, _currentServerCommand.Yaw, 0.0f);
                CameraPivot.Rotation = new Vector3(_currentServerCommand.Pitch, 0.0f, 0.0f);

                MovementComp.Tick(_currentServerCommand.Input, delta, CameraPivot);
            }

            Vector3 dir = -Camera.GlobalTransform.Basis.Z;
            Weapon.TickWeapon(delta, Camera.GlobalPosition, dir);
        }

        if (NetworkedComponent.IsLocal && !NetworkedComponent.IsAuthority)
        {
            InputCommand cmd = CaptureInput();
            MovementComp.Tick(cmd, delta, CameraPivot);

            _tickAccumulator += delta;

            if (_tickAccumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
            {
                _tickAccumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
                SendClientCommand(cmd);
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

        LastInputCommand = cmd;

        Weapon.HandleInput(cmd);

        return cmd;
    }

    private void SendClientCommand(InputCommand cmd)
    {
        // Capture current tick as a TickCommand
        ClientInputCommand tickCmd = new ClientInputCommand
        {
            TickNumber = MatchState.Instance.CurrentTick,
            Input = cmd, // use the captured input
            Yaw = GlobalRotation.Y,
            Pitch = CameraPivot.GlobalRotation.X
        };

        // Add to local history queue
        AddToHistory(tickCmd);

        // Grab the last N commands from history
        int batchSize = 4;
        ClientInputCommand[] historyArray = _commandHistory.ToArray();
        int startIdx = Mathf.Max(0, historyArray.Length - batchSize);
        int length = historyArray.Length - startIdx;
        ClientInputCommand[] commandsToSend = new ClientInputCommand[length];
        Array.Copy(historyArray, startIdx, commandsToSend, 0, length);

        // Send the batch directly
        ClientCommand.Send(commandsToSend, MatchState.Instance.LastAppliedServerTick);
    }

    // ----------------------
    // Remote Interpolation
    // ----------------------

    private void InterpolateRemoteSnapshot(double delta)
    {

        if (NetworkedComponent.IsLocal || NetworkedComponent.IsAuthority) return;

        Vector3 target = LastSnapshot.Position + LastSnapshot.Velocity * (float)delta;
        GlobalPosition = GlobalPosition.Lerp(target, 10f * (float)delta);

        var rot = GlobalRotation;
        rot.Y = Mathf.LerpAngle(rot.Y, LastSnapshot.Yaw, 10f * (float)delta);
        GlobalRotation = rot;

        if (ThirdPersonWeaponMesh != null)
        {
            var camRot = ThirdPersonWeaponMesh.GlobalRotation;
            camRot.X = Mathf.Lerp(camRot.X, LastSnapshot.AimPitch, 10f * (float)delta);
            ThirdPersonWeaponMesh.GlobalRotation = camRot;
        }
    }

    // ----------------------
    // Client Correction
    // ----------------------

    public void CorrectClientPosition(ArenaCharacterSnapshot serverSnapshot, double delta)
    {
        if (!NetworkedComponent.IsLocal) return;

        Vector3 serverPos = serverSnapshot.Position;
        Vector3 localPos = GlobalPosition;

        float distance = serverPos.DistanceTo(localPos);

        if (distance >= SnapThreshold)
        {
            GlobalPosition = serverPos;
        }
        else if (distance >= CorrectionThreshold)
        {
            GlobalPosition = localPos.Lerp(
                serverPos,
                (float)(CorrectionSpeed * delta)
            );
        }
    }

    // ----------------------
    // Utility
    // ----------------------

    public void TeleportTo(Transform3D t)
    {
        GlobalTransform = t;
        MovementComp.ResetVelocity();
    }

    public void LaunchCharacter(Vector3 velocity)
    {
        MovementComp.Launch(velocity);
    }

    public void ResetMovement()
    {
        MovementComp.ResetVelocity();
    }

    public void SetWeaponsEnabled(bool enabled)
    {
        _weaponsEnabled = enabled;
    }

    // ----------------------
    // Death
    // ----------------------

    public void OnDeath()
    {
        if (NetworkedComponent.IsLocal)
        {
            if (Camera == null || _deathCamPivot == null)
                return;

            Camera.GlobalPosition = _deathCamPivot.GlobalPosition;
            Camera.GlobalRotation = _deathCamPivot.GlobalRotation;

            _deathCamPivot.AddChild(Camera);

            ShowThirdPersonView();
        }

        CharacterMesh.Visible = false;
    }

    public bool IsAlive()
    {
        return HealthComponent.IsAlive;
    }
}