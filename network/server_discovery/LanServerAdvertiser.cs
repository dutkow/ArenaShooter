using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

/// <summary>
/// Broadcasts server info over the LAN so clients can discover available servers.
/// </summary>
public partial class LanServerAdvertiser
{
    private const int BroadcastPort = 42070;
    private UdpClient _udp;
    private ServerInfo _info;
    private System.Timers.Timer _timer;

    public void StartBroadcast(ServerInfo info)
    {
        _info = info;

        // Setup UDP client
        _udp = new UdpClient();
        _udp.EnableBroadcast = true;

        // Setup C# timer
        _timer = new System.Timers.Timer(250); // fires every 1000ms = 1s
        _timer.Elapsed += (sender, e) => Broadcast();
        _timer.AutoReset = true;
        _timer.Start();

        GD.Print("LAN broadcast started!");
    }

    private void Broadcast()
    {
        try
        {
            if (_udp == null)
            {
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(_info.ToString());
            _udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, BroadcastPort));
        }
        catch (Exception ex)
        {
            GD.PrintErr("Broadcast failed: " + ex);
        }
    }

    public void StopBroadcast()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _udp?.Close();
        _udp = null;
        GD.Print("broadcast stopped");
    }
}