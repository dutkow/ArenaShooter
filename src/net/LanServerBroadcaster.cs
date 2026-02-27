using Godot;
using System.Net;
using System.Net.Sockets;
using System.Text;

public partial class LanServerBroadcaster : Node
{
    private const int BroadcastPort = 42070;
    private UdpClient _udp;
    private ServerInfo _info;

    public void StartBroadcast(ServerInfo info)
    {
        _info = info;
        _udp = new UdpClient();
        _udp.EnableBroadcast = true;

        var timer = new Timer
        {
            WaitTime = 1.0,
            Autostart = true
        };

        timer.Timeout += Broadcast;
        AddChild(timer);
    }

    private void Broadcast()
    {
        byte[] data = Encoding.UTF8.GetBytes(_info.ToString());
        _udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, BroadcastPort));
    }
}