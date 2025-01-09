import json
import os
import redis
from dotenv import load_dotenv

load_dotenv()

REDIS = os.getenv("REDIS_HOST", "localhost:6379:0")

JSON_FILE_PATH = "./arena_data.json"

def parse_redis_config(redis_config):
    try:
        host, port, db = redis_config.split(":")
        return host, int(port), int(db)
    except ValueError:
        raise ValueError("REDIS_HOST format should be 'host:port:db'.")
        
def initialize_redis_participants(redis_client, season_id, participants):
    ranking_key = f"ranking:season:{season_id}"

    for participant in participants:
        avatar_address = participant["avatarAddr"][2:]
        initial_score = 1000

        # Add participant to the sorted set in Redis with an initial score
        redis_client.zadd(
            ranking_key, {f"participant:{avatar_address}:{season_id}": initial_score}
        )

    print(f"{len(participants)} participants initialized in Redis for season {season_id}.")

def main():
    try:
        # Parse REDIS configuration
        host, port, db = parse_redis_config(REDIS)
        redis_client = redis.StrictRedis(
            host=host,
            port=port,
            db=db,
            decode_responses=True,
        )

        # Ensure Redis connection is working
        redis_client.ping()
        print("Connected to Redis.")

        season_id = input("Enter the Season ID: ")
        if not season_id.isdigit():
            print("Invalid Season ID. Please enter a numeric value.")
            return

        try:
            with open(JSON_FILE_PATH, "r") as file:
                data = json.load(file)
                participants = data.get("arenaParticipants", [])
        except FileNotFoundError:
            print(f"JSON file not found at path: {JSON_FILE_PATH}")
            return
        except json.JSONDecodeError as e:
            print(f"Error parsing JSON: {e}")
            return

        initialize_redis_participants(redis_client, season_id, participants)

    except ValueError as e:
        print(f"Configuration error: {e}")
    except redis.ConnectionError as e:
        print(f"Redis connection error: {e}")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")

if __name__ == "__main__":
    main()
