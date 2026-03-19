using Godot;

using System.Collections.Generic;

public class CharacterPublicState
{
    public CharacterPublicFlags Flags;

    public Vector3 Position;
    public float Yaw;
    public float Pitch;
    public Vector3 Velocity;
    public CharacterMoveMode MovementMode;
    public WeaponType EquippedWeapon;

    // not replicated - derivable
    public bool IsGrounded;
    public bool WasLaunched;
    public Vector3 LaunchVelocity;
    public List<ICharacterCollidable> CurrentCollidables = new();
    public List<ICharacterCollidable> NewlyOverlappedCollidables = new();

    public Vector3 DesiredDirection;
    public float DesiredSpeed;
    public bool WantsToJump;
    public Vector3 GroundNormal;
    public Vector3 LastUnstuckPosition;
    public int ticksRemainingBeforeJump;


    public void ClearFlags()
    {
        Flags = 0;
    }

    public CharacterPublicState Copy()
    {
        return new CharacterPublicState
        {
            Flags = this.Flags,
            Position = this.Position,
            Yaw = this.Yaw,
            Pitch = this.Pitch,
            Velocity = this.Velocity,
            MovementMode = this.MovementMode,
            WasLaunched = this.WasLaunched,
            CurrentCollidables = new List<ICharacterCollidable>(CurrentCollidables)
        };
    }

    internal void Add(Message msg)
    {
        msg.Add(Position);
        msg.Add(Yaw);
        msg.Add(Yaw);
        msg.Add(Velocity);
        msg.AddEnum(MovementMode);
        msg.AddEnum(EquippedWeapon);
    }

    internal void Write(Message msg)
    {
        msg.Write(Position);
        msg.Write(Yaw);
        msg.Write(Pitch);
        msg.Write(Velocity);
        msg.WriteEnum(MovementMode);
        msg.WriteEnum(EquippedWeapon);
    }

    internal void Read(Message msg, bool forceFull = false)
    {
        msg.Read(out Position);
        msg.Read(out Yaw);
        msg.Read(out Pitch);
        msg.Read(out Velocity);
        msg.ReadEnum(out MovementMode);
        msg.ReadEnum(out EquippedWeapon);


    }
}