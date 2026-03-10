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
            if (_udp == null) return;

            byte[] data = Encoding.UTF8.GetBytes(_info.ToJson());

            IPAddress IP = IPAddress.Parse(NetworkConstants.GetBroadcastIP());
            IPEndPoint endPoint = new IPEndPoint(IP, _info.Port);

            _udp.Send(data, data.Length, endPoint);

            GD.Print($"broadcasting. IP string: {_info.IP}. port = {_info.Port}");
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