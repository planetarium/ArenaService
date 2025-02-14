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

def insert_season_and_rounds(file_path):
    """ CSV 파일을 읽고 시즌 및 라운드를 삽입 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                with open(file_path, "r", encoding="utf-8") as csvfile:
                    reader = csv.DictReader(csvfile)
                    for row in reader:
                        start_block = int(row["start_block"])
                        end_block = int(row["end_block"])
                        required_medal_count = int(row["required_medal_count"])
                        arena_type = int(row["arena_type"])
                        round_interval = int(row["round_interval"])
                        total_prize = int(row["total_prize"])
                        season_group_id = int(row["season_group_id"])
                        battle_ticket_policy_id = int(row["battle_ticket_policy_id"])
                        refresh_ticket_policy_id = int(row["refresh_ticket_policy_id"])

                        # round_count를 정확하게 계산
                        round_count = ((end_block - start_block) + 1) // round_interval

                        cursor.execute(
                            sql.SQL("""
                                INSERT INTO seasons (start_block, end_block, arena_type, round_interval, required_medal_count, total_prize, battle_ticket_policy_id, refresh_ticket_policy_id, prize_detail_url, season_group_id, created_at, updated_at)
                                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, now(), now())
                                RETURNING id
                            """),
                            (start_block, end_block, arena_type, round_interval, required_medal_count, total_prize, battle_ticket_policy_id, refresh_ticket_policy_id, "https://discord.com/channels/539405872346955788/1027763804643262505", season_group_id)
                        )
                        season_id = cursor.fetchone()[0]
                        conn.commit()
                        print(f"✅ 시즌 {season_id} 삽입 완료 (블록: {start_block} ~ {end_block}, 라운드 개수: {round_count})")

                        insert_rounds(cursor, season_id, round_interval, start_block, end_block, round_count)
                        conn.commit()
    except Exception as e:
        print(f"❌ 시즌 삽입 중 오류 발생: {e}")

def insert_rounds(cursor, season_id, round_interval, start_block, end_block, round_count):
    """ 시즌에 맞는 라운드 데이터를 삽입 (라운드 종료 인덱스가 시즌 종료 인덱스와 정확히 일치해야 함) """
    try:
        current_start = start_block
        for round_number in range(1, round_count + 1):
            current_end = current_start + round_interval - 1

            cursor.execute(
                sql.SQL("""
                    INSERT INTO rounds (season_id, start_block, end_block, created_at, updated_at)
                    VALUES (%s, %s, %s, now(), now())
                """),
                (season_id, current_start, current_end)
            )

            current_start = current_end + 1
        print(f"✅ 시즌 {season_id} 라운드 삽입 완료 ({round_count}개 생성, 종료 블록: {end_block})")
    except Exception as e:
        print(f"❌ 라운드 삽입 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="시즌 및 라운드를 삽입하는 스크립트")
    parser.add_argument("chain", type=str, choices=["thor", "heimdall", "odin"], help="체인 선택 (thor, heimdall, odin)")
    parser.add_argument("--csv-folder", type=str, default="./arena-sheets", help="CSV 파일이 위치한 폴더 경로")
    args = parser.parse_args()

    file_path = os.path.join(args.csv_folder, f"{args.chain}.csv")
    if os.path.exists(file_path):
        insert_season_and_rounds(file_path)
    else:
        print(f"⚠️ 파일 {file_path} 이(가) 존재하지 않습니다.")
