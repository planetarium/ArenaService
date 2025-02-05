import math

def calculate_group_range(group_ranges, total_ranking, my_ranking):
    result = {}
    for group, (start, end) in group_ranges.items():
        # 계산된 범위를 정수로 변환
        start_rank = math.ceil(my_ranking * start)
        end_rank = math.ceil(my_ranking * end)
        
        # 범위가 1 미만으로 내려가거나 전체 순위를 초과하지 않도록 조정
        start_rank = max(1, start_rank)
        start_rank = min(total_ranking, start_rank)
        end_rank = min(total_ranking, end_rank)
        
        result[group] = (start_rank, end_rank)
    return result

# 예제 데이터
group_ranges = {
    1: (0.2, 0.2),
    2: (0.4, 0.8),
    3: (0.8, 1.2), 
    4: (1.2, 1.8),
    5: (1.8, 3.0) 
}

# 사용자 입력 받기
total_ranking = int(input("총 참가자 수를 입력하세요: "))
my_ranking = int(input("내 랭킹을 입력하세요: "))

# 그룹 범위 계산
ranges = calculate_group_range(group_ranges, total_ranking, my_ranking)

# 결과 출력
for group, (start, end) in ranges.items():
    print(f"그룹 {group}: {start} ~ {end}")




# 1등이 조회했을 떄 그룹 5만 존재하면 그룹 5에서 뽑아주지만 그룹 n에 맞게 점수를 얻는다
# 2등이 조회했을 떄 그룹 1에서 이미 뽑은 아바타가 그룹 2에 들어가게 되면 해당 점수 그룹을 제외하고 다시 랜덤을 돌린다
# 항상 5명은 다른 5명이여야하고 그룹은 1~5가 전부 존재해야한다.

# 그냥 알 수 없는 이유로 그룹이 비어 있다면 아래와 같은 룰로 채운다
# 그룹 1이 비면 2,3,4,5로 내려가서 채운다
# 그룹 2가 비면 3,4,5로 내려가서 채운다
# 그룹 3이 비면 4,5,로 채우고
# 그룹 4가 비면 5로 채운다
# 그룹 5가 비면 4로 채운다


