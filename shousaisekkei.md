# 詳細設計書（Step 1）

| 項目 | 内容 |
|---|---|
| 版 | 0.2 |
| 作成日 | 2026-10-24 |
| 更新日 | 2026-10-24 |
| 対象範囲 | Step 1（unityroom 公開版）の Core・Data・Game の各クラス |
| 入力 | [kihonsekkei.md](kihonsekkei.md)（基本設計）／[youken.md](youken.md)（要件・数値の正本）／[kousei.md](kousei.md)（Unity プロジェクト構成） |

凡例: **（確定）** = 相談して決めたもの / **（案）** = 本書で設計したもの。実装しながら変えてよい / **（要確認）** = 実装前に確かめること

本書は基本設計を**そのままコードに書き起こせる粒度**にする。クラス・フィールド・メソッド・処理の順番・境界条件を決める。数値そのものは書かない（youken.md 5 章と kihonsekkei.md が正本）。本書で例として出す数値は、読みやすさのための写しである。

基本設計を読んで、そのままでは実装できない・食い違う点が見つかったので、本書で補った。一覧は [12 章](#12-基本設計からの補足と変更)。補った内容は基本設計 0.4 に反映済み。

---

## 目次

1. [方針と規約](#1-方針と規約)
2. [ファイル構成](#2-ファイル構成)
3. [設定データ（GameConfig）](#3-設定データgameconfig)
4. [GameSession と 1 Tick の処理](#4-gamesession-と-1-tick-の処理)
5. [お金・評判・倉庫・問屋](#5-お金評判倉庫問屋)
6. [店内レイアウトと配置](#6-店内レイアウトと配置)
7. [棚と会計](#7-棚と会計)
8. [客](#8-客)
9. [行商人・月末・年末・終了](#9-行商人月末年末終了)
10. [イベントとコマンド](#10-イベントとコマンド)
11. [Data と Game（Unity 側）](#11-data-と-gameunity-側)
12. [基本設計からの補足と変更](#12-基本設計からの補足と変更)
13. [テストケース](#13-テストケース)
14. [実装の順番](#14-実装の順番)

---

## 1. 方針と規約

### 1.1 コーディング規約（案）

既存の `GameClock` / `TimeConfig` / `SeededRandom` に合わせる。

| 項目 | 規約 |
|---|---|
| 名前空間 | `Shop.Core` / `Shop.Data` / `Shop.Game`（asmdef と同じ）。Core の中でフォルダを分けても名前空間は `Shop.Core` 1 つ |
| クラス | 継承しないものは `sealed` |
| フィールド | private は `_camelCase`。公開は get だけのプロパティ（`{ get; private set; }`） |
| 引数の検証 | 公開コンストラクタでは `ArgumentNullException` / `ArgumentOutOfRangeException`。**プレイヤーの操作の失敗は例外にせず `CommandResult` で返す**（基本設計 P5） |
| コメント | 公開メンバーに 1 行の `/// <summary>`。対応する設計の章番号を書く（例: `kihonsekkei.md 6.2`） |
| C# の機能 | C# 9 まで。`record` と `init` は使わない（Unity では `IsExternalInit` の自作が要るため） |
| 浮動小数 | Core のゲームの状態には `float` / `double` を持たない（確率の判定だけに使う）。お金・評判・時間はすべて `int` |
| コレクション | 処理順が結果を変えるものは `List<T>` を使い、`Dictionary` を列挙して処理しない（列挙順が決まっていないため。P3 再現性） |

### 1.2 ID と参照（案）

| 対象 | 持ち方 |
|---|---|
| 商品・職業・配置物の種類 | 定義オブジェクト（`ItemDef` / `JobDef` / `FixtureDef`）をそのまま参照する。保存はないので、文字列 ID は Data との対応とログのために持つだけ |
| 置いた配置物 | `int` の通し番号 `FixtureId`（1 始まり。撤去しても番号は使い回さない） |
| 客 | `int` の通し番号 `CustomerId`（1 始まり） |
| マス | `readonly struct Tile(int X, int Y)`。`==`、`GetHashCode` を持つ |

文字列 ID の一覧（案。名称は T-12 で変わっても ID は変えない）:

| 種類 | ID |
|---|---|
| 商品 | `potion` `antidote` `wooden_staff` `copper_sword` `iron_sword` `cloth_armor` `leather_shield` `iron_armor` |
| 行商人限定品 | `fairy_salve` `elixir` `silver_robe` `stardust_staff` `dragon_shield` |
| 職業 | `warrior` `mage` `priest` |
| 配置物 | `shelf_weapon` `shelf_armor` `shelf_tool` `counter` `bargain_table` `plant` `rug` `armor_stand` |

### 1.3 再現性のための決まり（確定: 基本設計 P2・P3）

- 乱数は `GameSession` が持つ 1 つの `IRandom` だけを使う。乱数を引く順番は本書の処理順で決まる
- 客・棚・カウンターは**番号の小さい順**に処理する
- 同じ距離・同じ条件で迷ったときは、**番号が小さいほう → マスの (y, x) が小さいほう**を選ぶ
- View（Unity 側）の見た目の乱数（吹き出しのセリフ選びなど）は `UnityEngine.Random` を使い、Core の乱数は引かない

---

## 2. ファイル構成

`Assets/_Project/Scripts/` の中。★は実装済み。既存の 3 ファイルは `Core/` 直下のまま動かさない。

```
Scripts/
├─ Core/                         Shop.Core（UnityEngine を参照しない）
│   ├─ GameClock.cs              ★ ClockEvents, GameClock
│   ├─ TimeConfig.cs             ★
│   ├─ SeededRandom.cs           ★ IRandom, SeededRandom
│   ├─ Config/
│   │   ├─ GameConfig.cs         GameConfig, BalanceDef
│   │   ├─ ItemDef.cs            ItemDef, ItemCategory
│   │   ├─ JobDef.cs
│   │   ├─ StarDef.cs
│   │   ├─ FixtureDef.cs         FixtureDef, FixtureKind
│   │   ├─ ShopSizeDef.cs
│   │   └─ ConfigValidator.cs    設定の食い違いを起動時に見つける
│   ├─ Common/
│   │   ├─ Tile.cs
│   │   ├─ CommandResult.cs      CommandResult, FailReason
│   │   └─ WeightedPicker.cs     重みつき抽選
│   ├─ Session/
│   │   ├─ GameSession.cs        窓口。Tick・コマンド・DrainEvents
│   │   ├─ ShopState.cs
│   │   ├─ MonthlyReport.cs      MonthlyReport, YearRecord, GameResult
│   │   └─ IScoreSubmitter.cs
│   ├─ Stock/
│   │   ├─ Warehouse.cs
│   │   └─ Wholesaler.cs
│   ├─ Layout/
│   │   ├─ ShopLayout.cs         PlacedFixture を含む
│   │   ├─ PlacementValidator.cs
│   │   └─ GridPathfinder.cs
│   ├─ Store/
│   │   ├─ Shelf.cs
│   │   └─ Counter.cs
│   ├─ Customers/
│   │   ├─ Customer.cs           Customer, CustomerState, Want, CustomerResult
│   │   ├─ CustomerFactory.cs    客の中身を決める
│   │   ├─ CustomerSpawner.cs    月の来店予定
│   │   └─ CustomerBrain.cs      客の状態遷移（1 Tick ぶん）
│   ├─ Merchant/
│   │   └─ Merchant.cs
│   ├─ Reputation/
│   │   └─ ReputationSystem.cs
│   └─ Events/
│       └─ GameEvents.cs         GameEvent と各イベント
├─ Data/                         Shop.Data（ScriptableObject）
│   ├─ ItemData.cs / RareItemData.cs / JobData.cs / StarTable.cs
│   ├─ FixtureData.cs / ShopSizeTable.cs / BalanceConfig.cs
│   ├─ GameDatabase.cs           上をまとめて持つ 1 つのアセット
│   └─ ConfigBuilder.cs          GameDatabase → GameConfig
├─ View/                         Shop.Game
│   ├─ GameRunner.cs             Tick を回す。倍速・一時停止
│   ├─ EventRouter.cs            イベントを View / UI へ配る
│   ├─ GridView.cs / FixtureView.cs / ShelfView.cs / CounterView.cs
│   ├─ CustomerView.cs / CustomerViewPool.cs / MerchantView.cs
│   ├─ FloatingText.cs
│   └─ TilePosition.cs           マス ⇔ ワールド座標
└─ UI/                           Shop.Game
    ├─ Hud.cs / BottomMenu.cs
    ├─ WholesalerPanel.cs / ShelfAssignPanel.cs / ExpandPanel.cs / MerchantPanel.cs
    ├─ PlacementMode.cs
    ├─ ModalQueue.cs             月末収支 → 星アップ → 年末審査 → 結果発表 の順番待ち
    ├─ MonthlyReportDialog.cs / StarUpEffect.cs / YearReviewDialog.cs / ResultDialog.cs
    ├─ CommentTable.cs           吹き出しのセリフ（T-13）
    ├─ FailReasonText.cs         FailReason → 表示文
    ├─ UnityroomScoreSubmitter.cs
    └─ TitleScreen.cs
```

テスト（`Assets/Tests/EditMode/`）:

```
EditMode/
├─ GameClockTests.cs / SeededRandomTests.cs   ★
├─ Support/TestConfig.cs        youken 5 章の値を C# で組んだ GameConfig
├─ Support/SessionBuilder.cs    状態を作ってから始めるための手助け
├─ WarehouseTests.cs / WholesalerTests.cs / ShelfTests.cs / CounterTests.cs
├─ PlacementTests.cs / PathfinderTests.cs
├─ CustomerFactoryTests.cs / CustomerSpawnerTests.cs / CustomerBehaviorTests.cs
├─ ReputationTests.cs / MerchantTests.cs / MonthEndTests.cs
├─ DeterminismTests.cs
├─ ConfigAssetTests.cs          ScriptableObject のアセットが youken 5 章と一致するか
└─ Balance/AutoPlayer.cs, BalanceComparisonTests.cs   [Category("Slow")]
```

`ConfigAssetTests` のため、`Shop.Tests.EditMode.asmdef` の参照に `Shop.Data` を足す（[12 章 K-09](#12-基本設計からの補足と変更)）。

---

## 3. 設定データ（GameConfig）

起動時に Data から作る、読み取り専用の値（基本設計 8.1）。Core はここからだけ数値を読む（P4）。

### 3.1 定義クラス

```csharp
public enum ItemCategory { Tool, Weapon, Armor, Rare }

public sealed class ItemDef
{
    public string Id { get; }
    public string Name { get; }
    public ItemCategory Category { get; }
    public int BuyPrice { get; }
    public int SellPrice { get; }
    public int UnlockStar { get; }        // 問屋の品: 解禁の星 / 限定品: 出始める星
    public bool IsRare => Category == ItemCategory.Rare;
    public JobDef RareJob { get; }        // 限定品で null = 全職業。問屋の品では null
    public int RareRepBonus { get; }      // 限定品だけ。問屋の品では 0
}

public sealed class JobDef
{
    public string Id { get; }
    public string Name { get; }
    public IReadOnlyList<(ItemDef Item, int Weight)> Wants { get; }   // 重み 0 の品は入れない
}

public sealed class StarDef
{
    public int Star { get; }              // 1〜5
    public int RequiredRep { get; }
    public int BaseVisits { get; }
    public int BudgetMin { get; }         // 両端を含む
    public int BudgetMax { get; }
    public int MerchantChancePercent { get; }   // 20 = 20%
}

public enum FixtureKind { Shelf, Counter, Decor }

public sealed class FixtureDef
{
    public string Id { get; }
    public string Name { get; }
    public FixtureKind Kind { get; }
    public ItemCategory ShelfCategory { get; }   // Kind == Shelf のとき。特売台は Rare
    public int Cost { get; }
    public int Width { get; }
    public int Height { get; }
    public bool Walkable { get; }                 // 絨毯だけ true
    public int DisplayMax { get; }                // 棚: 並ぶ数
    public int RestockTicks { get; }              // 棚: 補充の間隔
    public int Ambience { get; }                  // 装飾: 雰囲気値
    public int MaxCount { get; }                  // 0 = 広さの段階の上限に従う / 1 以上 = その数まで（特売台 1）
    public bool CountsTowardShelfLimit { get; }   // 武器・防具・道具棚 true、特売台 false
    public bool UsesFreeShelf { get; }            // 無料枠を使えるか（武器・防具・道具棚 true）
    public bool IsBargainTable => Kind == FixtureKind.Shelf && ShelfCategory == ItemCategory.Rare;
}

public sealed class ShopSizeDef
{
    public int Stage { get; }             // 1〜3
    public int Width { get; }
    public int Height { get; }
    public int ShelfMax { get; }
    public int CounterMax { get; }
    public int Cost { get; }              // 段階 1 は 0
}
```

### 3.2 BalanceDef と GameConfig

```csharp
public sealed class BalanceDef
{
    public int StartMoney { get; }
    public int RepFull { get; }  public int RepPartial { get; }
    public int RepNone { get; }  public int RepQueueLeft { get; }
    public int AmbiencePermille { get; }        // 雰囲気 1 あたり 15 = 1.5%（float を避けて千分率）
    public int AmbienceMax { get; }
    public int TwoWantsChancePercent { get; }   // 40
    public int CheckoutTicks { get; }           // 24
    public int QueuePatienceTicks { get; }      // 120
    public int WalkTicksPerTile { get; }        // 3
    public int PickTicks { get; }               // 5   棚で手に取る間（本書で追加）
    public int EmoteTicks { get; }              // 10  しょんぼり・「高い…」の間
    public int MaxCustomerStayTicks { get; }    // 400
    public int SpawnWindowEndTick { get; }      // 570 これ未満の Tick に来店させる
    public int MerchantArriveMinTick { get; }   // 60
    public int MerchantArriveMaxTick { get; }   // 400（含まない）
    public int MerchantStayTicks { get; }       // 150
    public int MerchantMaxBuy { get; }          // 2
    public int RefundPercent { get; }           // 100
    public int FreeShelfCount { get; }          // 3
    public Tile Entrance { get; }               // (2, 0)
    public IReadOnlyList<(FixtureDef Def, Tile Origin)> InitialLayout { get; }  // カウンター (4, 1)
}

public sealed class GameConfig
{
    public TimeConfig Time { get; }
    public BalanceDef Balance { get; }
    public IReadOnlyList<ItemDef> Items { get; }       // 問屋 8 品 → 限定品 5 品の順
    public IReadOnlyList<JobDef> Jobs { get; }
    public IReadOnlyList<StarDef> Stars { get; }       // [0] が ★1
    public IReadOnlyList<FixtureDef> Fixtures { get; }
    public IReadOnlyList<ShopSizeDef> ShopSizes { get; } // [0] が段階 1

    public StarDef StarOf(int star) => Stars[star - 1];
    public ShopSizeDef SizeOf(int stage) => ShopSizes[stage - 1];
    public int MaxStar => Stars.Count;
    public int MaxStage => ShopSizes.Count;
}
```

- 雰囲気値の来店数への効果は整数で計算する（式は 8.1）
- コンストラクタの最後で `ConfigValidator.Validate(this)` を呼び、問題があれば `InvalidOperationException`（メッセージに全部の問題を並べる）

### 3.3 ConfigValidator が確かめること（案）

| # | 内容 |
|---|---|
| V1 | ID が種類ごとに重複していない |
| V2 | 星が 1 から連番。必要評判が単調増加で ★1 は 0。予算は Min ≤ Max |
| V3 | 広さが段階ごとに縦横とも広がる（右と上に広がる前提 基本設計 4.1）。入口が全段階で内側 |
| V4 | 各職業の重みの合計が、★1 の解禁品だけで 1 以上（★1 の客が欲しいものを持てる） |
| V5 | 棚の種類（Tool/Weapon/Armor/Rare）がそれぞれ 1 つずつある。限定品は Rare、問屋の品は Rare 以外 |
| V6 | 限定品の出始める星が 1〜最大星。各星に 1 つ以上ある（行商人の品揃えが空にならない） |
| V7 | 初期配置が段階 1 の中で置ける（6.3 の判定を通る） |
| V8 | `DisplayMax`・`RestockTicks` は棚なら 1 以上 |
| V9 | `MerchantArriveMaxTick + MerchantStayTicks <= TicksPerMonth`（行商人が月をまたがない） |

---

## 4. GameSession と 1 Tick の処理

### 4.1 公開 API

```csharp
public sealed class GameSession
{
    public GameSession(GameConfig config, ulong seed);

    // 進める
    public void Tick();                                 // GameEnded 後は何もしない
    public IReadOnlyList<GameEvent> DrainEvents();      // ためたイベントを返して空にする

    // 見る（UI 用。返すのは読み取り専用のビュー）
    public GameConfig Config { get; }
    public GameClock Clock { get; }
    public ShopState State { get; }
    public Warehouse Warehouse { get; }
    public ShopLayout Layout { get; }
    public IReadOnlyList<Shelf> Shelves { get; }        // FixtureId の小さい順
    public IReadOnlyList<Counter> Counters { get; }
    public IReadOnlyList<Customer> Customers { get; }   // 店内にいる客。CustomerId の小さい順
    public Merchant Merchant { get; }
    public MonthlyReport CurrentReport { get; }         // 今月の途中集計
    public GameResult Result { get; }                   // GameEnded 前は null
    public bool IsEnded { get; }
    public IReadOnlyList<ItemDef> UnlockedItems { get; } // 今の星で問屋が売る品

    // コマンド（10.2）
    public CommandResult BuyFromWholesaler(ItemDef item, int count);
    public CommandResult BuyFromMerchant(int count);
    public CommandResult PlaceFixture(FixtureDef def, Tile origin);
    public CommandResult MoveFixture(int fixtureId, Tile origin);
    public CommandResult RemoveFixture(int fixtureId);
    public CommandResult AssignShelf(int fixtureId, ItemDef itemOrNull);
    public CommandResult ExpandShop();

    // 確かめるだけ（配置モードの赤・緑表示用。状態は変えない）
    public CommandResult CanPlace(FixtureDef def, Tile origin);
    public CommandResult CanMove(int fixtureId, Tile origin);
    public CommandResult CanRemove(int fixtureId);
    public int PlacementCost(FixtureDef def);          // 無料枠が残っていれば 0
}
```

- `DrainEvents` は内部の 2 つのリストを入れ替えて返す（毎フレームの割り当てを避ける）。返したリストは**次の `DrainEvents` まで有効**
- 状態を返すプロパティの型（`Shelf` など）は、外から変更できるメソッドを `internal` にする。UI からは読めるが変えられない

### 4.2 コンストラクタでの初期化

1. `IRandom = new SeededRandom(seed)`、`GameClock = new GameClock(config.Time)`
2. `ShopState`: 所持金 = StartMoney、評判 0、星 1、総売上 0、無料枠 = FreeShelfCount
3. `ShopLayout` を段階 1 で作り、`InitialLayout` を**無料で**置く（PaidCost = 0。無料枠は減らさない）。置いたカウンターの `Counter` を作る
4. 1 か月目の準備（9.2 の「翌月の準備」と同じ処理）: 来店予定を作る、行商人を抽選する
5. 最初のイベントとして `SessionStarted` を積む（UI はこれを見て配置モードで始める。基本設計 4.5）

### 4.3 1 Tick の処理順（基本設計 9.3 を変更。[K-01](#12-基本設計からの補足と変更)）

```
GameSession.Tick():
    if IsEnded: return
    t = Clock.TickInMonth              ← 今から処理する Tick（0〜599）
    Merchant.Tick(t)                   来訪・退去
    Spawner.Tick(t)                    予定の時刻になった客を出す
    for 客 in Customers（Id 順）:
        Brain.Tick(客)                 移動・棚・行列・帰る
    for カウンター in Counters（Id 順）:
        カウンター.Tick()               会計を進める
    for 棚 in Shelves（Id 順）:
        棚.Tick()                      補充
    RemoveGoneCustomers()              出口に着いた客を消す（CustomerDespawned）
    clockEvents = Clock.Advance()
    if DayChanged: Emit(DayChanged)
    if MonthEnded: EndMonth(clockEvents)    9.2
```

基本設計 0.3 では `Clock.Advance()` を先に呼んでいたが、そうすると月の最後の Tick（599）を処理するときにはもう翌月の 0 になっていて、来店予定や行商人の時刻とずれる。**先に各システムを動かし、最後に時計を進める**。

---

## 5. お金・評判・倉庫・問屋

### 5.1 ShopState

```csharp
public sealed class ShopState
{
    public int Money { get; }
    public int Reputation { get; }            // 0 以上
    public int Star { get; }                  // 到達した最高の星（下がらない）
    public int TotalSales { get; }            // スコア
    public int FreeShelvesLeft { get; }
    public IReadOnlyList<int> StarReachedMonth { get; }   // [0]=★1 は 1。未到達は 0

    internal void AddMoney(int delta);        // 結果が負になるなら InvalidOperationException（呼ぶ前に確かめる前提）
    internal void AddSales(int amount);       // Money と TotalSales の両方に足す
    internal int AddReputation(int delta);    // 0 で止める。実際に変わった量を返す
    internal void RaiseStar(int star, int month);
    internal void UseFreeShelf();
}
```

- お金を減らす処理は、すべて**先に `Money >= 額` を確かめてから**呼ぶ。ここで例外が出るのはバグ
- お金が動くたびに `MoneyChanged(delta, reason)` を積む。reason は `Sale / Purchase / Equipment / Refund / Expansion`

### 5.2 ReputationSystem

```csharp
internal sealed class ReputationSystem
{
    public int RepFor(CustomerResult result);                      // 結果ごとの増減（Rare ボーナスは含まない）
    public void Apply(Customer c);                                 // 結果＋掘り出し物ボーナスを足し、イベントを積む
    public int StarFor(int reputation);                            // 必要評判を満たす最大の星
    public int RepToNextStar(int reputation, int star);            // 次の星までの残り。最大星なら 0
    public bool TryRaiseStar(int month, out int newStar);          // 月末に呼ぶ（9.2）
}
```

- `Apply` は**評判を 1 回にまとめて足す**（例: 不満 −2 ＋ 掘り出し物 +3 = +1）。0 で止めるのも合計に対して行う（シミュレーションの `max(0, rep + delta)` は月単位だが、実ゲームでは客ごとに止まる。差はごく小さい）
- 積むイベント: `ReputationChanged(delta, reason: CustomerResult, rareBonus)`
- `TryRaiseStar`: `StarFor(Reputation) > Star` なら星を上げる。**一度に 2 段階上がってもよい**（`StarRaised` は 1 回、上がった先の星と、間の星で解禁された商品をすべて入れる）

### 5.3 Warehouse

```csharp
public sealed class Warehouse
{
    public int Count(ItemDef item);
    public IReadOnlyList<(ItemDef Item, int Count)> Entries { get; }   // Config.Items の順。0 個も含む
    public IEnumerable<ItemDef> RareItemsInStock { get; }              // 特売台の設定パネル用
    internal void Add(ItemDef item, int count);
    internal bool TryTake(ItemDef item);          // 1 個出す
}
```

- 中身は `int[]`（`Config.Items` の添字）。容量は無制限（基本設計 6.1）
- 増減のたびに `WarehouseChanged(item, count)` を積む

### 5.4 Wholesaler（問屋）

```csharp
internal sealed class Wholesaler
{
    public IReadOnlyList<ItemDef> Unlocked(int star);              // 問屋の品で UnlockStar <= star
    public CommandResult Buy(ItemDef item, int count);
}
```

`Buy` の判定順:

| 順 | 条件 | 失敗理由 |
|---|---|---|
| 1 | `count >= 1` | `InvalidCount` |
| 2 | 問屋の品（限定品ではない） | `NotSoldHere` |
| 3 | `item.UnlockStar <= Star` | `Locked` |
| 4 | `Money >= BuyPrice × count`（`long` で掛けて桁あふれを防ぐ） | `NotEnoughMoney` |

成功したら: お金を減らす（Purchase）→ 倉庫に足す → 今月の仕入れに足す → 並べる商品がこの品で、空いている棚があれば補充タイマーはそのまま（すぐには並べない。補充の速さで並ぶ）。

---

## 6. 店内レイアウトと配置

### 6.1 ShopLayout

```csharp
public sealed class PlacedFixture
{
    public int Id { get; }
    public FixtureDef Def { get; }
    public Tile Origin { get; private set; }  // 左下のマス
    public int PaidCost { get; }              // 払い戻しの基準。無料枠・初期配置は 0
    public IEnumerable<Tile> Tiles { get; }   // 占めるマス
    public Tile? PayTile { get; }             // カウンターだけ: Origin の 1 つ下 (x, y-1)
}

public sealed class ShopLayout
{
    public int Stage { get; }
    public int Width { get; }  public int Height { get; }
    public Tile Entrance { get; }
    public IReadOnlyList<PlacedFixture> Fixtures { get; }      // Id 順
    public int Ambience { get; }                               // 装飾の合計。AmbienceMax で頭打ち
    public int ShelfCount { get; }                             // CountsTowardShelfLimit の数
    public int CounterCount { get; }
    public int CountOf(FixtureDef def);

    public bool IsInside(Tile t);
    public bool IsWalkable(Tile t);          // 内側で、通れない配置物がない
    public PlacedFixture FixtureAt(Tile t);  // 通れない物を優先。なければ絨毯。なければ null

    internal PlacedFixture Add(FixtureDef def, Tile origin, int paidCost);
    internal void Move(PlacedFixture f, Tile origin);
    internal void Remove(PlacedFixture f);
    internal void Expand();                  // Stage++、Width/Height を次の段階に
    public int Version { get; }              // 変わるたびに +1。経路のキャッシュの無効化に使う
}
```

- 占有は `PlacedFixture[,] _blocking`（通れない物）と `PlacedFixture[,] _floor`（絨毯）の 2 枚で持つ。拡張したら配列を作り直して写す
- 雰囲気値は配置が変わったときに計算し直す。来店数に効くのは翌月から（基本設計 5.1）

### 6.2 「客が使う位置」

| 配置物 | 使う位置 |
|---|---|
| 棚・特売台（1×1） | 上下左右に隣接するマスのうち `IsWalkable` なもの（複数ありうる） |
| カウンター（2×1） | `PayTile` = 左のマスの 1 つ下。ここが会計位置 |

### 6.3 PlacementValidator（基本設計 4.3 を具体化）

```csharp
internal sealed class PlacementValidator
{
    // 状態は変えない。変えたあとのレイアウトを仮に作って確かめる
    public CommandResult CanPlace(FixtureDef def, Tile origin);
    public CommandResult CanMove(PlacedFixture f, Tile origin);
    public CommandResult CanRemove(PlacedFixture f);
}
```

**置く**（`CanPlace`）の判定順。最初に当たった理由を返す:

| 順 | 条件 | 失敗理由 | 基本設計 |
|---|---|---|---|
| 1 | 占めるマスがすべて内側 | `OutOfBounds` | L1 |
| 2 | 占めるマスに入口がない | `BlocksEntrance` | L1 |
| 3 | 占めるマスに他の物がない（絨毯の上にも置けない。絨毯は何もないマスにだけ） | `Occupied` | L1 |
| 4 | カウンターなら、会計マスが内側で、通れない物も入口もない（[K-05](#12-基本設計からの補足と変更)） | `NoPayTile` | 追加 |
| 5 | 数の上限: `MaxCount >= 1` ならその数まで。棚は `ShelfCount < ShelfMax`、カウンターは `CounterCount < CounterMax` | `LimitReached` | L2 |
| 6 | `Money >= PlacementCost(def)` | `NotEnoughMoney` | L3 |
| 7 | 置いたあとで通路がつながっている（下の `CheckReachability`） | `BlocksPath` | L4 |

**動かす**（`CanMove`）: 自分を取り除いた仮のレイアウトで 1〜4 と 7 を確かめる。5・6 は見ない（移動は無料）。同じ位置への移動は成功（何もしない）。

**撤去する**（`CanRemove`）:

| 順 | 条件 | 失敗理由 |
|---|---|---|
| 1 | カウンターなら、撤去後も 1 台以上残る | `LastCounter`（L5） |
| 2 | 撤去後に通路がつながっている（撤去でふさがることはないが、念のため同じ関数を通す） | `BlocksPath` |

**通路の判定**（`CheckReachability`）:

```
入口から 4 方向の幅優先探索で、たどり着けるマスの集合 R を作る
すべての棚・特売台について: 使う位置のどれか 1 つ以上が R に入っている
すべてのカウンターについて: 会計マスが R に入っている
→ どれか 1 つでも満たさなければ BlocksPath
```

`PlacementCost(def)`: 棚で `UsesFreeShelf` かつ `FreeShelvesLeft > 0` なら 0、それ以外は `def.Cost`。

### 6.4 配置コマンドの処理

**PlaceFixture**: `CanPlace` → 成功なら

1. 費用を払う（無料枠なら `UseFreeShelf`）。費用 > 0 なら `MoneyChanged(Equipment)`、今月の設備投資に足す
2. `Layout.Add(def, origin, paidCost)`
3. 棚なら `Shelf`、カウンターなら `Counter` を作る
4. `LayoutChanged` → `OnLayoutChanged()`（6.5）

**MoveFixture**: `CanMove` → `Layout.Move` → `LayoutChanged` → `OnLayoutChanged()`。棚の中身・行列はそのまま。

**RemoveFixture**: `CanRemove` → 成功なら

1. 棚なら、並んでいる品を倉庫に戻す。カウンターなら、行列の客を 6.5 のとおり並び直させる
2. `PaidCost × RefundPercent / 100` を戻す（`MoneyChanged(Refund)`、今月の設備投資から引く）。無料枠は戻らない
3. `Layout.Remove`、`Shelf` / `Counter` を消す
4. `LayoutChanged` → `OnLayoutChanged()`

**ExpandShop**: 判定 `Stage < MaxStage`（`MaxStage`）→ `Money >= 次の段階の Cost`（`NotEnoughMoney`）。成功なら払って（Expansion、設備投資に足す）`Layout.Expand()` → `ShopExpanded(stage)`、`LayoutChanged` → `OnLayoutChanged()`。払い戻しはない。

### 6.5 配置が変わったときの客の扱い（基本設計 4.4 を具体化）

時間は止まっている（配置モード・拡張パネル）が、コマンドを実行した**その場で**直す。「完了」まで待たない（結果は同じで、処理が簡単）。

```
OnLayoutChanged():
    Pathfinder のキャッシュを捨てる
    各カウンターの行列マス（7.2）を作り直す
    for 客 in Customers:
        if 客の立っているマスが通れない:
            入口からの幅優先で、いちばん近い通れるマスへ移す（CustomerMoved を ticks=0 で積む）
        if 客が向かっている棚が消えた / 商品が変わった:
            その欲しいものを「置いていない」扱いにする（8.4）
        if 客が並んでいるカウンターが消えた:
            Counter.Reassign で別のカウンターの最後に並ぶ（待った時間はそのまま）
            会計中だった客も同じ（会計は最初からやり直し）
        客の経路を引き直す
```

---

## 7. 棚と会計

### 7.1 Shelf（特売台も同じクラス）

```csharp
public sealed class Shelf
{
    public int FixtureId { get; }
    public FixtureDef Def { get; }
    public ItemDef Item { get; }           // null = 何も置かない
    public int Displayed { get; }
    public bool IsBargainTable => Def.IsBargainTable;

    internal CommandResult Assign(ItemDef itemOrNull);
    internal bool TryTake();               // 客が 1 個手に取る
    internal void PutBack();               // 行列で帰った客が戻す（満杯なら倉庫へ）
    internal void ReturnAllToWarehouse();
    internal void Tick();
}
```

**Assign** の判定順:

| 順 | 条件 | 失敗理由 |
|---|---|---|
| 1 | `itemOrNull == null` なら成功（並んでいた分を倉庫に戻す） | — |
| 2 | 特売台なら限定品、普通の棚なら `Category == Def.ShelfCategory` | `WrongCategory` |
| 3 | 問屋の品なら解禁済み | `Locked` |
| 4 | 限定品なら倉庫に 1 個以上ある | `NotInWarehouse` |
| 5 | 同じ品なら何もしない（成功） | — |

成功したら: 並んでいた分を倉庫に戻す → 品を変える → **倉庫から DisplayMax までまとめて並べる**（基本設計 6.2）→ 補充タイマー 0 → `ShelfChanged(shelf)`。

問屋の品は「倉庫に 0 個でも設定できる」（あとで仕入れると並ぶ）。限定品だけ 4 を見るのは、行商人が来ない限り入らないため。

**Tick**（基本設計 6.2 の擬似コードと同じ）:

```
if Item == null: return
if Displayed < DisplayMax and Warehouse.Count(Item) > 0:
    _timer++
    if _timer >= RestockTicks:
        Warehouse.TryTake(Item); Displayed++; _timer = 0
        Emit(ShelfChanged)
else:
    _timer = 0
```

**TryTake**: `Displayed > 0` なら 1 減らして `ShelfChanged`。0 になったら `ShelfEmptied` も積む（View がしょんぼり用の空表示にする）。

### 7.2 Counter

```csharp
public sealed class Counter
{
    public int FixtureId { get; }
    public Tile PayTile { get; }
    public IReadOnlyList<Customer> Queue { get; }  // 並んでいる順。会計中の客は含まない
    public Customer Serving { get; }
    public int Remaining { get; }                  // 会計の残り Tick
    public int Load => Queue.Count + (Serving != null ? 1 : 0);
    public IReadOnlyList<Tile> QueueTiles { get; } // [0] が行列の先頭のマス

    internal void Enqueue(Customer c);
    internal void Leave(Customer c);               // 行列から抜ける（待ちきれない、配置替え）
    internal void Tick();
}
```

**行列のマス**（`QueueTiles`）: 会計マスから入口までの最短経路をたどり、会計マスの次のマスから順に並べる。経路が短くて足りなければ、残りの客は最後のマスに重ねる（客どうしは重なってよい。基本設計 5.4）。会計マスと入口が同じ… にはならない（入口に会計マスは置けない 6.3 の 4）。

**Tick**:

```
if Serving != null:
    Remaining--
    if Remaining == 0:
        売上 = Serving のかごの売値の合計
        State.AddSales(売上)、今月の売上に足す
        Emit(CheckoutDone(counter, customer, 売上))
        Serving.OnCheckedOut()        → 結果を決めて Leaving（8.5）
        Serving = null
if Serving == null and Queue.Count > 0:
    先頭 = Queue[0]
    if 先頭が QueueTiles[0] か PayTile に着いている:
        Queue から外す、Serving = 先頭、Remaining = CheckoutTicks
        先頭.StartPaying(PayTile)     → 会計マスへ 1 歩（会計の時間の中で歩く）
        Emit(QueueChanged(counter))
```

- 行列の 2 番目以降は、先頭が抜けるたびに 1 つ前のマスへ歩く
- 会計マスまでの 1 歩を会計時間に含めるのは、**1 人 24 Tick（月 25 人）を守るため**（[K-02](#12-基本設計からの補足と変更)）。先頭が会計マスに着くのを待ってから始めると、1 人 27 Tick（月 22 人）になってしまう

**並ぶカウンターの選び方**（基本設計 6.3）: `Load` が最少 → 客の今のマスから会計マスまでの距離が最短 → `FixtureId` が小さい。

---

## 8. 客

### 8.1 CustomerSpawner（月の来店予定）

```csharp
internal sealed class CustomerSpawner
{
    public void PlanMonth(int star, int ambience);   // 月の初めに呼ぶ
    public void Tick(int tickInMonth);               // 予定の時刻になった客を出す
    public int PlannedCount { get; }
}
```

**PlanMonth**:

```
基本 = StarOf(star).BaseVisits
雰囲気 = min(ambience, AmbienceMax)
来店数 = (基本 × (1000 + 雰囲気 × AmbiencePermille) + 500) / 1000      ← 整数の四捨五入
予定 = []
for i in 0..来店数-1:
    予定.Add(Random.Range(0, SpawnWindowEndTick))
予定を小さい順に並べる（同じ時刻は何人いてもよい）
```

- 客の中身（職業・欲しいもの・予算）は**来店の時刻に**決める（8.2）。特売台に何が並んでいるかで掘り出し物の希望が変わるため
- 乱数を引く順番: 月末処理の中で「来店時刻 → 行商人の抽選」の順（9.2）

**Tick**: 予定の先頭が `tickInMonth` と同じあいだ、`CustomerFactory.Create` で客を作って店内に出す → `CustomerSpawned`。今月の来店数に足す。

### 8.2 CustomerFactory（客の中身）

```
Create(star):
    job = Jobs[Random.Range(0, Jobs.Count)]
    候補 = job.Wants のうち、問屋の品で UnlockStar <= star のもの        ← FR-CUST-06
    n = (Random.NextDouble() < TwoWantsChancePercent / 100.0) ? 2 : 1
    wants = []
    repeat n 回:
        品 = WeightedPicker.Pick(候補, Random)
        if 品 が wants にない: wants.Add(品)                        ← 同じ品は 1 つにまとめる
    予算 = Random.Range(BudgetMin, BudgetMax + 1)
    掘り出し物 = FindBargain(job, 予算)
    if 掘り出し物 != null: wants の先頭に (掘り出し物, IsRare=true) を入れる
```

- 乱数を引く順番は**シミュレーションの `make_customer` と同じ**（職業 → 1 品か 2 品か → 品 → 予算）。そのあと掘り出し物の判定（乱数は引かない）
- `WeightedPicker.Pick`: 重みの合計 S に対して `r = Random.Range(0, S)`、累積で最初に `r < 累積` となる品
- `FindBargain(job, 予算)`: 特売台（Id 順）を見て、`Displayed > 0`・`RareJob` が null か job と同じ・`SellPrice <= 予算` の最初の品。なければ null

```csharp
public sealed class Want
{
    public ItemDef Item { get; }
    public bool IsRare { get; }
    public WantStatus Status { get; internal set; }   // Pending / InBasket / Failed
    public FailKind Fail { get; internal set; }        // None / OutOfStock / TooExpensive / NotCarried / GaveUp
}
```

### 8.3 Customer の状態

基本設計 5.3 の状態を、1 Tick ずつ進められるように細かく分ける。

```csharp
public enum CustomerState
{
    Entering,      // 街の道から入口へ（WalkTicksPerTile の間）
    ToShelf,       // 次の欲しいものの棚へ歩く
    Picking,       // 棚の前で手に取る（PickTicks）
    Disappointed,  // しょんぼり / 「高い…」（EmoteTicks）
    Wandering,     // 置いていない品を探して、うろうろ
    ToQueue,       // カウンターの行列へ歩く
    Queuing,       // 行列で待つ
    Paying,        // 会計中（Counter が進める）
    Leaving,       // 出口（入口マス）へ歩く
    Gone,          // 出口に着いた。この Tick の終わりに消す
}

public sealed class Customer
{
    public int Id { get; }
    public JobDef Job { get; }
    public IReadOnlyList<Want> Wants { get; }
    public int Budget { get; }                 // 残りの予算
    public IReadOnlyList<(ItemDef Item, int ShelfId)> Basket { get; }
    public CustomerState State { get; }
    public Tile Tile { get; }                  // 今いるマス（歩いている途中は出発したマス）
    public CustomerResult? Result { get; }     // 結果が決まったら入る
    public int StayTicks { get; }              // 来店からの経過
    // 内部: _path(Queue<Tile>), _stepTimer, _stateTimer, _wantIndex, _targetShelfId,
    //       _counterId, _queueWaitTicks, _retargeted
}

public enum CustomerResult { Full, Partial, None, QueueLeft }
```

### 8.4 CustomerBrain（1 Tick の動き）

```
Tick(c):
    c.StayTicks++
    if c.State は ToQueue / Queuing:
        c._queueWaitTicks++
        if c._queueWaitTicks > QueuePatienceTicks: GiveUpQueue(c); return    ← 8.5
    if c.State は ToShelf / Picking / Disappointed / Wandering
       and c.StayTicks > MaxCustomerStayTicks:
        残りの欲しいものを Failed(GaveUp) にして FinishBrowsing(c)      ← K-03
    if c が歩いている途中（_path が残っている）:
        StepWalk(c); return                       ← 1 マス WalkTicksPerTile
    switch c.State:
        Entering    → NextWant(c)
        ToShelf     → ArriveAtShelf(c)            （着いた）
        Picking     → _stateTimer-- が 0 になったら NextWant(c)
        Disappointed→ _stateTimer-- が 0 になったら NextWant(c)
        Wandering   → Failed(NotCarried)、Emote(c, NotCarried)、State = Disappointed
        ToQueue     → c を行列の自分の位置で Queuing に（Counter が会計を始める）
        Queuing     → 行列の自分のマスと今のマスが違えば、そこへ歩く
        Paying      → 何もしない（Counter が進める）
        Leaving     → Gone、Emit(CustomerExited)
```

**StepWalk**: `_stepTimer == 0` のとき次のマスを取り出して `CustomerMoved(id, from, to, WalkTicksPerTile)` を積み、`_stepTimer = WalkTicksPerTile`。毎 Tick 1 減らし、0 になったら `Tile = to`。View は `CustomerMoved` を受けて、その Tick 数をかけて補間する。

**NextWant**（次の欲しいものを決める）:

```
w = Wants のうち最初の Pending
if w == null: FinishBrowsing(c); return
target = ChooseShelf(c, w.Item)
if target.棚 != null:
    _targetShelfId = target.棚、経路 = 今のマス → target.マス、State = ToShelf
elif w.IsRare:
    w.Failed(OutOfStock)、NextWant(c)            ← 掘り出し物は無ければ何もせず次へ（K-04）
else:
    同じ系統の棚があれば、いちばん近いその棚の前へ（State = ToShelf, 着いたら NotCarried）
    なければ 入口から 2〜4 マスの通れるマスを 1 つ選んで歩き（State = Wandering）
```

`ChooseShelf(c, item)`:

1. `item` を並べた棚のうち `Displayed > 0` のもの → 客から最も近い（使う位置までの歩数）
2. なければ `item` を並べた棚のうち最も近いもの（在庫なしでも行って、しょんぼりを見せる FR-CUST-07）
3. どちらもなければ null
4. 迷ったら 1.3 の決まり（Id → マス）。うろうろ先の選び方だけ乱数を使う（`Random.Range`）

**ArriveAtShelf**:

```
棚 = _targetShelfId の棚（なければ「置いていない」扱い）
w = 今の欲しいもの
if 棚.Item != w.Item:                → Failed(NotCarried)、Emote(NotCarried)、Disappointed
elif 棚.Displayed == 0:
    if not _retargeted and 在庫のある別の棚がある:
        _retargeted = true、そこへ歩く（ToShelf）       ← K-06
    else:
        Failed(OutOfStock)、Emote(OutOfStock)、Disappointed
elif w.Item.SellPrice > Budget:     → Failed(TooExpensive)、Emote(TooExpensive)、Disappointed
else:
    棚.TryTake()、Budget -= SellPrice、Basket.Add、w = InBasket
    Emit(ItemTaken(shelf, customer, item))、State = Picking（PickTicks）
_retargeted = false（次の欲しいもののため）
```

- 予算は**手に取った時点で引く**（基本設計 5.3）。掘り出し物は先頭にあるので、先に予算を使う（シミュレーションと同じ）

**FinishBrowsing**:

```
if Basket が空: Decide(c, None 相当 → 8.5)、State = Leaving、経路 = 入口へ
else:          カウンターを選んで Enqueue、State = ToQueue、経路 = 行列の自分のマスへ
```

### 8.5 結果の決め方と退店

結果は**店を出ることが決まった瞬間**に 1 回だけ決める（基本設計 5.5）。

| きっかけ | 結果 |
|---|---|
| 会計が終わった（`OnCheckedOut`） | 問屋の品の欲しいものがすべて InBasket → `Full`、1 つ以上 → `Partial`、0 → `None` |
| かごが空で回り終えた | `None` |
| 行列で待ちきれなかった（`GiveUpQueue`） | `QueueLeft` |

- 掘り出し物（`IsRare`）は数えない。会計できた限定品があれば `RareRepBonus` を足す（`QueueLeft` のときは会計していないので足さない）
- 問屋の品の欲しいものが 0 個で掘り出し物だけ買えた → `None`（−2）＋ボーナス（基本設計 5.5 と同じ）
- 決まったら: `ReputationSystem.Apply(c)` → 今月の集計（満足・一部・不満・行列で帰った・限定品の販売数）に足す → `CustomerLeft(id, result, boughtItems)` を積む → State = Leaving、経路 = 入口へ
- 吹き出しのセリフは View が `result` と買った品から選ぶ（[K-08](#12-基本設計からの補足と変更)）

**GiveUpQueue**: カウンターの行列から抜ける → かごの品を 1 個ずつ、取った棚に `PutBack`（棚が消えた・品が変わった・満杯なら倉庫へ）→ 結果 `QueueLeft`。

### 8.6 GridPathfinder

```csharp
internal sealed class GridPathfinder
{
    public GridPathfinder(ShopLayout layout);
    public IReadOnlyList<Tile> FindPath(Tile from, Tile to);    // from を含まず to を含む。届かなければ null
    public int Distance(Tile from, Tile to);                    // 届かなければ int.MaxValue
    public int[,] DistanceField(Tile from);                     // 1 回の BFS で全マスの距離
}
```

- 4 方向の幅優先探索。隣を調べる順番は **上 → 右 → 下 → 左**で固定（同じ長さの経路が複数あるときに結果を一意にする）
- `from` が通れないマス（配置替えの直後など）でも、そこから出ることはできる
- `DistanceField` は `(from, Layout.Version)` をキーに数個だけキャッシュする（棚選びで同じ地点から何度も引くため）。マスは最大 70 なので、キャッシュなしでも足りる。重ければ入れる

---

## 9. 行商人・月末・年末・終了

### 9.1 Merchant

```csharp
public enum MerchantState { None, Scheduled, Present, Left }

public sealed class Merchant
{
    public MerchantState State { get; }
    public ItemDef Item { get; }
    public int ArrivalTick { get; }      // 月の中の Tick
    public int LeaveTick { get; }
    public int Remaining { get; }        // あと何個買えるか（最初は MerchantMaxBuy）
    public int TicksLeft { get; }        // HUD の「残り n 秒」用

    internal void PlanMonth(int star);
    internal void Tick(int tickInMonth);
    internal CommandResult Buy(int count);
}
```

**PlanMonth(star)**（月の初めに 1 回）:

```
State = None
if Random.NextDouble() < StarOf(star).MerchantChancePercent / 100.0:
    候補 = 限定品で star-1 <= UnlockStar <= star     ← ★1 は ★1 の品だけになる
    Item = 候補[Random.Range(0, 候補.Count)]
    ArrivalTick = Random.Range(MerchantArriveMinTick, MerchantArriveMaxTick)
    LeaveTick = ArrivalTick + MerchantStayTicks
    Remaining = MerchantMaxBuy、State = Scheduled
```

- 乱数を引く順番（来る / 品 / 時刻）はシミュレーションの「来る → 品」に時刻を足したもの

**Tick**: `Scheduled` で `t == ArrivalTick` → `Present`、`MerchantArrived(item, leaveTick)`。`Present` で `t == LeaveTick` → `Left`、`MerchantLeft`。

**Buy(count)** の判定順: `Present`（`MerchantGone`）→ `1 <= count <= Remaining`（`InvalidCount`）→ お金（`NotEnoughMoney`）。成功したら払って（Purchase、今月の仕入れ）倉庫へ入れ、`Remaining -= count`。特売台がなくても買える（基本設計 6.2.1）。`Remaining` が 0 になっても行商人は `LeaveTick` までいる（見た目のため）。

### 9.2 月末処理（基本設計 7.1 を具体化）

```
EndMonth(clockEvents):
    month = 終わった月（1〜24）
    report = CurrentReport を締める（RepAfter, Star, RepToNext を書き込む）
    raised = Reputation.TryRaiseStar(month + 1, out newStar)      ← 翌月から反映（FR-REP-05）
    Emit(MonthEnded(report))
    if raised: Emit(StarRaised(newStar, 解禁された商品))
    if YearEnded: record = 年の記録を締める; Emit(YearEnded(record))
    if GameEnded:
        Result = GameResult を作る; IsEnded = true
        Emit(GameEnded(Result))
        return                                                    ← 翌月の準備はしない
    翌月の準備:
        CurrentReport = 新しい MonthlyReport(month + 1)
        Spawner.PlanMonth(State.Star, Layout.Ambience)            ← 乱数: 来店時刻
        Merchant.PlanMonth(State.Star)                            ← 乱数: 行商人
```

- 店内に残っている客はそのまま翌月へ（基本設計 7.1）。その客の売上・結果は**会計した月**に数える（[K-10](#12-基本設計からの補足と変更)）
- `StarReachedMonth` には「その星の客が来始める月」（= month + 1）を入れる。シミュレーションの `star_month[new_star] = month + 1` と同じ

### 9.3 集計

```csharp
public sealed class MonthlyReport
{
    public int Month { get; }                 // 通算 1〜24
    public int Sales { get; }                 // 会計の売上
    public int Purchases { get; }             // 問屋＋行商人
    public int Equipment { get; }             // 配置＋拡張 − 払い戻し（マイナスもありうる）
    public int Net => Sales - Purchases - Equipment;
    public int Visitors { get; }              // 今月来店した人数
    public int Full { get; }  public int Partial { get; }
    public int None { get; }  public int QueueLeft { get; }
    public int RareSold { get; }
    public int RepBefore { get; }  public int RepAfter { get; }
    public int RepDelta => RepAfter - RepBefore;
    public int Star { get; }                  // 月末の判定のあとの星
    public int RepToNextStar { get; }
}

public sealed class YearRecord
{
    public int Year { get; }
    public int RepGained { get; }             // 年末の評判 − 年初の評判（基本設計 7.2）
    public int Sales { get; }
    public int Star { get; }
    public ReviewGrade Grade { get; }         // 講評の 3 段階（下）
}

public sealed class GameResult
{
    public int TotalSales { get; }            // スコア
    public int FinalStar { get; }
    public int FinalReputation { get; }
    public IReadOnlyList<int> StarReachedMonth { get; }
    public IReadOnlyList<YearRecord> Years { get; }
}
```

**年末審査の講評**（基本設計 7.4。数値上の効果はない）: `RepGained` を 3 段階に分ける。境目は `BalanceDef` に置く（案: 1 年目 150 未満 / 150〜449 / 450 以上、2 年目 300 未満 / 300〜899 / 900 以上）。`ReviewGrade { Steady, Good, Excellent }`。文言は UI 側（T-10・T-13 で決める）。

### 9.4 スコア送信

```csharp
public interface IScoreSubmitter
{
    // 成功・失敗をコールバックで返す（WebGL は非同期の通信になるため）
    void Submit(int score, Action<bool> onDone);
}
```

- Core はこのインターフェースを置くだけで呼ばない。結果発表の画面（UI）が `GameResult.TotalSales` を渡して呼ぶ
- 結果発表を開いたときに 1 回。失敗したら「送信できませんでした」と再送ボタン（基本設計 7.5）

---

## 10. イベントとコマンド

### 10.1 イベント一覧

```csharp
public abstract class GameEvent { public int Tick { get; } }   // 起きた時点の ElapsedTicks
```

すべて `sealed class XxxEvent : GameEvent`、中身は get だけのプロパティ。View は `switch (e)` の型パターンで振り分ける。

| 分類 | イベント | 中身 | 積むところ |
|---|---|---|---|
| 進行 | `SessionStarted` | — | 4.2 |
| | `DayChanged` | Day, Month | 4.3 |
| | `MonthEnded` | MonthlyReport | 9.2 |
| | `StarRaised` | NewStar, Unlocked (ItemDef[]) | 9.2 |
| | `YearEnded` | YearRecord | 9.2 |
| | `GameEnded` | GameResult | 9.2 |
| お金・評判 | `MoneyChanged` | Delta, Reason, Money | 5.1 |
| | `ReputationChanged` | Delta, Reputation, Result, RareBonus | 5.2 |
| | `WarehouseChanged` | Item, Count | 5.3 |
| 客 | `CustomerSpawned` | CustomerId, Job, Tile | 8.1 |
| | `CustomerMoved` | CustomerId, From, To, Ticks | 8.4 |
| | `CustomerEmote` | CustomerId, Kind（OutOfStock / TooExpensive / NotCarried / Picked / Treasure） | 8.4 |
| | `CustomerLeft` | CustomerId, Result, Bought (ItemDef[]) | 8.5 |
| | `CustomerExited` | CustomerId | 8.4 |
| 棚・会計 | `ShelfChanged` | FixtureId, Item, Displayed | 7.1 |
| | `ShelfEmptied` | FixtureId | 7.1 |
| | `ItemTaken` | FixtureId, CustomerId, Item | 8.4 |
| | `QueueChanged` | FixtureId, Load | 7.2 |
| | `CheckoutDone` | FixtureId, CustomerId, Amount | 7.2 |
| 行商人 | `MerchantArrived` | Item, LeaveTick | 9.1 |
| | `MerchantLeft` | — | 9.1 |
| 配置 | `LayoutChanged` | — | 6.4 |
| | `ShopExpanded` | Stage | 6.4 |

- 掘り出し物を手に取ったときは `CustomerEmote(Treasure)` を積む（「お宝！」の演出 基本設計 6.2.1）
- 同じ Tick の中では、上の表の「積むところ」の処理順で並ぶ

### 10.2 コマンドと失敗理由

```csharp
public enum FailReason
{
    None,
    InvalidCount, NotEnoughMoney, Locked, NotSoldHere,            // 仕入れ
    MerchantGone,                                                 // 行商人
    OutOfBounds, BlocksEntrance, Occupied, NoPayTile,             // 配置
    LimitReached, BlocksPath, LastCounter, NotFound,
    WrongCategory, NotInWarehouse,                                // 棚の設定
    MaxStage,                                                     // 拡張
    GameOver,                                                     // GameEnded 後のすべてのコマンド
}

public readonly struct CommandResult
{
    public bool Ok { get; }
    public FailReason Reason { get; }
    public static CommandResult Success { get; }
    public static CommandResult Fail(FailReason r);
}
```

| コマンド | 失敗理由（判定順） | 成功時のイベント |
|---|---|---|
| `BuyFromWholesaler(item, n)` | GameOver → 5.4 の表 | MoneyChanged, WarehouseChanged |
| `BuyFromMerchant(n)` | GameOver → 9.1 | MoneyChanged, WarehouseChanged |
| `PlaceFixture(def, tile)` | GameOver → 6.3 の表 | MoneyChanged（費用 > 0）, LayoutChanged, ShelfChanged（棚）, CustomerMoved（どかした客） |
| `MoveFixture(id, tile)` | GameOver → NotFound → 6.3 | LayoutChanged ほか |
| `RemoveFixture(id)` | GameOver → NotFound → 6.3 | MoneyChanged（払い戻し > 0）, WarehouseChanged, LayoutChanged |
| `AssignShelf(id, item)` | GameOver → NotFound → 7.1 の表 | WarehouseChanged, ShelfChanged |
| `ExpandShop()` | GameOver → MaxStage → NotEnoughMoney | MoneyChanged, ShopExpanded, LayoutChanged |

- **失敗したときは状態を一切変えず、イベントも積まない**（テストで確かめる。13 章 UT-STK-03 など）
- UI は `FailReasonText.Get(reason)` で表示文にする（「お金が足りません」など。基本設計 4.3 の文言）

---

## 11. Data と Game（Unity 側）

### 11.1 ScriptableObject（Shop.Data）

`[CreateAssetMenu(menuName = "Shop/...")]` をつける。フィールドは `[SerializeField] private` ＋ 読み取りプロパティ。

| アセット | フィールド |
|---|---|
| `ItemData` | id, displayName, category（Tool/Weapon/Armor）, buyPrice, sellPrice, unlockStar, icon (Sprite) |
| `RareItemData` | id, displayName, buyPrice, sellPrice, appearStar, job (JobData, null=全職業), repBonus, icon |
| `JobData` | id, displayName, wants: List<{ ItemData item; int weight; }>, sprite |
| `StarTable` | rows: List<{ requiredRep, baseVisits, budgetMin, budgetMax, merchantChancePercent }>（[0] が ★1） |
| `FixtureData` | id, displayName, kind, shelfCategory, cost, width, height, walkable, displayMax, restockTicks, ambience, maxCount, countsTowardShelfLimit, usesFreeShelf, sprite |
| `ShopSizeTable` | rows: List<{ width, height, shelfMax, counterMax, cost }> |
| `BalanceConfig` | 3.2 の `BalanceDef` の各値、時間（ticksPerDay, daysPerMonth, monthsPerYear, totalMonths）、entrance、initialLayout: List<{ FixtureData; Vector2Int }>, 講評の境目 |
| `GameDatabase` | 上をすべて参照する 1 つのアセット（items[], rareItems[], jobs[], starTable, fixtures[], sizes, balance） |

- アセットの置き場所: `Assets/_Project/Data/`（`Items/` `RareItems/` `Jobs/` `Fixtures/` に分ける）
- `ConfigBuilder.Build(GameDatabase db) : GameConfig` で変換する。Sprite などの見た目は Core に渡さない。View は `ItemDef.Id` から `GameDatabase` を引いてアイコンを取る（`GameDatabase.IconOf(string id)`）
- 作り方の手間を減らすため、エディタ拡張 `Shop/Create Step1 Data`（youken 5 章の値でアセットを一式作る）を作ってもよい（案）。数値の正本は youken.md のまま

### 11.2 GameRunner（倍速・一時停止）

```csharp
public sealed class GameRunner : MonoBehaviour
{
    [SerializeField] GameDatabase _database;
    [SerializeField] EventRouter _router;
    public GameSession Session { get; private set; }
    public int Speed { get; private set; } = 1;          // 1 or 2
    public bool IsPaused => _pauseReasons != PauseReason.None;

    public void StartNewGame(ulong seed);                  // シードは時刻から。デバッグ時はインスペクターで固定
    public void SetSpeed(int speed);
    public void Pause(PauseReason r);  public void Resume(PauseReason r);

    void Update()
    {
        if (Session == null || Session.IsEnded) { Route(); return; }
        if (!IsPaused)
        {
            _accumulator += Time.unscaledDeltaTime * Speed;
            int n = 0;
            while (_accumulator >= TimeConfig.SecondsPerTick && n < MaxTicksPerFrame)
            {
                Session.Tick(); _accumulator -= TimeConfig.SecondsPerTick; n++;
                if (_pauseReasons != PauseReason.None) break;   // 月末で止まったらそこで止める
            }
            if (n == MaxTicksPerFrame) _accumulator = 0;          // 超えた分は捨てる（基本設計 2.2）
        }
        Route();
    }

    void Route() => _router.Dispatch(Session.DrainEvents());   // 一時停止中もコマンドのイベントを配る
}

[Flags] public enum PauseReason { None = 0, User = 1, Placement = 2, Expand = 4, Merchant = 8, Modal = 16 }
```

- 一時停止は**理由ごとのフラグ**で持つ（配置モード中に月末収支が出る… ことはないが、ユーザーの ⏸ と行商人パネルが重なるなどに備える）
- 月末に 1 フレームで何 Tick も進めると、月末収支を出す前に翌月へ進んでしまう。そのため `Tick()` のたびに、**その Tick で `MonthEnded` を積んだか**を見て止める。具体的には `EventRouter` が `MonthEnded` を受けたら `Pause(Modal)` するのでは間に合わない（配るのはループのあと）ので、`GameSession` に `bool MonthEndedThisTick` を持たせ、ループの中で見て `Pause(Modal)` してから抜ける
- `MaxTicksPerFrame` = 10（基本設計 2.2）。2 倍速・60fps で 1 フレーム約 0.33 Tick なので、ふだんは届かない

### 11.3 EventRouter と View

- `EventRouter.Dispatch(list)` は、受け取ったイベントを順に各 View / UI の `Handle(GameEvent)` に渡す。登録は `[SerializeField]` で並べる（順番が決まる）
- View は**状態を持たない**か、Core の状態の写しだけを持つ。表示がずれたら `Session` を読み直して描き直してよい（`LayoutChanged` では全部描き直す）

| クラス | 受けるイベント | すること |
|---|---|---|
| `GridView` | LayoutChanged, ShopExpanded | 床のマス、壁、入口、街からの道（FR-REP-07） |
| `FixtureView` | LayoutChanged | 配置物のスプライト。棚は `ShelfView` を子に持つ |
| `ShelfView` | ShelfChanged, ShelfEmptied | 並んでいる数の表示（アイコンを n 個）。空なら空の絵。特売台はキラキラ |
| `CounterView` | QueueChanged, CheckoutDone | 「チャリン」、売上の浮き文字 |
| `CustomerView` | CustomerSpawned/Moved/Emote/Left/Exited | `CustomerViewPool` から出し入れ。`CustomerMoved` の Ticks を秒（÷10 ÷ Speed）にして補間。Spawned では道の端から入口まで歩かせる |
| `MerchantView` | MerchantArrived, MerchantLeft | 入口の横に立つ、頭上の「!」、タップで行商人パネル |
| `FloatingText` | MoneyChanged, ReputationChanged | 「+100G」「+2」 |

`TilePosition`: `Vector3 ToWorld(Tile t) = origin + new Vector3(t.X + 0.5f, t.Y + 0.5f) * tileSize`。1 マスの大きさは素材が決まってから（kousei 5 章）。仮は 1 ユニット。

### 11.4 UI

| クラス | 画面 | 時間 | 主な動き |
|---|---|---|---|
| `Hud` | SC-02 上 | — | `MoneyChanged`・`ReputationChanged`・`DayChanged` で更新。月のバーは毎フレーム `Clock.MonthProgress`。⏸ ▶ ▶▶ |
| `BottomMenu` | SC-02 下 | — | 仕入れ / 配置 / 拡張。行商人が `Present` の間は `[行商人 残り n 秒]` を出す |
| `WholesalerPanel` | SC-03 | 進む | `Session.Config.Items` の問屋の品を並べる。未解禁はグレーで「★n で解禁」。+1/+5/+10、合計、購入 → `BuyFromWholesaler` |
| `PlacementMode` | SC-04 | 止める（Placement） | マウスのマスごとに `CanPlace` を呼んで緑 / 赤と理由。クリックで `PlaceFixture`。配置物を選んで移動・撤去（撤去の確認に戻る金額）。完了で `Resume` |
| `ShelfAssignPanel` | SC-05 | 進む | 棚の系統の品（解禁済み）と倉庫の数、「何も置かない」。特売台は `Warehouse.RareItemsInStock` |
| `ExpandPanel` | SC-06 | 止める（Expand） | 次の段階の広さ・上限・費用 → `ExpandShop` |
| `MerchantPanel` | SC-07 | 止める（Merchant） | 品、欲しがる職業、仕入れ値・売値、1〜2 個、`TicksLeft` のゲージ。特売台がなければ注意書き |
| `ModalQueue` | — | 止める（Modal） | 下の 11.5 |
| `MonthlyReportDialog` | SC-08 | 止める | `MonthlyReport` を出す。OK / Enter / Space で閉じる |
| `StarUpEffect` | SC-09 | 止めない | 2 秒の演出 |
| `YearReviewDialog` | SC-10 | 止める | `YearRecord`、講評 |
| `ResultDialog` | SC-11 | 止める | `GameResult`、スコア送信、もう一度 / タイトルへ |

**開始直後の案内**（基本設計 4.5）: `SessionStarted` を受けたら `PlacementMode` を開き、案内「まずは棚を 3 台置きましょう（無料）」。完了時に棚が 0 台なら確認ダイアログ、道具棚がなければヒント。完了後に仕入れボタンを光らせる。

### 11.5 ModalQueue（月末からの順番）

1 つの Tick で `MonthEnded` → `StarRaised` → `YearEnded` → `GameEnded` が続けて来ることがある。これを順番に出す。

```
受けたら列に足す: MonthEnded → 月末収支, StarRaised → 星アップ,
                 YearEnded → 年末審査, GameEnded → 結果発表
列が空でなければ Pause(Modal)
先頭を出す。閉じたら次へ
星アップ（止めない演出）の番:
    列にほかがなければ Resume(Modal) してから 2 秒の演出
    ほかがあれば、止めたまま 2 秒の演出 → 次へ
列が空になったら Resume(Modal)（結果発表は閉じない）
```

### 11.6 吹き出しのセリフ（CommentTable）

- `CommentTable`（ScriptableObject、UI 側）に `CustomerResult` ごとのセリフの一覧と、買った品の系統で差し込む `{item}` を持つ
- Core は結果と買った品だけを渡す。セリフ集（T-13）が決まるまでは仮の 1 行ずつ

### 11.7 unityroom のスコア送信（要確認）

- `UnityroomScoreSubmitter : MonoBehaviour, IScoreSubmitter`。unityroom の公式ライブラリ（unityroom-client-library）の送信 API を呼ぶ。ボード番号・書き込みモード（大きいほうを残す）はインスペクターで設定
- HMAC キーは `Assets/_Project/Secrets/` のアセットから読む（Git に入れない kousei 6・7 章）。エディタではキーがなければ送らずにログだけ出す
- ライブラリの導入方法と API の名前は公開前（M8）に最新の手順を確かめる

---

## 12. 基本設計からの補足と変更

本書で決めたもの。すべて（案）。2026-10-24 に基本設計（[kihonsekkei.md](kihonsekkei.md) 0.4、一覧は 11.1）へ反映した。

| # | 内容 | 理由 | 本書 |
|---|---|---|---|
| K-01 | 1 Tick の処理順を「各システム → 時計を進める → 月末処理」に変えた（基本設計 9.3 は時計が先） | 時計が先だと、月の最後の Tick を処理するときに翌月の Tick 0 になり、予定とずれる | 4.3 |
| K-02 | 会計は、行列の先頭が 1 番目の行列マスにいれば始め、会計マスへの 1 歩を会計時間に含める | 着いてから始めると 1 人 27 Tick になり、月 25 人（youken 5.5）を下回る | 7.2 |
| K-03 | 最長滞在（40 秒）を超えたら、残りの欲しいものをあきらめて**会計に向かう**（かごが空なら帰る）。行列・会計中は対象外 | 基本設計は「強制的に Leaving」だが、かごの品の扱いが決まっていなかった。会計中の客を消すと売上が消える | 8.4 |
| K-04 | 掘り出し物の希望は「おまけ」。来店してから売り切れても失敗にせず、しょんぼりもしない | 掘り出し物は結果（満足・不満）に数えない（基本設計 5.5）ので、見せ方もそろえる | 8.4 |
| K-05 | カウンターを置くとき、会計マスがグリッドの内側で空いていることを条件に足した | 下端に置くと会計マスが外に出る。L4 だけでは判定の前に座標が外れる | 6.3 |
| K-06 | 棚に着いたら売り切れていたとき、在庫のある同じ品の棚が別にあれば 1 回だけそちらへ向かう | 棚を 2 台並べた意味が出るように。何度も迷わないよう 1 回まで | 8.4 |
| K-07 | 24 か月目の終わりに店内に残っている客の売上は数えない | 時計は 24 か月で止まる。来店は月の最後 3 秒にはないので、残るのは会計待ちの数人。影響は小さい | 9.2 |
| K-08 | 吹き出しのセリフは Core ではなく View が選ぶ | セリフ集（T-13）が Core の乱数と再現性に影響しないように | 8.5, 11.6 |
| K-09 | テストのアセンブリから `Shop.Data` を参照する | ScriptableObject の値が youken 5 章と一致するかをテストで確かめるため（`ConfigAssetTests`） | 2 章 |
| K-10 | 月をまたいだ客の売上・評判は、会計した月（結果が決まった月）に数える。来店数は来店した月 | 集計を「起きたときに足す」だけで済ませる | 9.2 |
| K-11 | 雰囲気値の効果を千分率の整数（1 あたり 15）で持ち、四捨五入する | Core の状態に浮動小数を入れない（1.1）。youken 5 章の値で .5 ちょうどになるのは ★1・雰囲気 20 の 19.5 だけで、シミュレーションの `round`（偶数への丸め）と同じ 20 になる | 3.2, 8.1 |
| K-12 | 評判の 0 止めを客ごとにする（シミュレーションは月ごと） | 実時間で客ごとに足すため。評判 0 の近くにいるのは序盤の数か月だけ | 5.2 |
| K-13 | 本書で足した値: 手に取る時間 0.5 秒、しょんぼりの時間 1 秒、うろうろ先は入口から 2〜4 マス、年末講評の境目 | 基本設計に値がなかった | 3.2, 8.4, 9.3 |

---

## 13. テストケース

`Support/TestConfig.Step1()` で youken 5 章の値の `GameConfig` を作る。`SessionBuilder` は「所持金 n で」「★3 で」「棚をこの位置に置いて」などの状態を作ってから始めるための手助け（内部の状態を直接いじらず、コマンドと `Tick` で作る。どうしても必要なものだけ `internal` を `InternalsVisibleTo("Shop.Tests.EditMode")` で開ける）。

### 13.1 単体テスト（EditMode）

| ID | 対象 | 確かめること |
|---|---|---|
| UT-CFG-01 | ConfigValidator | `TestConfig.Step1()` がエラーなく通る |
| UT-CFG-02 | ConfigValidator | ID の重複、星の必要評判が減る、入口が外、などでそれぞれ例外 |
| UT-CFG-03 | ConfigAssetTests | `GameDatabase` のアセットから作った GameConfig が `TestConfig.Step1()` と全項目一致 |
| UT-STK-01 | 問屋 | 買うとお金が `仕入れ値 × 個数` 減り、倉庫が増える |
| UT-STK-02 | 問屋 | 未解禁（★1 で鉄の剣）は `Locked` |
| UT-STK-03 | 問屋 | お金不足は `NotEnoughMoney`。所持金・倉庫・イベントが変わらない |
| UT-STK-04 | 問屋 | 限定品は `NotSoldHere`、個数 0 は `InvalidCount` |
| UT-SHF-01 | 棚 | 商品を設定すると、倉庫から DisplayMax（武器 3 / 道具 8）まですぐ並ぶ |
| UT-SHF-02 | 棚 | 1 個減らすと、RestockTicks（40 / 15）後に 1 個補充される。その前には補充されない |
| UT-SHF-03 | 棚 | 倉庫が空なら補充されない。並ぶ数 0 で `ShelfEmptied` |
| UT-SHF-04 | 棚 | 商品を変えると並んでいた分が倉庫に戻る |
| UT-SHF-05 | 棚 | 武器棚に薬は `WrongCategory`。特売台に問屋の品は `WrongCategory`。倉庫にない限定品は `NotInWarehouse` |
| UT-SHF-06 | 棚 | 600 Tick ずっと売れ続けたとき、武器棚から売れる数が 18（最初に並ぶ 3 個 ＋ 補充 15 個）。2 か月目以降は月 15 |
| UT-LAY-01 | 配置 | L1: 外・入口・重なりで失敗 |
| UT-LAY-02 | 配置 | カウンターを y=0 に置くと `NoPayTile` |
| UT-LAY-03 | 配置 | L2: 段階 1 で棚 7 台目は `LimitReached`。特売台 2 台目は `LimitReached`、特売台は棚の数に入らない |
| UT-LAY-04 | 配置 | L3: お金不足。無料枠が残っていれば所持金 0 でも棚を置ける |
| UT-LAY-05 | 配置 | L4: 入口を囲む・棚の 4 方向をふさぐ・会計マスを孤立させる配置は `BlocksPath` |
| UT-LAY-06 | 配置 | L5: 最後のカウンターは撤去できない |
| UT-LAY-07 | 配置 | 撤去で払った額が全額戻る。無料枠の棚は 0G。無料枠の残りは戻らない |
| UT-LAY-08 | 配置 | 拡張で広さが変わり、既存の配置物の座標は変わらない。最大段階は `MaxStage` |
| UT-LAY-09 | 配置 | 客の立っているマスに置くと、客が近くの通れるマスへ移る |
| UT-PTH-01 | 経路 | 障害物を避けた最短経路。同じ長さのときは毎回同じ経路 |
| UT-PTH-02 | 経路 | 届かないときは null。絨毯の上は通れる |
| UT-CUS-01 | 客の生成 | ★1 では解禁済みの品だけを欲しがる（10,000 人） |
| UT-CUS-02 | 客の生成 | 予算が星の範囲内（両端を含む） |
| UT-CUS-03 | 客の生成 | 2 品の割合が約 40%（重複で 1 品になる分を見込む） |
| UT-CUS-04 | 来店予定 | 雰囲気 0 / 10 / 20 で、来店数が基本 ×1.0 / ×1.15 / ×1.3（四捨五入） |
| UT-CUS-05 | 来店予定 | 来店時刻がすべて 0 以上 570 未満 |
| UT-CUS-06 | 客の生成 | 特売台に限定品があり、職業が合い予算が足りると、欲しいものの先頭に入る。職業が違えば入らない |
| UT-BHV-01 | 客の行動 | 在庫のある棚へ歩き、手に取り、会計して `Full`、評判 +2、売上が増える |
| UT-BHV-02 | 客の行動 | 品切れの棚でしょんぼり（`CustomerEmote(OutOfStock)`）→ 何も買えず `None`、評判 −2 |
| UT-BHV-03 | 客の行動 | 予算より高い品は買わない（`TooExpensive`） |
| UT-BHV-04 | 客の行動 | 2 品のうち 1 品だけ買えて `Partial`、評判 ±0 |
| UT-BHV-05 | 客の行動 | 置いていない品: 同じ系統の棚へ行ってしょんぼり / 系統の棚もなければうろうろ |
| UT-BHV-06 | 客の行動 | 売り切れの棚に着いたとき、在庫のある同じ品の棚が別にあれば 1 回だけそちらへ（K-06） |
| UT-BHV-07 | 客の行動 | 最長滞在を超えると会計へ向かう（K-03） |
| UT-BHV-08 | 客の行動 | 掘り出し物だけ買えた → `None` −2 ＋ ボーナス |
| UT-CNT-01 | 会計 | 会計 1 人 24 Tick。5 人並べると最後の人は 24×5 Tick 後に終わる（K-02） |
| UT-CNT-02 | 会計 | 120 Tick を超えて待つと `QueueLeft`、評判 −1、かごの品が棚へ戻る（満杯なら倉庫へ） |
| UT-CNT-03 | 会計 | 並ぶカウンターは待ち人数が少ないほう |
| UT-CNT-04 | 会計 | 並んでいたカウンターを撤去すると、別のカウンターの最後に並び直し、待った時間を引き継ぐ |
| UT-REP-01 | 評判 | 0 未満にならない |
| UT-REP-02 | 評判 | 月の途中で必要評判を超えても星は変わらず、月末に上がる。`StarRaised` に解禁品が入る |
| UT-REP-03 | 評判 | 一度に 2 段階上がる |
| UT-REP-04 | 評判 | 評判が下がっても星は下がらない |
| UT-MER-01 | 行商人 | ★1 で 10,000 か月抽選して来る割合が約 20%（±2%） |
| UT-MER-02 | 行商人 | 品揃えが「今の星か 1 つ下の星」の品。★1 は ★1 の品だけ |
| UT-MER-03 | 行商人 | 来る時刻が 60〜399、150 Tick で帰る。帰ったあとは `MerchantGone` |
| UT-MER-04 | 行商人 | 3 個は `InvalidCount`。2 個まで買える。特売台がなくても買えて倉庫に入る |
| UT-MER-05 | 行商人 | 限定品は月をまたいでも倉庫に残る |
| UT-END-01 | 月末 | 600 Tick で `MonthEnded`。収支（売上・仕入れ・設備投資）が実際のお金の動きと一致 |
| UT-END-02 | 月末 | 12 か月目に `YearEnded`、24 か月目に `GameEnded`。その後の `Tick` とコマンドは何もしない（`GameOver`） |
| UT-END-03 | 月末 | `GameResult.TotalSales` が全月の売上の合計と一致し、払い戻しを含まない |
| UT-DET-01 | 再現性 | 同じシード・同じコマンド列で 24 か月回すと、全月の `MonthlyReport` とイベント列が完全に一致 |
| UT-DET-02 | 再現性 | シードが違えば結果が変わる |

### 13.2 シミュレーションとの比較（Slow）

`AutoPlayer` は Core のコマンドだけで遊ぶ。balance_sim.py の `run()` と同じ方針にする。

```
毎月の初め（MonthEnded を処理したあと）:
    1. 棚: 上限まで、所持金 >= 費用 + 150 のあいだ置く（種類は 2 の割り当てに合わせる）
    2. カウンター: 台数 × 25 < 来店数 のあいだ、上限まで、所持金 >= 250 + 150 なら置く
    3. 拡張: 所持金 >= 次の費用 + 400 で、棚かカウンターが上限なら拡張
    4. 装飾: 高いものから 1 つ、所持金 >= 費用 + 800 なら置く（雰囲気の上限まで）
    5. 棚の割り当て: 解禁品ごとの見込み需要（来店数 × 職業ごとの欲しがる確率の平均）と、
       棚 1 台が月に売れる数（15 / 40）から、利益＋評判×15 が大きくなるよう貪欲に割り当てる
    6. 仕入れ: 割り当てた品ごとに「見込み需要と棚の販売上限の小さいほう − 倉庫と棚の数」を買う
行商人が来たら: 買えるだけ（2 個まで）買う。特売台がなく 200G あれば置いて、限定品を並べる
置く場所: グリッドを上の行から左→右に見て、最初に CanPlace が通るマス
```

- シミュレーションの `choose_display`（客 150 人を作って全組み合わせを試す）は Core の乱数を引いてしまうので使わず、期待値で割り当てる。そのぶん少し下手になる
- 20 シードで 24 か月を回し、基本設計 10.2 の許容範囲（★5 の月 18〜24、総売上 60,000〜100,000G）に入るかを見る
- 外れても**テストは失敗にしない**（`Assert.Inconclusive` でログを残す）。数値の調整の材料にする

### 13.3 手で確かめること

基本設計 10.3 のとおり。これに加えて:

- 2 倍速で 1 か月が 30 秒になり、月末で必ず止まる（11.2）
- 配置モード中にタブを裏に回して戻しても、客が飛ばない
- 月末収支 → 星アップ → 年末審査が順番に出る（12 か月目に星が上がったとき）

---

## 14. 実装の順番

kousei.md 8 章の M2〜M8 に、本書のクラスを割り当てる。各段階の終わりに EditMode テストがすべて通り、Unity で動くこと。

| 段階 | Core | Data / Game | テスト |
|---|---|---|---|
| M2 店内 | `Tile`, `CommandResult`, Config 一式, `ConfigValidator`, `ShopLayout`, `PlacementValidator`, `GridPathfinder`, `GameSession`（配置コマンドだけ） | `GameDatabase` と各 SO, `ConfigBuilder`, `GameRunner`, `EventRouter`, `GridView`, `FixtureView`, `PlacementMode`（仮の四角） | UT-CFG, UT-LAY, UT-PTH |
| M3 客と販売 | `Warehouse`（初期在庫はデバッグ用に直接入れる）, `Shelf`, `Counter`, `Customer`, `CustomerFactory`, `CustomerSpawner`, `CustomerBrain`, `ShopState`（お金・売上） | `ShelfView`, `CounterView`, `CustomerView`, `FloatingText`, `Hud`（所持金） | UT-SHF, UT-CUS, UT-BHV, UT-CNT |
| M4 仕入れと時間 | `Wholesaler`, `MonthlyReport`, 月末処理（星なし） | `WholesalerPanel`, `ShelfAssignPanel`, `BottomMenu`, `ModalQueue`, `MonthlyReportDialog`, 倍速・一時停止 | UT-STK, UT-END-01 |
| M5 評判 | `ReputationSystem`, 星・解禁 | `StarUpEffect`, `CommentTable`, Hud の星と評判 | UT-REP |
| M6 行商人・拡張・装飾 | `Merchant`, 特売台, `ExpandShop`, 雰囲気値 | `MerchantView`, `MerchantPanel`, `ExpandPanel` | UT-MER, UT-LAY-08, UT-CUS-04, 06 |
| M7 終わり方 | `YearRecord`, `GameResult`, `IScoreSubmitter` | `YearReviewDialog`, `ResultDialog`, `TitleScreen`, Title シーン | UT-END-02, 03, UT-DET, 比較テスト |
| M8 公開準備 | — | `UnityroomScoreSubmitter`, WebGL ビルド設定, 日本語フォント | 手で確かめること（13.3） |

- M3 は問屋がまだないので、`SessionBuilder` かデバッグ用のボタンで倉庫に品を入れて動かす
- `AutoPlayer` と比較テスト（13.2）は M6 で全部の要素がそろってから書くが、M4 の時点で「仕入れと棚だけ」の版を回すと、補充と会計の実時間化（K-02）のずれを早めに見つけられる

---

## 変更履歴

| 版 | 日付 | 内容 |
|---|---|---|
| 0.1 | 2026-10-24 | 初版。基本設計 0.3 をもとに、Core のクラス・処理順・判定順、Data・Game のクラス、テストケース、実装の順番を決めた。基本設計への補足と変更を K-01〜K-13 にまとめた |
| 0.2 | 2026-10-24 | K-01〜K-13 を基本設計（0.4）に反映したことを明記 |
