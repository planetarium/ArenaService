import os
import random
import string
import argparse
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv

load_dotenv()

# 데이터베이스 연결 정보 설정
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

def generate_random_clan_name():
    """ 무작위 클랜 이름 생성 """
    prefix = random.choice(["Storm", "Shadow", "Dragon", "Fire", "Ice", "Thunder", "Dark", "Light"])
    suffix = random.choice(["Warriors", "Knights", "Guardians", "Hunters", "Legends", "Raiders", "Champions"])
    return f"{prefix} {suffix}"

def insert_clan():
    """ 클랜 생성 및 ID 반환 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                clan_name = generate_random_clan_name()
                image_url = "https://example.com/default_clan_image.png"

                cursor.execute(
                    sql.SQL("""
                        INSERT INTO clans (name, image_url, created_at, updated_at)
                        VALUES (%s, %s, now(), now())
                        RETURNING id
                    """),
                    (clan_name, image_url)
                )
                clan_id = cursor.fetchone()[0]
                conn.commit()
                print(f"✅ 클랜 생성 완료: {clan_name} (ID: {clan_id})")
                return clan_id
    except Exception as e:
        print(f"❌ 클랜 삽입 중 오류 발생: {e}")
        return None

def assign_users_to_clan(clan_id):
    """ clan_id가 없는 유저 중 10명을 랜덤으로 선택하여 클랜에 배정 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                # clan_id가 NULL인 유저 중 랜덤 10명 선택
                cursor.execute(
                    sql.SQL("""
                        SELECT avatar_address FROM users
                        WHERE clan_id IS NULL
                        ORDER BY RANDOM()
                        LIMIT 10
                    """)
                )
                users = cursor.fetchall()

                if not users:
                    print("⚠️ 클랜에 할당할 유저가 없습니다.")
                    return

                # 선택된 유저들의 clan_id 업데이트
                avatar_addresses = [user[0] for user in users]
                cursor.execute(
                    sql.SQL("""
                        UPDATE users
                        SET clan_id = %s, updated_at = now()
                        WHERE avatar_address = ANY(%s)
                    """),
                    (clan_id, avatar_addresses)
                )
                conn.commit()
                print(f"✅ 클랜 {clan_id}에 {len(users)}명의 유저 배정 완료")
    except Exception as e:
        print(f"❌ 유저 클랜 배정 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="클랜을 생성하고 무작위 유저 10명을 배정하는 스크립트")
    args = parser.parse_args()

    TOTAL_CLANS = 10

    for _ in range(TOTAL_CLANS):
        clan_id = insert_clan()
        if clan_id:
            assign_users_to_clan(clan_id)
