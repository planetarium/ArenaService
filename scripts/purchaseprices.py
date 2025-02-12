def generate_sequence(length, start, step):
    return [round(start + step * i, 1) for i in range(length)]

if __name__ == "__main__":
    length = int(input("길이를 입력하세요: "))
    start = float(input("초기 숫자를 입력하세요: "))
    step = float(input("증폭 값을 입력하세요: "))

    sequence = generate_sequence(length, start, step)
    print(sequence)
