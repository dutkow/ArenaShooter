using System;

[Flags]
public enum ENetPacketFlags : int
{
    None = 0,
    Reliable = 1,
    Unsequenced = 2,
    UnreliableFragment = 8
}
