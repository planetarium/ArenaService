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
REDIS_CONFIG = os.getenv("REDIS_HOST", "localhost:6379:0")

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

def parse_redis_config(redis_config):
    try:
        host, port, db = redis_config.split(":")
        return host, int(port), int(db)
    except ValueError:
        raise ValueError("REDIS_HOST format should be 'host:port:db'.")

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

                participants = [(u[0], season_id, random.randint(1000, 1200)) for u in users]

                cursor.executemany("""
                    INSERT INTO participants (avatar_address, season_id, initialized_score, score, total_win, total_lose, created_at, updated_at)
                    VALUES (%s, %s, %s, %s, 0, 0, now(), now())
                """, [(p[0], p[1], p[2], p[2]) for p in participants])

                conn.commit()
                print(f"✅ 시즌 {season_id} 참가자 {len(participants)}명 추가 완료")
    except Exception as e:
        print(f"❌ 참가자 추가 중 오류 발생: {e}")

def initialize_redis_participants(redis_client, season_id, round_id):
    """ Redis에 참가자 초기화 (현재 라운드 + 다음 라운드) """
    try:
        rounds = [round_id, round_id + 1]

        for r in rounds:
            ranking_key = f"season:{season_id}:round:{r}:ranking"
            redis_client.delete(ranking_key)

            with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT avatar_address, score FROM participants
                        WHERE season_id = %s
                    """, (season_id,))
                    participants = cursor.fetchall()

                    for avatar_address, score in participants:
                        participant_key = f"participant:{avatar_address}"
                        redis_client.zadd(ranking_key, {participant_key: float(score)})

            print(f"✅ Redis 참가자 랭킹 초기화 완료: 시즌 {season_id}, 라운드 {r}")
    except Exception as e:
        print(f"❌ 참가자 랭킹 초기화 오류: {e}")

def initialize_clan_ranking(redis_client, season_id, round_id):
    """ 클랜 랭킹 초기화 (현재 라운드 + 다음 라운드) """
    try:
        rounds = [round_id, round_id + 1]

        for r in rounds:
            clan_ranking_key = f"season:{season_id}:round:{r}:ranking-clan"
            redis_client.delete(clan_ranking_key)

            with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT u.clan_id, SUM(p.score)
                        FROM users u
                        JOIN participants p ON u.avatar_address = p.avatar_address
                        WHERE u.clan_id IS NOT NULL AND p.season_id = %s
                        GROUP BY u.clan_id
                    """, (season_id,))
                    clans = cursor.fetchall()

                    for (clan_id, total_score) in clans:
                        clan_key = f"clan:{clan_id}"
                        redis_client.zadd(clan_ranking_key, {clan_key: float(total_score)})

            print(f"✅ 클랜 랭킹 초기화 완료: 시즌 {season_id}, 라운드 {r}")
    except Exception as e:
        print(f"❌ 클랜 랭킹 초기화 오류: {e}")


def initialize_redis_group_ranking(redis_client, season_id, round_id):
    """ 그룹 랭킹 초기화 (현재 라운드 + 다음 라운드) """
    try:
        rounds = [round_id, round_id + 1]

        for r in rounds:
            group_ranking_key = f"season:{season_id}:round:{r}:ranking-group"
            redis_client.delete(group_ranking_key)

            with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT avatar_address, score FROM participants
                        WHERE season_id = %s
                    """, (season_id,))
                    participants = cursor.fetchall()

                    for avatar_address, score in participants:
                        participant_key = f"participant:{avatar_address}"
                        group_key = f"season:{season_id}:round:{r}:group:{score}"
                        redis_client.hset(group_key, participant_key, score)
                        redis_client.zadd(group_ranking_key, {group_key: float(score)})

            print(f"✅ 그룹 랭킹 초기화 완료: 시즌 {season_id}, 라운드 {r}")
    except Exception as e:
        print(f"❌ 그룹 랭킹 초기화 오류: {e}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="시즌 참가자 추가 및 Redis 초기화 스크립트")
    parser.add_argument("block_index", type=int, help="현재 블록 인덱스")
    args = parser.parse_args()

    season_id, round_id = get_current_season_and_round(args.block_index)

    if season_id and round_id:
        insert_participants(season_id)

        host, port, db = parse_redis_config(REDIS_CONFIG)
        redis_client = redis.StrictRedis(host=host, port=port, db=db, decode_responses=True)

        initialize_redis_participants(redis_client, season_id, round_id)
        initialize_clan_ranking(redis_client, season_id, round_id)
        initialize_redis_group_ranking(redis_client, season_id, round_id)
    else:
        print("❌ 현재 블록 인덱스에 해당하는 시즌 및 라운드를 찾을 수 없습니다.")
