import os
import argparse
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv
import pandas as pd
import csv

load_dotenv()

# 데이터베이스 연결 정보 설정
CONNECTION_STRING = os.getenv("DB_CONNECTION_STRING", "Host=localhost;Database=arena;Username=yourusername;Password=yourpassword")

def convert_connection_string(dotnet_string):
    mapping = {
        "Host": "host",
        "Database": "dbname",
        "Username": "user",
        "Password": "password",
        "Port": "port"
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

def import_csv_to_db(conn, table_name, csv_path, temp_file=None, transform_func=None):
    """
    PostgreSQL COPY 명령어를 사용하여 CSV 파일을 DB로 가져옵니다.
    
    Args:
        conn: psycopg2 연결 객체
        table_name: 데이터를 가져올 테이블 이름
        csv_path: CSV 파일 경로
        temp_file: 임시 파일 경로 (변환이 필요한 경우)
        transform_func: CSV 데이터 변환 함수 (필요한 경우)
    """
    try:
        # 파일이 존재하는지 확인
        if not os.path.exists(csv_path):
            print(f"⚠️ {os.path.basename(csv_path)} 파일이 존재하지 않아 건너뜁니다.")
            return False
            
        # 만약 변환 함수가 있고 임시 파일 이름이 제공된 경우
        if transform_func and temp_file:
            # 원본 CSV를 변환하여 임시 파일로 저장
            transform_func(csv_path, temp_file)
            file_to_import = temp_file
        else:
            file_to_import = csv_path

        with conn.cursor() as cursor:
            with open(file_to_import, 'r', encoding='utf-8') as f:
                # 헤더 읽기 (첫 번째 줄)
                header = next(csv.reader([f.readline()]))
                
                # COPY 명령 실행
                cursor.copy_expert(
                    sql=f"COPY {table_name} ({', '.join(header)}) FROM STDIN WITH CSV HEADER",
                    file=open(file_to_import, 'r', encoding='utf-8')
                )
                
            conn.commit()
            print(f"✅ {table_name} 테이블에 {os.path.basename(csv_path)} 데이터 가져오기 완료")
            
            # 변환된 임시 파일이 있으면 삭제
            if transform_func and temp_file and os.path.exists(temp_file):
                os.remove(temp_file)
            
            return True
                
    except Exception as e:
        conn.rollback()
        print(f"❌ {table_name} 테이블로 데이터 가져오기 중 오류 발생: {e}")
        raise

def transform_available_opponents(input_path, output_path):
    """
    available_opponents.csv 파일을 변환하여 success_battle_id를 NULL로 설정합니다.
    """
    df = pd.read_csv(input_path)
    # success_battle_id 열을 NULL로 설정
    df['success_battle_id'] = None
    df.to_csv(output_path, index=False)

def update_success_battle_id(conn, opponents_csv_path):
    """
    available_opponents 테이블의 success_battle_id를 원본 CSV 파일의 값으로 업데이트합니다.
    """
    if not os.path.exists(opponents_csv_path):
        print(f"⚠️ {os.path.basename(opponents_csv_path)} 파일이 존재하지 않아 success_battle_id 업데이트를 건너뜁니다.")
        return
        
    try:
        df = pd.read_csv(opponents_csv_path)
        valid_updates = df[df['success_battle_id'].notna()]
        
        with conn.cursor() as cursor:
            for _, row in valid_updates.iterrows():
                cursor.execute(
                    sql.SQL("""
                        UPDATE available_opponents
                        SET success_battle_id = %s
                        WHERE id = %s
                    """),
                    (row['success_battle_id'], row['id'])
                )
            conn.commit()
            print(f"✅ available_opponents 테이블의 success_battle_id 업데이트 완료 ({len(valid_updates)}개 행)")
    except Exception as e:
        conn.rollback()
        print(f"❌ success_battle_id 업데이트 중 오류 발생: {e}")

def reset_sequences(conn):
    """
    하드코딩된 테이블 목록의 레코드 수를 조회하여 시퀀스를 재설정합니다.
    """
    try:
        # 시퀀스를 재설정할 테이블 목록 - 하드코딩
        tables = [
            "clans",
            "seasons",
            "rounds",
            "battles",
            "available_opponents",
            "refresh_ticket_policies",
            "refresh_ticket_statuses_per_round",
            "refresh_ticket_purchase_logs",
            "refresh_ticket_usage_logs",
            "battle_ticket_policies",
            "battle_ticket_statuses_per_round",
            "battle_ticket_statuses_per_season",
            "battle_ticket_purchase_logs",
            "battle_ticket_usage_logs"
        ]
        
        with conn.cursor() as cursor:
            for table_name in tables:
                # 테이블 존재 여부 확인
                cursor.execute("""
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = %s
                    )
                """, (table_name,))
                
                if not cursor.fetchone()[0]:
                    print(f"⚠️ {table_name} 테이블이 존재하지 않아 시퀀스 재설정을 건너뜁니다.")
                    continue
                
                # 시퀀스 이름 생성
                sequence_name = f"{table_name}_id_seq"
                
                # 시퀀스 존재 여부 확인
                cursor.execute("""
                    SELECT EXISTS (
                        SELECT FROM pg_sequences
                        WHERE schemaname = 'public'
                        AND sequencename = %s
                    )
                """, (sequence_name,))
                
                if not cursor.fetchone()[0]:
                    print(f"⚠️ {sequence_name} 시퀀스가 존재하지 않아 재설정을 건너뜁니다.")
                    continue
                
                # 테이블의 레코드 수 조회
                cursor.execute(f"SELECT COUNT(*) FROM {table_name}")
                record_count = cursor.fetchone()[0]
                
                # 시퀀스 값 설정 (레코드 수 + 여유 값)
                buffer = 1000
                next_id = record_count + buffer
                
                # 시퀀스 재설정
                cursor.execute(f"ALTER SEQUENCE {sequence_name} RESTART WITH {next_id}")
                
                print(f"✅ {table_name} 테이블의 시퀀스를 {next_id}로 재설정했습니다 (레코드 수: {record_count})")
                
            conn.commit()
            print("✅ 모든 테이블의 시퀀스 재설정이 완료되었습니다")
    except Exception as e:
        conn.rollback()
        print(f"❌ 시퀀스 재설정 중 오류가 발생했습니다: {e}")

def main(folder_path):
    # 임시 파일 경로
    temp_file_path = os.path.join(os.path.dirname(folder_path), "temp_available_opponents.csv")
    
    # CSV 파일 경로 생성
    users_csv = os.path.join(folder_path, "users.csv")
    refresh_ticket_policies_csv = os.path.join(folder_path, "refresh_ticket_policies.csv")
    battle_ticket_policies_csv = os.path.join(folder_path, "battle_ticket_policies.csv")
    clans_csv = os.path.join(folder_path, "clans.csv")
    seasons_csv = os.path.join(folder_path, "seasons.csv")
    rounds_csv = os.path.join(folder_path, "rounds.csv")
    participants_csv = os.path.join(folder_path, "participants.csv")
    medals_csv = os.path.join(folder_path, "medals.csv")
    ranking_snapshots_csv = os.path.join(folder_path, "ranking_snapshots.csv")
    refresh_ticket_statuses_per_round_csv = os.path.join(folder_path, "refresh_ticket_statuses_per_round.csv")
    refresh_ticket_purchase_logs_csv = os.path.join(folder_path, "refresh_ticket_purchase_logs.csv")
    refresh_ticket_usage_logs_csv = os.path.join(folder_path, "refresh_ticket_usage_logs.csv")
    available_opponents_csv = os.path.join(folder_path, "available_opponents.csv")
    battles_csv = os.path.join(folder_path, "battles.csv")
    battle_ticket_statuses_per_round_csv = os.path.join(folder_path, "battle_ticket_statuses_per_round.csv")
    battle_ticket_statuses_per_season_csv = os.path.join(folder_path, "battle_ticket_statuses_per_season.csv")
    battle_ticket_purchase_logs_csv = os.path.join(folder_path, "battle_ticket_purchase_logs.csv")
    battle_ticket_usage_logs_csv = os.path.join(folder_path, "battle_ticket_usage_logs.csv")
    
    # 폴더 존재 확인
    if not os.path.exists(folder_path):
        print(f"❌ {folder_path} 폴더가 존재하지 않습니다.")
        return
    
    # 존재하는 파일 목록 확인
    existing_files = [f for f in os.listdir(folder_path) if f.endswith('.csv')]
    if not existing_files:
        print(f"❌ {folder_path} 폴더에 CSV 파일이 존재하지 않습니다.")
        return
    
    print(f"📋 {folder_path} 폴더에서 다음 CSV 파일을 찾았습니다: {', '.join(existing_files)}")
    
    # 임포트할 테이블과 CSV 파일 매핑
    table_csv_mapping = {
        "users": users_csv,
        "refresh_ticket_policies": refresh_ticket_policies_csv,
        "battle_ticket_policies": battle_ticket_policies_csv,
        "clans": clans_csv,
        "seasons": seasons_csv,
        "rounds": rounds_csv,
        "participants": participants_csv,
        "medals": medals_csv,
        "ranking_snapshots": ranking_snapshots_csv,
        "refresh_ticket_statuses_per_round": refresh_ticket_statuses_per_round_csv,
        "refresh_ticket_purchase_logs": refresh_ticket_purchase_logs_csv,
        "refresh_ticket_usage_logs": refresh_ticket_usage_logs_csv,
        "battles": battles_csv,
        "battle_ticket_statuses_per_round": battle_ticket_statuses_per_round_csv,
        "battle_ticket_statuses_per_season": battle_ticket_statuses_per_season_csv,
        "battle_ticket_purchase_logs": battle_ticket_purchase_logs_csv,
        "battle_ticket_usage_logs": battle_ticket_usage_logs_csv
    }
    
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            # 1단계: users, refresh_ticket_policies, battle_ticket_policies, clans
            print("👉 1단계 임포트 시작: users, refresh_ticket_policies, battle_ticket_policies, clans")
            for table in ["users", "refresh_ticket_policies", "battle_ticket_policies", "clans"]:
                import_csv_to_db(conn, table, table_csv_mapping[table])
            
            # 2단계: seasons, rounds
            print("\n👉 2단계 임포트 시작: seasons, rounds")
            for table in ["seasons", "rounds"]:
                import_csv_to_db(conn, table, table_csv_mapping[table])
            
            # 3단계: participants, medals, ranking_snapshots, refresh_ticket_statuses_per_round, 
            # refresh_ticket_purchase_logs, refresh_ticket_usage_logs
            print("\n👉 3단계 임포트 시작: participants, medals, ranking_snapshots, refresh_ticket_statuses_per_round, refresh_ticket_purchase_logs, refresh_ticket_usage_logs")
            for table in ["participants", "medals", "ranking_snapshots", "refresh_ticket_statuses_per_round", 
                         "refresh_ticket_purchase_logs", "refresh_ticket_usage_logs"]:
                import_csv_to_db(conn, table, table_csv_mapping[table])
            
            # 4단계: available_opponents - success_battle_id를 NULL로 변경하여 임포트
            print("\n👉 4단계 임포트 시작: available_opponents (success_battle_id를 NULL로 변경)")
            if os.path.exists(available_opponents_csv):
                import_csv_to_db(conn, "available_opponents", available_opponents_csv, 
                                temp_file=temp_file_path, transform_func=transform_available_opponents)
            else:
                print(f"⚠️ {os.path.basename(available_opponents_csv)} 파일이 존재하지 않아 건너뜁니다.")
            
            # 5단계: battles
            print("\n👉 5단계 임포트 시작: battles")
            import_csv_to_db(conn, "battles", table_csv_mapping["battles"])
            
            # 6단계: battle_ticket_statuses_per_round, battle_ticket_statuses_per_season, 
            # battle_ticket_purchase_logs, battle_ticket_usage_logs
            print("\n👉 6단계 임포트 시작: battle_ticket_statuses_per_round, battle_ticket_statuses_per_season, battle_ticket_purchase_logs, battle_ticket_usage_logs")
            for table in ["battle_ticket_statuses_per_round", "battle_ticket_statuses_per_season", 
                         "battle_ticket_purchase_logs", "battle_ticket_usage_logs"]:
                import_csv_to_db(conn, table, table_csv_mapping[table])
            
            # 7단계: available_opponents의 success_battle_id 복구
            print("\n👉 7단계 임포트 시작: available_opponents의 success_battle_id 복구")
            update_success_battle_id(conn, available_opponents_csv)
            
            # 8단계: 모든 테이블의 시퀀스 재설정
            print("\n👉 8단계 진행: 모든 테이블의 자동 증가 시퀀스 재설정")
            reset_sequences(conn)
            
            print("\n🎉 데이터 임포트가 완료되었습니다!")
            
    except Exception as e:
        print(f"\n❌ 데이터 임포트 중 오류가 발생했습니다: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="CSV 파일을 PostgreSQL 데이터베이스로 가져오는 스크립트")
    parser.add_argument("folder_path", type=str, help="CSV 파일이 있는 폴더 경로")
    args = parser.parse_args()
    
    main(args.folder_path)
