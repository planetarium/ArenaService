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
                    ("battle-ticket-normal-season-policy", 5, 4, [1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.2, 2.4, 2.6, 2.8, 3.0, 3.2, 3.4, 3.6, 3.8, 4.0])
                )
                battle_policy_id = cursor.fetchone()[0]
                conn.commit()

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

                print(f"✅ 배틀 티켓 정책 ID: {battle_policy_id}, 리프레시 티켓 정책 ID: {refresh_policy_id}")
                return battle_policy_id, refresh_policy_id
    except Exception as e:
        print(f"❌ 정책 삽입 중 오류 발생: {e}")
        return None, None

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="배틀 및 리프레시 티켓 정책을 삽입하는 스크립트")
    parser.add_argument("--auto", action="store_true", help="자동 실행 플래그 (ID 입력 없이 실행)")
    args = parser.parse_args()

    battle_policy_id, refresh_policy_id = insert_policy()
    if args.auto:
        print(f"--auto 플래그 적용: {battle_policy_id} {refresh_policy_id}")
