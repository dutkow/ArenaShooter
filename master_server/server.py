# debug_lobby_poster.py
import requests
import time
import traceback
import random

BASE_URL = "https://vincegamedev.pythonanywhere.com"
HEARTBEAT_SECONDS = 3
NUM_LOBBIES = 3  # number of fake lobbies to rotate

# Pre-generate some fake lobby templates
lobby_templates = [
    {
        "port": 6969 + i,
        "name": f"Debug Lobby #{i+1}",
        "map": random.choice(["Ancient Desert", "Oasis", "Riverlands"]),
        "game_mode": random.choice(["Deathmatch", "Team Death"]),
        "max_players": random.choice([4, 6, 8]),
        "num_connected_players": random.randint(0, 2),
        "connected_player_names": ["Alice", "Bob"][:random.randint(0, 2)]
    } for i in range(NUM_LOBBIES)
]

def post_lobby(lobby):
    try:
        response = requests.post(f"{BASE_URL}/register_lobby", json=lobby, timeout=10)
        print(f"[{time.strftime('%H:%M:%S')}] POST '{lobby['name']}' -> {response.status_code}")
    except Exception:
        print(f"[{time.strftime('%H:%M:%S')}] POST error for '{lobby['name']}':")
        traceback.print_exc()

def main():
    print("Debug Lobby Poster running. Press Ctrl+C to stop.")
    try:
        while True:
            for lobby in lobby_templates:
                # Randomize number of connected players slightly
                lobby["num_connected_players"] = random.randint(0, lobby["max_players"])
                lobby["connected_player_names"] = [
                    f"BOT_{chr(65+random.randint(0,25))}{random.randint(1,99)}"
                    for _ in range(lobby["num_connected_players"])
                ]
                post_lobby(lobby)
            time.sleep(HEARTBEAT_SECONDS)
    except KeyboardInterrupt:
        print("Stopped by user.")
    except Exception:
        traceback.print_exc()
    finally:
        input("Press Enter to exit...")

if __name__ == "__main__":
    main()
