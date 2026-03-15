using Godot;
using System;

public abstract class ServerAdvertiser
{
    public abstract void StartBroadcast(ServerInfo info);

    public abstract void Broadcast();
    public abstract void StopBroadcast();

}
