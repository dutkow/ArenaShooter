using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class MasterServerHttpRequests
{
    public static MasterServerHttpRequests Instance { get; } = new MasterServerHttpRequests();

    private readonly HttpClient _httpClient = new HttpClient();

    public event Action RefreshServersStarted;
    public event Action<List<ServerAdvertisement>> RefreshServersCompleted;

    /// <summary>
    /// Refreshes the lobby list from the given URL.
    /// </summary>
    public async Task RefreshLobbiesAsync()
    {
        RefreshServersStarted?.Invoke();

        try
        {
            var response = await _httpClient.GetAsync(NetworkConstants.SERVER_BROWSER_ADDRESS);

            if (!response.IsSuccessStatusCode)
            {
                RefreshServersCompleted?.Invoke(new List<ServerAdvertisement>());
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
                var lobbies = JsonSerializer.Deserialize<List<ServerAdvertisement>>(text, options);

                if (lobbies != null)
                {
                    // Ensure ConnectedPlayers is not null
                    foreach (var lobby in lobbies)
                    {
                        if (lobby.ConnectedPlayers == null)
                            lobby.ConnectedPlayers = new List<string>();
                    }

                    RefreshServersCompleted?.Invoke(lobbies);
                }
                else
                {
                    RefreshServersCompleted?.Invoke(new List<ServerAdvertisement>());
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to parse server JSON: {e.Message}");
                RefreshServersCompleted?.Invoke(new List<ServerAdvertisement>());
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Failed to fetch servers: {e.Message}");
            RefreshServersCompleted?.Invoke(new List<ServerAdvertisement>());
        }
    }


    /// <summary>
    /// Posts a lobby to the server.
    /// </summary>
    public async Task PostLobbyAsync(ServerAdvertisement serverAdvertisement, string url)
    {
        try
        {
            string body = JsonSerializer.Serialize(serverAdvertisement);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Failed to POST lobby: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"HTTP POST failed: {e.Message}");
        }
    }

    /// <summary>
    /// Removes (unregisters) a lobby from the server browser.
    /// </summary>
    public async Task RemoveLobbyAsync(ServerAdvertisement serverAdvertisement, string url)
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
}
