using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class ServerBrowserRequests
{
    public static ServerBrowserRequests Instance { get; } = new ServerBrowserRequests();

    private readonly HttpClient _httpClient = new HttpClient();

    public event Action RefreshServersStarted;
    public event Action<List<ServerInfo>> RefreshInternetServersFinished;
    public event Action<List<ServerInfo>> RefreshLanServersFinished;

    /// <summary>
    /// Refreshes the lobby list from the given URL.
    /// </summary>
    public async Task RefreshInternetServersAsync()
    {
        RefreshServersStarted?.Invoke();

        try
        {
            var response = await _httpClient.GetAsync(NetworkConstants.SERVER_BROWSER_ADDRESS);

            if (!response.IsSuccessStatusCode)
            {
                RefreshInternetServersFinished?.Invoke(new List<ServerInfo>());
                return;
            }

            string text = await response.Content.ReadAsStringAsync();

            // Set options for System.Text.Json
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) // handles GameMode enum
            }
            };

            try
            {
                var servers = JsonSerializer.Deserialize<List<ServerInfo>>(text, options);

                if (servers != null)
                {
                    // Ensure ConnectedPlayers is not null
                    foreach (var server in servers)
                    {
                        if (server.ConnectedPlayers == null)
                            server.ConnectedPlayers = new List<string>();
                    }

                    RefreshInternetServersFinished?.Invoke(servers);
                }
                else
                {
                    RefreshInternetServersFinished?.Invoke(new List<ServerInfo>());
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to parse server JSON: {e.Message}");
                RefreshInternetServersFinished?.Invoke(new List<ServerInfo>());
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Failed to fetch servers: {e.Message}");
            RefreshInternetServersFinished?.Invoke(new List<ServerInfo>());
        }
    }


    /// <summary>
    /// Posts a server to the master server.
    /// </summary>
    public async Task PostServerAsync(ServerInfo serverAdvertisement, string url)
    {
        try
        {
            string body = JsonSerializer.Serialize(serverAdvertisement);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Failed to post server: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"HTTP post failed: {e.Message}");
        }
    }

    /// <summary>
    /// Removes (unregisters) a server from the master server.
    /// </summary>
    public async Task RemoveInternetServerAsync(ServerInfo serverAdvertisement, string url)
    {
        try
        {
            // Typically you'd send identifying data (like IP + port or a lobby ID)
            var body = JsonSerializer.Serialize(new
            {
                ip = serverAdvertisement.IP,
                port = serverAdvertisement.Port
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Failed to remove lobby: {response.StatusCode}");
            }
            else
            {
                Console.WriteLine("Lobby successfully removed from server browser.");
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"HTTP remove failed: {e.Message}");
        }
    }


    // ----------------------
    // LAN Discovery
    // ----------------------
    public async void RefreshLanServersAsync(float listenSeconds = 0.3f)
    {
        var servers = await ListenForLanServersAsync(listenSeconds);
        RefreshLanServersFinished?.Invoke(servers);
    }

    private async Task<List<ServerInfo>> ListenForLanServersAsync(float listenSeconds)
    {
        var discoveredServers = new List<ServerInfo>();
        var seenServerIDs = new HashSet<string>();

        using (var listener = new UdpClient(NetworkHandler.Instance.LanBroadcastPort))
        {
            listener.EnableBroadcast = true;

            var timeout = DateTime.Now.AddSeconds(listenSeconds);

            while (DateTime.Now < timeout)
            {
                if (listener.Available > 0)
                {
                    var result = await listener.ReceiveAsync();

                    var data = Encoding.UTF8.GetString(result.Buffer);
                    var serverInfo = ServerInfo.FromJson(data);

                    if (serverInfo != null && !seenServerIDs.Contains(serverInfo.Name)) // or use a unique ID
                    {
                        discoveredServers.Add(serverInfo);
                        seenServerIDs.Add(serverInfo.Name);
                    }
                }

                await Task.Delay(50);
            }
        }

        return discoveredServers;
    }
}
