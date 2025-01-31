import os
import csv
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

ROUND_INTERVAL = 7560
ARENA_TYPE_MAP = {"OffSeason": 0, "Season": 1, "Championship": 2}

def insert_season_and_rounds(file_path, battle_policy_id, refresh_policy_id):
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                with open(file_path, "r", encoding="utf-8") as csvfile:
                    reader = csv.DictReader(csvfile)
                    for row in reader:
                        start_block = int(row["start_block_index"])
                        end_block = int(row["end_block_index"])
                        required_medal_count = int(row["required_medal_count"])
                        arena_type = ARENA_TYPE_MAP.get(row["arena_type"], 1)  # 기본값 Season(1)

                        cursor.execute(
                            sql.SQL("""
                                INSERT INTO seasons (start_block, end_block, arena_type, round_interval, required_medal_count, total_prize, battle_ticket_policy_id, refresh_ticket_policy_id, created_at, updated_at)
                                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, now(), now())
                                RETURNING id
                            """),
                            (start_block, end_block, arena_type, ROUND_INTERVAL, required_medal_count, 10000, battle_policy_id, refresh_policy_id)
                        )
                        season_id = cursor.fetchone()[0]
                        conn.commit()
                        print(f"✅ 시즌 {season_id} 삽입 완료 (파일: {file_path})")

                        # 라운드 생성
                        insert_rounds(cursor, season_id, start_block, end_block)

    except Exception as e:
        print(f"❌ 시즌 삽입 중 오류 발생: {e}")

def insert_rounds(cursor, season_id, start_block, end_block):
    """ 주어진 시즌에 대해 라운드를 생성하는 함수 """
    try:
        current_start = start_block
        round_number = 1
        while current_start < end_block:
            current_end = min(current_start + ROUND_INTERVAL - 1, end_block)

            cursor.execute(
                sql.SQL("""
                    INSERT INTO rounds (season_id, start_block, end_block, created_at, updated_at)
                    VALUES (%s, %s, %s, now(), now())
                """),
                (season_id, current_start, current_end)
            )
            current_start = current_end + 1
            round_number += 1
        print(f"✅ 시즌 {season_id} 라운드 삽입 완료 ({round_number}개 생성)")
    except Exception as e:
        print(f"❌ 라운드 삽입 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="시즌 및 라운드를 삽입하는 스크립트")
    parser.add_argument("battle_policy_id", type=int, help="배틀 티켓 정책 ID")
    parser.add_argument("refresh_policy_id", type=int, help="리프레시 티켓 정책 ID")
    parser.add_argument("chain", type=str, choices=["thor", "heimdall", "odin"], help="체인 선택 (thor, heimdall, odin)")
    parser.add_argument("--csv-folder", type=str, default="./arena-sheets", help="CSV 파일이 위치한 폴더 경로")
    args = parser.parse_args()

    file_path = os.path.join(args.csv_folder, f"{args.chain}.csv")
    if os.path.exists(file_path):
        insert_season_and_rounds(file_path, args.battle_policy_id, args.refresh_policy_id)
    else:
        print(f"⚠️ 파일 {file_path} 이(가) 존재하지 않습니다.")
