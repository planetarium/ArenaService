import os
import json
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

def load_participants_from_json(file_path):
    """ JSON 파일에서 참가자 데이터를 로드 """
    try:
        with open(file_path, "r", encoding="utf-8") as file:
            data = json.load(file)
            return data.get("data").get("stateQuery").get("arenaParticipants", [])
    except FileNotFoundError:
        print(f"⚠️ JSON 파일을 찾을 수 없습니다: {file_path}")
        return []
    except json.JSONDecodeError as e:
        print(f"❌ JSON 파일 파싱 오류: {e}")
        return []

def insert_users(participants):
    """ DB에 유저 정보 삽입 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                for participant in participants:
                    avatar_address = participant["avatarAddr"][2:]  # "0x" 제거
                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO users (agent_address, avatar_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s, now(), now())
                            ON CONFLICT (avatar_address) DO NOTHING
                        """),
                        (avatar_address, avatar_address, participant["nameWithHash"], int(participant["portraitId"]), int(participant["cp"]), int(participant["level"]))
                    )
                conn.commit()
                print(f"✅ {len(participants)}명의 유저 삽입 완료")
    except Exception as e:
        print(f"❌ 유저 삽입 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="유저 데이터를 삽입하는 스크립트")
    parser.add_argument("chain", type=str, choices=["thor", "heimdall", "odin"], help="체인 선택 (thor, heimdall, odin)")
    parser.add_argument("--data-folder", type=str, default="./arena-data", help="유저 데이터가 위치한 폴더 경로")
    args = parser.parse_args()

    file_path = os.path.join(args.data_folder, f"{args.chain}.json")
    participants = load_participants_from_json(file_path)

    if not participants:
        print("⚠️ 삽입할 유저 데이터가 없습니다.")
    else:
        insert_users(participants)
