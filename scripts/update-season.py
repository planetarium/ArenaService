import os
import argparse
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv
import pandas as pd
from tabulate import tabulate
from math import ceil

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

def update_seasons(conn, csv_path, dry_run=False):
    """
    CSV 파일에서 시즌 정보를 읽어 데이터베이스의 시즌 정보를 업데이트합니다.
    
    Args:
        conn: psycopg2 연결 객체
        csv_path: 시즌 CSV 파일 경로
        dry_run: True인 경우 실제 변경 없이 예상 변경사항만 출력
    """
    try:
        # CSV 파일이 존재하는지 확인
        if not os.path.exists(csv_path):
            print(f"⚠️ {os.path.basename(csv_path)} 파일이 존재하지 않습니다.")
            return False
        
        # CSV 파일 읽기 - 데이터 타입 명시적 지정
        seasons_df = pd.read_csv(csv_path, dtype={
            'id': int,
            'start_block': int,
            'end_block': int,
            'arena_type': int,
            'round_interval': int,
            'required_medal_count': int,
            'total_prize': int,
            'battle_ticket_policy_id': int,
            'refresh_ticket_policy_id': int,
            'season_group_id': int
        })
        print(f"✅ {os.path.basename(csv_path)} 파일을 성공적으로 읽었습니다.")
        
        updated_seasons = []
        season_changes = []
        
        with conn.cursor() as cursor:
            for _, row in seasons_df.iterrows():
                # numpy 데이터 타입을 Python 기본 타입으로 변환
                season_id = int(row['id'])
                start_block = int(row['start_block'])
                end_block = int(row['end_block'])
                round_interval = int(row['round_interval'])
                
                # 기존 시즌 정보 가져오기
                cursor.execute("""
                    SELECT id, start_block, end_block, round_interval
                    FROM seasons
                    WHERE id = %s
                """, (season_id,))
                
                result = cursor.fetchone()
                if result:
                    current_id, current_start_block, current_end_block, current_round_interval = result
                    
                    # 블록 정보가 변경되었는지 확인
                    if current_start_block != start_block or current_end_block != end_block:
                        # 변경 로그 추가
                        season_changes.append({
                            'id': season_id,
                            'old_start': current_start_block,
                            'new_start': start_block,
                            'old_end': current_end_block,
                            'new_end': end_block,
                            'round_interval': round_interval
                        })
                        
                        # dry_run이 아닌 경우에만 실제 DB 업데이트
                        if not dry_run:
                            cursor.execute("""
                                UPDATE seasons
                                SET start_block = %s, end_block = %s, updated_at = NOW()
                                WHERE id = %s
                            """, (start_block, end_block, season_id))
                        
                        updated_seasons.append({
                            'id': season_id,
                            'old_start_block': current_start_block,
                            'old_end_block': current_end_block,
                            'new_start_block': start_block,
                            'new_end_block': end_block,
                            'round_interval': round_interval
                        })
                        
                        mode = "[DRY RUN] " if dry_run else ""
                        print(f"{mode}✅ 시즌 {season_id} 업데이트: {current_start_block}->{start_block}, {current_end_block}->{end_block}")
                else:
                    print(f"⚠️ 시즌 ID {season_id}를 찾을 수 없습니다.")
        
        # 변경 요약 테이블 출력
        if season_changes:
            print("\n📊 시즌 변경 요약:")
            headers = ["시즌 ID", "이전 시작 블록", "새 시작 블록", "이전 종료 블록", "새 종료 블록", "라운드 간격"]
            data = [[c['id'], c['old_start'], c['new_start'], c['old_end'], c['new_end'], c['round_interval']] for c in season_changes]
            print(tabulate(data, headers=headers, tablefmt="grid"))
        else:
            print("ℹ️ 변경할 시즌 정보가 없습니다.")
            
        return updated_seasons
    
    except Exception as e:
        print(f"❌ 시즌 업데이트 중 오류가 발생했습니다: {e}")
        return False

def adjust_rounds(conn, updated_seasons, current_block=None, dry_run=False):
    """
    업데이트된 시즌 정보에 기반하여 라운드 정보를 새로 생성합니다.
    현재 블록 이전의 모든 라운드는 유지하고, 현재 블록 이후의 라운드만 조정합니다.
    
    Args:
        conn: psycopg2 연결 객체
        updated_seasons: 업데이트된 시즌 정보 리스트
        current_block: 현재 블록 인덱스. 이 블록 이전의 라운드는 삭제하지 않음
        dry_run: True인 경우 실제 변경 없이 예상 변경사항만 출력
    """
    try:
        if not updated_seasons:
            print("ℹ️ 업데이트된 시즌이 없어 라운드 조정이 필요하지 않습니다.")
            return True
        
        # 삭제될/생성될 라운드 정보를 저장할 리스트
        deleted_rounds = []
        kept_rounds = []
        new_rounds = []
        
        with conn.cursor() as cursor:
            for season in updated_seasons:
                season_id = season['id']
                new_start_block = season['new_start_block']
                new_end_block = season['new_end_block']
                round_interval = season['round_interval']
                
                # 1. 현재 존재하는 라운드 가져오기
                cursor.execute("""
                    SELECT id, start_block, end_block
                    FROM rounds
                    WHERE season_id = %s
                    ORDER BY start_block
                """, (season_id,))
                
                existing_rounds = cursor.fetchall()
                
                if not existing_rounds:
                    print(f"ℹ️ 시즌 {season_id}에 기존 라운드가 없습니다. 새로 생성합니다.")
                    # 시즌 전체에 대한 라운드 생성
                    total_blocks = new_end_block - new_start_block + 1
                    num_rounds = ceil(total_blocks / round_interval)
                    
                    for i in range(num_rounds):
                        round_start = new_start_block + i * round_interval
                        
                        # 마지막 라운드인 경우
                        if i == num_rounds - 1 or round_start + round_interval > new_end_block:
                            round_end = new_end_block
                        else:
                            round_end = round_start + round_interval - 1
                        
                        # 새 라운드 생성
                        new_rounds.append({
                            'season_id': season_id,
                            'start_block': round_start,
                            'end_block': round_end
                        })
                        
                        # dry_run이 아닌 경우 실제 DB 삽입
                        if not dry_run:
                            cursor.execute("""
                                INSERT INTO rounds (season_id, start_block, end_block, created_at, updated_at)
                                VALUES (%s, %s, %s, NOW(), NOW())
                            """, (season_id, round_start, round_end))
                        
                        mode = "[DRY RUN] " if dry_run else ""
                        print(f"{mode}➕ 새 라운드 추가: 시즌 {season_id}, 범위 {round_start}-{round_end}")
                    
                    # 다음 시즌으로 건너뛰기
                    continue
                
                # 현재 블록을 포함하는 라운드 찾기
                current_round_end_block = None
                found_current_round = False
                
                if current_block:
                    for round_id, round_start_block, round_end_block in existing_rounds:
                        if round_start_block <= current_block <= round_end_block:
                            current_round_end_block = round_end_block
                            found_current_round = True
                            print(f"ℹ️ 현재 블록({current_block})을 포함하는 라운드를 찾았습니다: ID {round_id}, 범위 {round_start_block}-{round_end_block}")
                            break
                    
                    if not found_current_round:
                        print(f"⚠️ 현재 블록({current_block})을 포함하는 라운드를 찾지 못했습니다.")
                
                # 2. 현재 블록 이전/포함 라운드는 유지, 이후 라운드는 삭제 대상으로 표시
                for round_id, round_start_block, round_end_block in existing_rounds:
                    # 현재 블록이 지정되어 있고, 현재 블록을 포함하는 라운드를 찾은 경우
                    if current_block and found_current_round:
                        # 현재 블록 이전 또는 포함하는 라운드는 유지
                        if round_end_block <= current_round_end_block:
                            kept_rounds.append({
                                'id': round_id,
                                'season_id': season_id,
                                'start_block': round_start_block,
                                'end_block': round_end_block,
                                'reason': f"현재 블록({current_block}) 이전 또는 포함하는 라운드"
                            })
                            
                            mode = "[DRY RUN] " if dry_run else ""
                            print(f"{mode}🔒 라운드 {round_id} 유지: 블록 범위 {round_start_block}-{round_end_block}")
                            continue
                    
                    # 삭제 대상으로 표시
                    deleted_rounds.append({
                        'id': round_id,
                        'season_id': season_id,
                        'start_block': round_start_block,
                        'end_block': round_end_block,
                        'reason': "시즌 업데이트로 인한 라운드 재생성"
                    })
                    
                    # dry_run이 아닌 경우 실제 삭제
                    if not dry_run:
                        cursor.execute("""
                            DELETE FROM rounds
                            WHERE id = %s
                        """, (round_id,))
                    
                    mode = "[DRY RUN] " if dry_run else ""
                    print(f"{mode}🗑️ 라운드 {round_id} 삭제: 시즌 업데이트로 인한 라운드 재생성")
                
                # 3. 유지되는 마지막 라운드 이후부터 시즌 끝까지 새 라운드 생성
                start_from_block = new_start_block
                
                # 유지되는 라운드가 있으면 마지막 유지 라운드의 다음 블록부터 시작
                if kept_rounds and any(r['season_id'] == season_id for r in kept_rounds):
                    # 이 시즌의 유지되는 라운드 중 가장 큰 end_block 찾기
                    max_kept_end_block = max(
                        (r['end_block'] for r in kept_rounds if r['season_id'] == season_id), 
                        default=new_start_block - 1
                    )
                    start_from_block = max_kept_end_block + 1
                
                # 시작 블록이 시즌 종료 블록을 넘어가면 생성할 라운드 없음
                if start_from_block > new_end_block:
                    print(f"ℹ️ 시즌 {season_id}에 대해 추가로 생성할 라운드가 없습니다.")
                    continue
                
                # 시즌의 남은 블록 수
                remaining_blocks = new_end_block - start_from_block + 1
                
                # 필요한 라운드 수 계산 (올림)
                num_rounds = ceil(remaining_blocks / round_interval)
                
                # 라운드 생성
                for i in range(num_rounds):
                    round_start = start_from_block + i * round_interval
                    
                    # 마지막 라운드인 경우 또는 다음 라운드가 시즌 끝을 넘어갈 경우
                    if i == num_rounds - 1 or round_start + round_interval > new_end_block:
                        round_end = new_end_block
                    else:
                        round_end = round_start + round_interval - 1
                    
                    # 새 라운드 생성
                    new_rounds.append({
                        'season_id': season_id,
                        'start_block': round_start,
                        'end_block': round_end
                    })
                    
                    # dry_run이 아닌 경우 실제 DB 삽입
                    if not dry_run:
                        cursor.execute("""
                            INSERT INTO rounds (season_id, start_block, end_block, created_at, updated_at)
                            VALUES (%s, %s, %s, NOW(), NOW())
                        """, (season_id, round_start, round_end))
                    
                    mode = "[DRY RUN] " if dry_run else ""
                    print(f"{mode}➕ 새 라운드 추가: 시즌 {season_id}, 범위 {round_start}-{round_end}")
        
        # 변경 요약 출력
        if kept_rounds:
            print("\n📊 유지될 라운드 요약:")
            headers = ["라운드 ID", "시즌 ID", "시작 블록", "종료 블록", "이유"]
            data = [[r['id'], r['season_id'], r['start_block'], r['end_block'], r['reason']] for r in kept_rounds]
            print(tabulate(data, headers=headers, tablefmt="grid"))
            
        if deleted_rounds:
            print("\n📊 삭제될 라운드 요약:")
            headers = ["라운드 ID", "시즌 ID", "시작 블록", "종료 블록", "삭제 이유"]
            data = [[r['id'], r['season_id'], r['start_block'], r['end_block'], r['reason']] for r in deleted_rounds]
            print(tabulate(data, headers=headers, tablefmt="grid"))
        
        if new_rounds:
            print("\n📊 새로 생성될 라운드 요약:")
            headers = ["시즌 ID", "시작 블록", "종료 블록"]
            data = [[r['season_id'], r['start_block'], r['end_block']] for r in new_rounds]
            print(tabulate(data, headers=headers, tablefmt="grid"))
        
        if not deleted_rounds and not new_rounds and not kept_rounds:
            print("ℹ️ 변경할 라운드 정보가 없습니다.")
                
        return True
    
    except Exception as e:
        print(f"❌ 라운드 조정 중 오류가 발생했습니다: {e}")
        return False

def main(csv_path, current_block=None, dry_run=False):
    try:
        # CSV 파일 경로 확인
        if not os.path.exists(csv_path):
            print(f"❌ {csv_path} 파일이 존재하지 않습니다.")
            return
        
        mode = "[DRY RUN] " if dry_run else ""
        print(f"{mode}🔄 {os.path.basename(csv_path)} 파일을 사용하여 시즌 및 라운드 정보를 업데이트합니다...")
        if current_block:
            print(f"{mode}ℹ️ 현재 블록 인덱스: {current_block}. 이 블록 이전 또는 포함하는 라운드는 유지됩니다.")
        
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            # 1. 시즌 정보 업데이트
            print(f"\n{mode}👉 1단계: 시즌 정보 업데이트")
            updated_seasons = update_seasons(conn, csv_path, dry_run)
            
            if not updated_seasons:
                print(f"{mode}ℹ️ 업데이트할 시즌 정보가 없거나 오류가 발생했습니다.")
                return
            
            # 2. 라운드 정보 재생성
            print(f"\n{mode}👉 2단계: 라운드 정보 재생성")
            adjust_rounds(conn, updated_seasons, current_block, dry_run)
            
            # 변경사항 커밋 (dry_run이 아닌 경우에만)
            if not dry_run:
                conn.commit()
                print("\n🎉 시즌 및 라운드 정보 업데이트가 완료되었습니다!")
            else:
                # dry_run인 경우 롤백
                conn.rollback()
                print("\n🔍 DRY RUN 모드: 실제 변경은 적용되지 않았습니다.")
                print("💡 실제로 적용하려면 --dry-run 옵션 없이 명령을 실행하세요.")
            
    except Exception as e:
        print(f"\n❌ 오류가 발생했습니다: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="시즌 및 라운드 정보를 업데이트하는 스크립트")
    parser.add_argument("csv_path", type=str, help="업데이트할 시즌 정보가 있는 CSV 파일 경로")
    parser.add_argument("--current-block", "-b", type=int, help="현재 블록 인덱스. 이 블록 이전 또는 포함하는 라운드는 유지됩니다.")
    parser.add_argument("--dry-run", "-d", action="store_true", help="변경사항을 실제로 적용하지 않고 예상 변경사항만 출력")
    args = parser.parse_args()
    
    main(args.csv_path, args.current_block, args.dry_run)
