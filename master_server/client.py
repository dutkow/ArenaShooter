# debug_client.py
import requests
import time
import traceback

BASE_URL = "https://vincegamedev.pythonanywhere.com"
HEARTBEAT_SECONDS = 3

def fetch_lobbies():
    try:
        response = requests.get(f"{BASE_URL}/lobbies", timeout=10)
        lobbies = response.json()
        print(f"[{time.strftime('%H:%M:%S')}] GET /lobbies -> {len(lobbies)} active")
        return lobbies
    except Exception:
        print(f"[{time.strftime('%H:%M:%S')}] GET /lobbies error:")
        traceback.print_exc()
        return []

def connect_to_lobby(lobby):
    join_payload = {
        "port": lobby["port"],
        "name": lobby["name"],
        "map": lobby.get("map", ""),
        "game_mode": lobby.get("game_mode", ""),
        "max_players": lobby.get("max_players", 4),
        "num_connected_players": (lobby.get("num_connected_players", 0) + 1),
        "connected_player_names": lobby.get("connected_player_names", []) + ["DebugClient"]
    }
    try:
        response = requests.post(f"{BASE_URL}/register_lobby", json=join_payload, timeout=10)
        print(f"[{time.strftime('%H:%M:%S')}] Connected to lobby '{lobby['name']}' -> {response.status_code}")
    except Exception:
        print(f"[{time.strftime('%H:%M:%S')}] Error connecting to lobby '{lobby['name']}':")
        traceback.print_exc()

def main():
    print("Debug Client running. Press Ctrl+C to stop.")
    try:
        while True:
            lobbies = fetch_lobbies()
            if lobbies:
                first_lobby = lobbies[0]
                connect_to_lobby(first_lobby)
            else:
                print(f"[{time.strftime('%H:%M:%S')}] No lobbies available")
            time.sleep(HEARTBEAT_SECONDS)
    except KeyboardInterrupt:
        print("Stopped by user.")
    finally:
        input("Press Enter to exit...")

if __name__ == "__main__":
    main()
