import os
import random
import argparse
import psycopg2
import redis
from psycopg2 import sql
from dotenv import load_dotenv

load_dotenv()

# 데이터베이스 및 Redis 연결 정보
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

def get_current_season_and_round(block_index):
    """ 주어진 블록 인덱스로 현재 진행 중인 시즌과 라운드 찾기 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                cursor.execute("""
                    SELECT id FROM seasons
                    WHERE start_block <= %s AND end_block >= %s
                    ORDER BY id DESC LIMIT 1
                """, (block_index, block_index))
                season_result = cursor.fetchone()

                if not season_result:
                    print("⚠️ 해당 블록 인덱스에 해당하는 시즌이 없습니다.")
                    return None, None

                season_id = season_result[0]

                cursor.execute("""
                    SELECT id FROM rounds
                    WHERE season_id = %s AND start_block <= %s AND end_block >= %s
                    ORDER BY id DESC LIMIT 1
                """, (season_id, block_index, block_index))
                round_result = cursor.fetchone()

                if not round_result:
                    print("⚠️ 해당 블록 인덱스에 해당하는 라운드가 없습니다.")
                    return season_id, None

                round_id = round_result[0]
                return season_id, round_id
    except Exception as e:
        print(f"❌ 시즌 및 라운드 검색 오류: {e}")
        return None, None

def insert_ranking_snapshots(season_id, round_id):
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

                cursor.executemany("""
                    INSERT INTO ranking_snapshots (season_id, clan_id, round_id, avatar_address, score, created_at)
                    VALUES (%s, %s, %s, %s, %s, now())
                """, [(season_id, clan_id, round_id, avatar_address, score) 
                      for avatar_address, score, clan_id in participants])

                conn.commit()
                print(f"✅ Ranking Snapshot {len(participants)}명 추가 완료")
    except Exception as e:
        print(f"❌ 참가자 추가 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="스냅샷 초기화 스크립트")
    parser.add_argument("block_index", type=int, help="현재 블록 인덱스")
    args = parser.parse_args()

    season_id, round_id = get_current_season_and_round(args.block_index)

    if season_id and round_id:
        insert_ranking_snapshots(season_id, round_id)

    else:
        print("❌ 현재 블록 인덱스에 해당하는 시즌 및 라운드를 찾을 수 없습니다.")
