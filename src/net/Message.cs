using Godot;
using System;

/// <summary>
/// Message wraps a network payload and flags for sending.
/// </summary>
public class Message
{
    public byte[] Payload { get; private set; }
    public ENetPacketFlags ENetFlags { get; private set; }
    public int Flags => (int)ENetFlags;

    public Message(byte[] payload, ENetPacketFlags flags = ENetPacketFlags.Reliable)
    {
        Payload = payload;
        ENetFlags = flags;
    }

    public byte[] WriteMessage()
    {
        // Could prepend packet type or timestamp here if needed
        return Payload;
    }
}