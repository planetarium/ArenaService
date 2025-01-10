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

def add_season_and_intervals(start_block, end_block, interval):
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                # Insert the season
                cursor.execute(
                    sql.SQL("""
                        INSERT INTO seasons (start_block, end_block, interval)
                        VALUES (%s, %s, %s)
                        RETURNING id
                    """),
                    (start_block, end_block, interval)
                )
                season_id = cursor.fetchone()[0]
                print(f"Season {season_id} added with start_block={start_block}, end_block={end_block}, interval={interval}.")

                # Add intervals for the season
                current_block = start_block
                while current_block < end_block:
                    interval_end = min(current_block + interval, end_block)
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO rounds (season_id, start_block, end_block)
                            VALUES (%s, %s, %s)
                        """),
                        (season_id, current_block, interval_end)
                    )
                    print(f"Added interval: {current_block} - {interval_end}")
                    current_block = interval_end

                conn.commit()
                return season_id
    except Exception as e:
        print(f"An error occurred: {e}")
        return None

def main():
    try:
        start_block = int(input("Enter the Start Block: "))
        end_block = int(input("Enter the End Block: "))
        interval = int(input("Enter the Interval: "))

        if start_block >= end_block:
            print("End Block must be greater than Start Block.")
            return

        season_id = add_season_and_intervals(start_block, end_block, interval)
        if season_id is not None:
            print(f"Season {season_id} and intervals successfully added to the database.")

    except ValueError:
        print("Invalid input. Please enter numeric values for Start Block, End Block, and Interval.")
    except Exception as e:
        print(f"Unexpected error: {e}")

if __name__ == "__main__":
    main()
