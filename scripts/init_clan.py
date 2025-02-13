import os
import csv
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv

# 환경 변수 로드
load_dotenv()

# DB 연결 정보
CONNECTION_STRING = os.getenv("DB_CONNECTION_STRING", "Host=localhost;Database=arena;Username=yourusername;Password=yourpassword")

# PostgreSQL 연결 문자열 변환
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

# CSV 파일 경로
CSV_FILE_PATH = "clan/thor.csv"

def get_clan_id(clan_name, conn):
    """ 클랜 ID를 조회하고 없으면 생성 """
    with conn.cursor() as cursor:
        cursor.execute("SELECT id FROM clans WHERE name = %s", (clan_name,))
        result = cursor.fetchone()
        if result:
            return result[0]

        # 클랜이 없으면 새로 생성
        cursor.execute("""
            INSERT INTO clans (name, image_url, created_at, updated_at)
            VALUES (%s, %s, now(), now())
            RETURNING id
        """, (clan_name, "https://example.com/default_clan_image.png"))
        conn.commit()
        return cursor.fetchone()[0]

def user_exists(avatar_address, conn):
    """ 유저 존재 여부 확인 """
    with conn.cursor() as cursor:
        cursor.execute("SELECT COUNT(*) FROM users WHERE avatar_address = %s", (avatar_address.lower(),))
        return cursor.fetchone()[0] > 0

def add_user(avatar_address, agent_address, conn):
    """ 유저 추가 (없을 경우만) """
    if not user_exists(avatar_address.lower(), conn):
        with conn.cursor() as cursor:
            cursor.execute("""
                INSERT INTO users (avatar_address, agent_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                VALUES (%s, %s, %s, %s, %s, %s, now(), now())
            """, (avatar_address.lower(), agent_address.lower(), "temp", 40100032, 1, 1))
        conn.commit()

def assign_user_to_clan(avatar_address, clan_id, conn):
    """ 유저를 클랜에 배정 (이미 배정된 경우 유지) """
    with conn.cursor() as cursor:
        cursor.execute("SELECT clan_id FROM users WHERE avatar_address = %s", (avatar_address.lower(),))
        result = cursor.fetchone()
        if result and result[0]:  # 이미 클랜이 할당된 경우
            return

        # 클랜 배정
        cursor.execute("""
            UPDATE users SET clan_id = %s, updated_at = now()
            WHERE avatar_address = %s
        """, (clan_id, avatar_address.lower()))
        conn.commit()

def process_csv():
    """ CSV 파일을 읽고 데이터베이스에 저장 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with open(CSV_FILE_PATH, newline="", encoding="utf-8") as csvfile:
                reader = csv.reader(csvfile)
                header = next(reader)  # 첫 번째 행 (헤더)

                # avatar_address 컬럼 인덱스 찾기
                avatar_indexes = [i for i, col in enumerate(header) if "Avatar Addreses" in col]
                agent_indexes = [i for i, col in enumerate(header) if "Agent Address" in col]

                for row in reader:
                    clan_name = row[0].strip()
                    clan_id = get_clan_id(clan_name, conn)

                    for idx in range(len(avatar_indexes)):
                        avatar_address = row[avatar_indexes[idx]].strip()
                        agent_address = row[agent_indexes[idx]].strip()

                        if not avatar_address:
                            continue  # 데이터가 없으면 건너뛰기

                        add_user(avatar_address.lower(), agent_address.lower(), conn)
                        assign_user_to_clan(avatar_address.lower(), clan_id, conn)

                print("✅ CSV 데이터 처리 완료")

    except Exception as e:
        print(f"❌ 데이터 처리 중 오류 발생: {e}")

if __name__ == "__main__":
    process_csv()
