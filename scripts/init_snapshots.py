import os
import random
import argparse
import psycopg2
from psycopg2.errors import UniqueViolation
from dotenv import load_dotenv

load_dotenv()

# 데이터베이스 연결 정보
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

def insert_ranking_snapshots(season_id, round_id):
    """ 시즌과 라운드의 랭킹 스냅샷을 삽입 (중복 오류 시 무시) """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                cursor.execute("""
                    SELECT p.avatar_address, p.score, u.clan_id
                    FROM participants p
                    JOIN users u ON p.avatar_address = u.avatar_address
                    WHERE p.season_id = %s
                """, (season_id,))
                participants = cursor.fetchall()

                success_count = 0
                for avatar_address, score, clan_id in participants:
                    try:
                        cursor.execute("""
                            INSERT INTO ranking_snapshots (season_id, clan_id, round_id, avatar_address, score, created_at)
                            VALUES (%s, %s, %s, %s, %s, now())
                        """, (season_id, clan_id, round_id, avatar_address, score))
                        success_count += 1
                    except UniqueViolation:
                        print(f"⚠️ 스냅샷 {avatar_address} 는 이미 존재하여 건너뜀.")
                        conn.rollback()  # 트랜잭션을 롤백하여 다음 삽입이 정상적으로 진행되도록 함

                conn.commit()
                print(f"✅ Ranking Snapshot {success_count}명 추가 완료")
    except Exception as e:
        print(f"❌ 참가자 추가 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="스냅샷 초기화 스크립트")
    parser.add_argument("season_id", type=int, help="season_id")
    parser.add_argument("round_id", type=int, help="round_id")
    args = parser.parse_args()

    if args.season_id and args.round_id:
        insert_ranking_snapshots(args.season_id, args.round_id)
    else:
        print("❌ 현재 블록 인덱스에 해당하는 시즌 및 라운드를 찾을 수 없습니다.")
