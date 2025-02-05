def calculate_group_range(group_ranges, total_ranking, my_ranking):
    result = {}
    for group, (start, end) in group_ranges.items():
        # 계산된 범위를 정수로 변환
        start_rank = int(my_ranking * start)
        end_rank = int(my_ranking * end)
        
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
