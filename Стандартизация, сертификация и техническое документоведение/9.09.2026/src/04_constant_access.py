from time import perf_counter

def measure_time(func, data, repeats=1):
    start = perf_counter()
    for _ in range(repeats):
        func(data)
    elapsed = perf_counter() - start
    return elapsed / repeats

def get_middle(data):
    return data[len(data) // 2]

for n in [
    1_000,
    10_000,
    100_000,
    1_000_000,
]:
    numbers = list(range(n))

    elapsed = measure_time(
        get_middle,
        numbers,
        repeats=100
    )

    print(
        f"n={n:>8} | "
        f"time={elapsed:.10f}"
    )
