import os
import random
import argparse
import psycopg2
from psycopg2 import sql
from psycopg2.errors import UniqueViolation
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

def insert_participants(season_id):
    """ 현재 시즌에 참가하지 않은 유저들을 `participants` 테이블에 추가 (점수는 1000~1200 랜덤) """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                cursor.execute("""
                    SELECT avatar_address FROM users
                    WHERE NOT EXISTS (
                        SELECT 1 FROM participants p WHERE p.avatar_address = users.avatar_address AND p.season_id = %s
                    )
                """, (season_id,))
                users = cursor.fetchall()

                if not users:
                    print("⚠️ 새로운 참가자가 없습니다.")
                    return

                success_count = 0
                for user in users:
                    avatar_address = user[0]
                    score = 1000
                    try:
                        cursor.execute("""
                            INSERT INTO participants (avatar_address, season_id, initialized_score, score, total_win, total_lose, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, 0, 0, now(), now())
                        """, (avatar_address, season_id, score, score))
                        success_count += 1
                    except UniqueViolation:
                        print(f"⚠️ 참가자 {avatar_address} 는 이미 존재하여 건너뜀.")
                        conn.rollback()  # 트랜잭션을 롤백하여 에러 상태 초기화

                conn.commit()
                print(f"✅ 시즌 {season_id} 참가자 {success_count}명 추가 완료")
    except Exception as e:
        print(f"❌ 참가자 추가 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="시즌 참가자 추가 스크립트")
    parser.add_argument("season_id", type=int, help="현재 시즌 ID")
    args = parser.parse_args()

    insert_participants(args.season_id)
