import subprocess

# 1. 정책 삽입 실행
policy_result = subprocess.run(["python", "insert_policy.py", "--auto"], capture_output=True, text=True)
lines = policy_result.stdout.split("\n")

# 정책 ID 추출
battle_policy_id, refresh_policy_id = None, None
for line in lines:
    if "배틀 티켓 정책 ID" in line:
        ids = [int(s) for s in line.split() if s.isdigit()]
        if len(ids) == 2:
            battle_policy_id, refresh_policy_id = ids

if battle_policy_id and refresh_policy_id:
    # 2. 시즌 삽입 실행
    subprocess.run(["python", "insert_season.py", str(battle_policy_id), str(refresh_policy_id), "--csv-folder", "./arena-sheets"])
else:
    print("❌ 정책 ID를 가져오지 못했습니다.")
