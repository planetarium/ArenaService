import json
import os
import psycopg2
from psycopg2 import sql
import redis
from dotenv import load_dotenv

load_dotenv()

# Configuration
CONNECTION_STRING = os.getenv("DB_CONNECTION_STRING", "Host=localhost;Database=arena;Username=yourusername;Password=yourpassword")
REDIS = os.getenv("REDIS_HOST", "localhost:6379:0")
JSON_FILE_PATH = "./arena_data.json"

def convert_connection_string(dotnet_string):
    mapping = {
        "Host": "host",
        "Database": "dbname",
        "Username": "user",
        "Password": "password",
    }
    components = dotnet_string.split(";")
    converted = []
    for component in components:
        if "=" in component:
            key, value = component.split("=", 1)
            key = mapping.get(key, key).lower()
            converted.append(f"{key}={value}")
    return " ".join(converted)

CONVERTED_CONNECTION_STRING = convert_connection_string(CONNECTION_STRING)

def parse_redis_config(redis_config):
    try:
        host, port, db = redis_config.split(":")
        return host, int(port), int(db)
    except ValueError:
        raise ValueError("REDIS_HOST format should be 'host:port:db'.")

def insert_policy():
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                cursor.execute(
                    sql.SQL("""
                        INSERT INTO battle_ticket_policies (name, default_tickets_per_round, max_purchasable_tickets_per_round, purchase_prices, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    ("battle-ticket-normal-season-policy", 5, 4, [1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 2.8, 3.0, 3.2, 3.4, 3.6, 3.8, 4.0, 4.2, 4.4, 4.6, 4.8, 5.0, 5.2, 5.4, 5.6, 5.8, 6.0, 6.2, 6.4, 6.6, 6.8, 7.0, 7.2, 7.4, 7.6, 7.8, 8.0, 8.2, 8.4, 8.6, 8.8])
                )
                battle_policy_id = cursor.fetchone()[0]
                conn.commit()
                print(f"Policy {battle_policy_id} and details inserted successfully.")

                cursor.execute(
                    sql.SQL("""
                        INSERT INTO refresh_ticket_policies (name, default_tickets_per_round, max_purchasable_tickets_per_round, purchase_prices, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    ("refresh-ticket-normal-season-policy", 2, 4, [0.5, 1.5, 3, 4.5])
                )
                refresh_policy_id = cursor.fetchone()[0]
                conn.commit()
                return battle_policy_id, refresh_policy_id
    except Exception as e:
        print(f"An error occurred while inserting the policy: {e}")
        return None

def insert_season(start_block, end_block, round_interval, battle_policy_id, refresh_policy_id):
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                cursor.execute(
                    sql.SQL("""
                        INSERT INTO seasons (start_block, end_block, arena_type, round_interval, required_medal_count, total_prize, battle_ticket_policy_id, refresh_ticket_policy_id, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    (start_block, end_block, 1, round_interval, 120, 10000, battle_policy_id, refresh_policy_id)
                )
                season_id = cursor.fetchone()[0]
                conn.commit()
                print(f"Season {season_id} inserted successfully.")
                return season_id
    except Exception as e:
        print(f"An error occurred while inserting the season: {e}")
        return None

def insert_rounds(season_id, start_block, end_block, round_interval):
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                current_start = start_block
                round_number = 1
                while current_start < end_block:
                    current_end = min(current_start + round_interval - 1, end_block)
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO rounds (season_id, start_block, end_block, created_at, updated_at)
                            VALUES (%s, %s, %s, now(), now())
                        """),
                        (season_id, current_start, current_end)
                    )
                    current_start = current_end + 1
                    round_number += 1
                conn.commit()
                print(f"Rounds for season {season_id} inserted successfully.")
    except Exception as e:
        print(f"An error occurred while inserting rounds: {e}")

def insert_participants(season_id, participants):
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                for participant in participants:
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO users (agent_address, avatar_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s, now(), now())
                            ON CONFLICT (avatar_address) DO NOTHING
                        """),
                        (participant["avatarAddr"][2:], participant["avatarAddr"][2:], participant["nameWithHash"], participant["portraitId"], participant["cp"], participant["level"])
                    )
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO participants (avatar_address, season_id, initialized_score, score, total_win, total_lose, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s, now(), now())
                        """),
                        (participant["avatarAddr"][2:], season_id, 1000, 1000, 0, 0)
                    )
                conn.commit()
                print(f"{len(participants)} participants successfully inserted for season {season_id}.")
    except Exception as e:
        print(f"An error occurred while inserting participants: {e}")

def initialize_redis_participants(redis_client, season_id, participants):
    ranking_key = f"ranking:season:{season_id}:round:1"
    for participant in participants:
        avatar_address = participant["avatarAddr"][2:]
        initial_score = 1000
        member_key = f"participant:{avatar_address}"
        redis_client.zadd(ranking_key, {str(member_key): float(initial_score)})
    print(f"{len(participants)} participants initialized in Redis for season {season_id}.")

def main():
    try:
        # Redis configuration
        host, port, db = parse_redis_config(REDIS)
        redis_client = redis.StrictRedis(host=host, port=port, db=db, decode_responses=True)
        redis_client.ping()
        print("Connected to Redis.")

        # Input block index
        start_block = int(input("Enter the starting block index: "))
        end_block = start_block + 100000
        round_interval = 1000

        # Insert policy and season
        battle_policy_id, refresh_policy_id = insert_policy()
        if not refresh_policy_id or not battle_policy_id:
            return

        season_id = insert_season(start_block, end_block, round_interval, battle_policy_id, refresh_policy_id)
        if not season_id:
            return

        # Insert rounds
        insert_rounds(season_id, start_block, end_block, round_interval)

        # Load participants from JSON
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

        # Insert participants into database
        insert_participants(season_id, participants)

        # Initialize participants in Redis
        initialize_redis_participants(redis_client, season_id, participants)

    except Exception as e:
        print(f"An unexpected error occurred: {e}")

if __name__ == "__main__":
    main()
