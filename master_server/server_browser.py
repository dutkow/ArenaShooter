from flask import Flask, request, jsonify
import time
import logging
import threading

app = Flask("Retro RTS server Server")
logging.basicConfig(level=logging.INFO)

lobbies = {}
server_TIMEOUT = 30
pending_clients = {}  # server_id -> list of pending clients
LONG_POLL_TIMEOUT = 15  # seconds

lock = threading.Lock()


# ------------------- HTTP Endpoints -------------------

@app.route("/register_server", methods=["POST"])
def register_server():
    """
    Host registers a server.
    Only IP, port, and server metadata are stored.
    """
    data = request.json
    required_fields = ["port", "name", "map", "game_mode", "max_players",
                       "num_connected_players", "connected_player_names"]
    missing = [f for f in required_fields if f not in data]
    if missing:
        return jsonify({"status": "error", "message": f"Missing fields: {missing}"}), 400

    # Detect host's public IP
    forwarded_for = request.headers.get("X-Forwarded-For")
    public_ip = forwarded_for.split(",")[0].strip() if forwarded_for else request.remote_addr

    server_id = f"{public_ip}:{data['port']}"

    server_data = {
        "ip": public_ip,
        "port": data["port"],
        "name": data["name"],
        "map": data["map"],
        "game_mode": data["game_mode"],
        "max_players": data["max_players"],
        "num_connected_players": data["num_connected_players"],
        "connected_player_names": (
            data["connected_player_names"] if isinstance(data["connected_player_names"], list) else []
        ),
        "last_seen": time.time(),
        "metadata": data.get("metadata", {})  # optional extra info
    }

    with lock:
        lobbies[server_id] = server_data
        pending_clients.setdefault(server_id, [])

    app.logger.info(f" server registered: {server_id}")
    return jsonify({"status": "ok", "server_id": server_id}), 200


@app.route("/lobbies", methods=["GET"])
def get_lobbies():
    """
    Returns active lobbies for clients to browse.
    """
    now = time.time()
    with lock:
        active = [
            {"server_id": lid, **server}
            for lid, server in lobbies.items()
            if now - server["last_seen"] < server_TIMEOUT
        ]
    return jsonify(active), 200


@app.route("/join_server", methods=["POST"])
def join_server():
    """
    Client requests to join a server. Added to pending clients.
    """
    data = request.json
    server_id = data.get("server_id")
    client_port = data.get("port")
    if server_id not in lobbies:
        return jsonify({"status": "error", "message": "server not found"}), 404

    forwarded_for = request.headers.get("X-Forwarded-For")
    client_ip = forwarded_for.split(",")[0].strip() if forwarded_for else request.remote_addr

    app.logger.info(f" Client {client_ip}:{client_port} wants to join {server_id}")

    with lock:
        pending_clients.setdefault(server_id, []).append({
            "ip": client_ip,
            "port": client_port,
            "join_time": time.time()
        })

    return jsonify({"status": "ok"}), 200


@app.route("/pending_clients", methods=["GET"])
def get_pending_clients():
    """
    Host long-polls for clients trying to join.
    """
    server_id = request.args.get("server_id")
    if not server_id:
        return jsonify([])

    start = time.time()
    while time.time() - start < LONG_POLL_TIMEOUT:
        with lock:
            clients = pending_clients.get(server_id, [])
            if clients:
                pending_clients[server_id] = []  # clear after sending
                return jsonify(clients)
        time.sleep(0.1)

    return jsonify([])  # timeout


# ------------------- Cleanup -------------------

def cleanup_lobbies():
    while True:
        time.sleep(30)
        now = time.time()
        with lock:
            to_delete = [lid for lid, server in lobbies.items() if now - server["last_seen"] > server_TIMEOUT]
            for lid in to_delete:
                lobbies.pop(lid, None)
                pending_clients.pop(lid, None)
                app.logger.info(f" Cleaned up stale server {lid}")


threading.Thread(target=cleanup_lobbies, daemon=True).start()


# ------------------- Server Run -------------------

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
