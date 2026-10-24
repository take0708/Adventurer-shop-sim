"""Step 1 数値バランスの簡易シミュレーション（月単位で集計）。
プレイヤーは「そこそこ上手い」想定の自動戦略で動かす。
"""
import random, itertools, statistics, sys

# ---- 商品: name -> (系統, 仕入れ値, 売値, 解禁★)
ITEMS = {
    "回復薬":  ("道具", 6,   10,  1),
    "解毒薬":  ("道具", 9,   15,  2),
    "木の杖":  ("武器", 30,  50,  1),
    "銅の剣":  ("武器", 60,  100, 1),
    "鉄の剣":  ("武器", 180, 300, 3),
    "布の服":  ("防具", 24,  40,  1),
    "革の盾":  ("防具", 45,  75,  2),
    "鉄の鎧":  ("防具", 240, 400, 4),
}
# 行商人限定品: name -> (仕入れ値, 売値, 評判ボーナス, 出始める★)
# 売値はその★の客の予算で 4割ほどが買える水準
RARES = {
    "妖精の傷薬":   (45,  75,  3,  1),
    "霊薬":         (70,  120, 4,  2),
    "銀糸のローブ": (150, 250, 6,  3),
    "星屑の杖":     (270, 450, 8,  4),
    "竜鱗の盾":     (420, 700, 10, 5),
}
RARE_JOB = {"妖精の傷薬": None, "霊薬": "僧侶", "銀糸のローブ": "魔法使い",
            "星屑の杖": "魔法使い", "竜鱗の盾": "戦士"}  # None = 全職業

# 職業ごとの欲しいもの（重み）
WANTS = {
    "戦士":     {"銅の剣": 3, "鉄の剣": 3, "革の盾": 2, "鉄の鎧": 2, "回復薬": 2},
    "魔法使い": {"木の杖": 4, "布の服": 3, "回復薬": 2, "解毒薬": 1},
    "僧侶":     {"回復薬": 3, "解毒薬": 3, "布の服": 2, "木の杖": 1, "革の盾": 1},
}

STAR_REP     = [0, 0, 50, 220, 560, 1250]
STAR_VISITS  = [0, 15, 22, 32, 44, 58]
STAR_BUDGET  = [0, (40, 130), (40, 180), (80, 380), (150, 650), (250, 1000)]
MERCHANT_P   = [0, 0.20, 0.25, 0.30, 0.35, 0.40]  # 月ごとの行商人出現確率

SHELF_COST   = 100
SHELF_FLOW   = 15
SHELF_FLOW_TOOL = 40       # 道具棚はたくさん売れる
COUNTER_COST = 250
COUNTER_CAP  = 25            # カウンター1台が月にさばける客数
DECOR        = [("観葉植物", 80, 1), ("絨毯", 150, 2), ("甲冑の飾り", 300, 4)]
AMBIENCE_PER = 0.015
AMBIENCE_MAX = 20
SIZES        = [(6, 5, 6, 2, 0), (8, 6, 10, 3, 1500), (10, 7, 15, 4, 5000)]  # w,h,棚上限,カウンター上限,費用
START_MONEY  = 500
REP_OK, REP_PART, REP_NG, REP_QUEUE = 2, 0, -2, -1


def star_of(rep):
    s = 1
    for i in range(1, 6):
        if rep >= STAR_REP[i]:
            s = i
    return s


def make_customer(star, unlocked, rng):
    job = rng.choice(list(WANTS))
    pool = {k: w for k, w in WANTS[job].items() if k in unlocked}
    n = 1 if rng.random() < 0.6 else 2
    wants = []
    for _ in range(n):
        names, ws = zip(*pool.items())
        pick = rng.choices(names, ws)[0]
        if pick not in wants:
            wants.append(pick)
    lo, hi = STAR_BUDGET[star]
    return job, wants, rng.randint(lo, hi)


def serve(cust, shown, rares_left):
    job, wants, budget = cust
    bought, sales, cost, rep_bonus = 0, 0, 0, 0
    # レア品は職業が合って予算が足りれば優先して買う
    for r, cnt in rares_left.items():
        if cnt > 0 and RARE_JOB[r] in (None, job) and RARES[r][1] <= budget:
            rares_left[r] -= 1
            budget -= RARES[r][1]; sales += RARES[r][1]; cost += RARES[r][0]
            rep_bonus += RARES[r][2]
            break
    for w in wants:
        price = ITEMS[w][2]
        if shown.get(w, 0) > 0 and price <= budget:
            shown[w] -= 1
            budget -= price; sales += price; cost += ITEMS[w][1]; bought += 1
    if bought == len(wants): rep = REP_OK
    elif bought > 0: rep = REP_PART
    else: rep = REP_NG
    return sales, cost, rep + rep_bonus


def flow(item):
    return SHELF_FLOW_TOOL if ITEMS[item][0] == "道具" else SHELF_FLOW


def choose_display(star, shelves, unlocked, rng, visits):
    """棚の割り当て {商品: 棚の数}。品目選び→余った棚を需要の多い品へ。"""
    samples = [make_customer(star, unlocked, rng) for _ in range(150)]
    def value(alloc):
        v = 0
        scale = visits / len(samples)
        stock = {k: n * flow(k) / scale for k, n in alloc.items()}
        for c in samples:
            sl, co, rp = serve(c, stock, {})
            v += (sl - co) + rp * 15
        return v
    k = min(shelves, len(unlocked))
    best = max((dict.fromkeys(c, 1) for c in itertools.combinations(unlocked, k)), key=value)
    for _ in range(shelves - k):
        cand = []
        for item in best:
            a = dict(best); a[item] += 1
            cand.append((value(a), a))
        best = max(cand, key=lambda x: x[0])[1]
    return best


def run(seed, verbose=False):
    rng = random.Random(seed)
    money, rep = START_MONEY, 0
    shelves, counters, ambience, size = 3, 1, 0, 0
    total_sales = 0
    log = []
    star_month = {1: 1}
    for month in range(1, 25):
        star = star_of(rep)
        unlocked = [k for k, v in ITEMS.items() if v[3] <= star]
        # --- 投資（月初）: 棚 → カウンター → 拡張 → 装飾
        w, h, shelf_max, counter_max, _ = SIZES[size]
        visits = round(STAR_VISITS[star] * (1 + AMBIENCE_PER * ambience))
        while shelves < shelf_max and money >= SHELF_COST + 150:
            money -= SHELF_COST; shelves += 1
        while counters * COUNTER_CAP < visits and counters < counter_max and money >= COUNTER_COST + 150:
            money -= COUNTER_COST; counters += 1
        if size < 2 and money >= SIZES[size + 1][4] + 400 and (
                shelves >= shelf_max or counters * COUNTER_CAP < visits):
            money -= SIZES[size + 1][4]; size += 1
        for name, c, a in reversed(DECOR):
            if ambience + a <= AMBIENCE_MAX and money >= c + 800:
                money -= c; ambience += a
                break
        visits = round(STAR_VISITS[star] * (1 + AMBIENCE_PER * ambience))
        alloc = choose_display(star, shelves, unlocked, rng, visits)
        shown = {k: n * flow(k) for k, n in alloc.items()}
        # --- 行商人
        rares_left = {}
        if rng.random() < MERCHANT_P[star]:
            # その★で出始める品と、1つ下の★の品から選ぶ
            pool = [k for k, v in RARES.items() if star - 1 <= v[3] <= star]
            r = rng.choice(pool)
            buy = min(2, money // RARES[r][0])
            if buy > 0:
                rares_left[r] = buy
        # --- 営業
        sales = cost = 0; rep_delta = 0; served = 0
        cap = counters * COUNTER_CAP
        for i in range(visits):
            if i >= cap:
                rep_delta += REP_QUEUE; continue
            sl, co, rp = serve(make_customer(star, unlocked, rng), shown, rares_left)
            sales += sl; cost += co; rep_delta += rp; served += 1
        # 売れ残ったレア品は在庫として持ち越さず、仕入れなかった扱い（簡略化）
        money += sales - cost
        total_sales += sales
        rep = max(0, rep + rep_delta)
        new_star = star_of(rep)
        if new_star not in star_month:
            star_month[new_star] = month + 1
        log.append((month, star, visits, served, sales, sales - cost, money, rep, shelves, counters, size, ambience))
    if verbose:
        print("月 ★ 来店 会計 売上 粗利 所持金 評判 棚 カ 広 雰")
        for row in log:
            print(*row)
    return total_sales, star_of(rep), star_month, money, log


if __name__ == "__main__":
    run(1, verbose=True)
    res = [run(s) for s in range(200)]
    print("\n200回の平均")
    print("総売上 中央値", statistics.median(r[0] for r in res))
    print("最終所持金 中央値", statistics.median(r[3] for r in res))
    for s in range(2, 6):
        ms = [r[2].get(s) for r in res]
        got = [m for m in ms if m]
        print(f"★{s} 到達率 {len(got)/len(res):.0%}  到達月 中央値 {statistics.median(got) if got else '-'}")
    for sz in (1, 2):
        ms = [next((row[0] for row in r[4] if row[10] >= sz), None) for r in res]
        got = [m for m in ms if m]
        print(f"拡張{sz} 到達率 {len(got)/len(res):.0%}  月 中央値 {statistics.median(got) if got else '-'}")
