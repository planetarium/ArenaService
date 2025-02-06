import os
import argparse
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
                    ("battle-ticket-season-policy", 5, 4, [1.0,1.2,1.4,1.6,1.8,2.0,2.2,2.4,2.6,2.8,3.0,3.2,3.4,3.6,3.8,4.0,4.2,4.4,4.6,4.8,5.0,5.2,5.4,5.6,5.8,6.0,6.2,6.4,6.6,6.8,7.0,7.2,7.4,7.6,7.8,8.0,8.2,8.4,8.6,8.8])
                )
                conn.commit()

                cursor.execute(
                    sql.SQL("""
                        INSERT INTO battle_ticket_policies (name, default_tickets_per_round, max_purchasable_tickets_per_round, purchase_prices, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    ("battle-ticket-off-season-policy", 5, 4, [1.0,1.2,1.4,1.6,1.8,2.0,2.2,2.4,2.6,2.8])
                )
                conn.commit()

                cursor.execute(
                    sql.SQL("""
                        INSERT INTO refresh_ticket_policies (name, default_tickets_per_round, max_purchasable_tickets_per_round, purchase_prices, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    ("refresh-ticket-season-policy", 2, 4, [0.5, 1.5, 3, 4.5])
                )
                conn.commit()

                cursor.execute(
                    sql.SQL("""
                        INSERT INTO refresh_ticket_policies (name, default_tickets_per_round, max_purchasable_tickets_per_round, purchase_prices, created_at, updated_at)
                        VALUES (%s, %s, %s, %s, now(), now())
                        RETURNING id
                    """),
                    ("refresh-ticket-off-season-policy", 2, 4, [0.5, 1.5, 3, 4.5])
                )
                conn.commit()

    except Exception as e:
        print(f"❌ 정책 삽입 중 오류 발생: {e}")
        

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="배틀 및 리프레시 티켓 정책을 삽입하는 스크립트")
    args = parser.parse_args()

    insert_policy()