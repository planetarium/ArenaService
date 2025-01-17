import json
import os
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv

load_dotenv()

CONNECTION_STRING = os.getenv("DB_CONNECTION_STRING", "Host=localhost;Database=arena;Username=yourusername;Password=yourpassword")

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

JSON_FILE_PATH = "./arena_data.json"

def insert_participants(season_id, participants):
    try:
        # Connect to the database
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                for participant in participants:
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO users (agent_address, avatar_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                        """),
                        (participant["avatarAddr"][2:], participant["avatarAddr"][2:], participant["nameWithHash"], participant["portraitId"], participant["cp"], participant["level"], "now()", "now()")
                    )
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO participants (avatar_address, season_id, initialized_score, score, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s)
                        """),
                        (participant["avatarAddr"][2:], season_id, 1000, 1000, "now()", "now()")
                    )
                    conn.commit()
                print(f"{len(participants)} participants successfully inserted for season {season_id}.")
    except Exception as e:
        print(f"An error occurred: {e}")

def main():
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

    insert_participants(season_id=int(season_id), participants=participants)

if __name__ == "__main__":
    main()
