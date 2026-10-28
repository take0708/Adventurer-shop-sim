# Unity プロジェクト構成（Step 1）

| 項目 | 内容 |
|---|---|
| 版 | 0.8 |
| 作成日 | 2026-10-22 |
| 更新日 | 2026-10-24 |
| 対象 | [youken.md](youken.md) の T-09。ゲームの動きの設計は [kihonsekkei.md](kihonsekkei.md) |

凡例: **（確定）** = 相談して決めたもの / **（案）** = 実装しながら変えてよいもの

---

## 1. 開発環境

| 項目 | 内容 |
|---|---|
| Unity（確定） | **6000.5.6f1**（インストール済み） |
| テンプレート（確定） | 2D（URP）。Unity 同梱の `com.unity.template.2d-cross-platform-2d 6.1.5` から作成 |
| パッケージ | テンプレートから collab-proxy（Unity のバージョン管理）と Visual Scripting を外した。Test Framework 1.4.5、uGUI 2.0（TextMeshPro を含む）、URP 17.6、Input System 1.19 など |
| Unity の操作（確定） | Unity エディタは **MCP でつないで作業する**。Unity 公式の AI Assistant（`com.unity.ai.assistant` 2.21.0-pre.1）の MCP サーバーを使う。エディタで C# を実行する `Unity_RunCommand`、コンソールの取得、画面の撮影などができる |
| アセット生成（確定） | **Unity AI のアセット生成は使わない**。M2〜M5 は仮の素材で作り、本番の素材は別に用意する（T-11） |
| ビルド先（確定） | WebGL（unityroom）。WebGL モジュールはインストール済み |
| 将来のビルド先 | Windows（Steam）。Windows モジュールはインストール済み |
| UI（確定） | **uGUI**（Canvas）＋ TextMeshPro |
| 数値データ（確定） | **ScriptableObject** |
| バージョン管理（確定） | **Git ＋ GitHub（公開リポジトリ）** |

---

## 2. フォルダ構成

Unity プロジェクトは `C:\dev\Adventurer-shop-sim` で、そのフォルダ自体をリポジトリのルートにする（確定、2026-10-24 変更）。文書・シミュレーションは Unity プロジェクトのルートに同居させる（Unity は `Assets/` などの外のファイルを読まない）。

```
Adventurer-shop-sim/              ← Unity プロジェクト兼 Git リポジトリのルート
├─ youken.md / balance.md / kousei.md / kabeuchi.md / zankadai.md
├─ tools/
│   └─ balance_sim.py             ← 数値シミュレーション
├─ Packages/manifest.json
├─ ProjectSettings/
└─ Assets/
    ├─ Settings/              ← テンプレートの URP 設定（そのまま使う）
    ├─ Scenes/SampleScene.unity ← テンプレートのサンプル。Title / Game を作ったら消す
    ├─ _Project/              ← 自作のものはすべてここ（アセットストア品と混ざらない）
        ├─ Scenes/            ← Title.unity / Game.unity（M2 以降で作る）
        ├─ Scripts/
        │   ├─ Shop.Game.asmdef ← View/ と UI/ をまとめるアセンブリ
        │   ├─ Core/          ← ゲームのルール（Unity に依存しない C#）。Shop.Core.asmdef
        │   ├─ Data/          ← ScriptableObject の定義。Shop.Data.asmdef
        │   ├─ View/          ← 店内の表示（客・棚のスプライト、移動の補間）
        │   └─ UI/            ← HUD・メニュー・月末収支・結果発表
        ├─ Data/              ← ScriptableObject のアセット（数値の実体）
        ├─ Prefabs/
        ├─ Sprites/
        ├─ Fonts/
        └─ Audio/
    └─ Tests/
        └─ EditMode/          ← Core のテスト。Shop.Tests.EditMode.asmdef
```

- 空のフォルダには `.gitkeep` を置いて Git に残している（Unity は `.` で始まるファイルを無視する）
- `Library/` などのほか、`Assets/ThirdParty/` と `Assets/_Project/Secrets/` は Git に入れない（7.1）

### シーン（案）

| シーン | 内容 |
|---|---|
| Title | タイトル、スタートボタン |
| Game | 店内、HUD、メニュー。月末収支・年末審査・結果発表はパネルとして重ねて表示 |

---

## 3. スクリプトの分け方

### 3.1 基本方針: ルールと見た目を分ける（案）

```
 ScriptableObject（数値）
        │ 起動時に変換
        ▼
 ┌──────────── Core（普通の C#）────────────┐
 │ 時間・お金・在庫・客・評判・行商人のルール │ ← EditMode テストで確かめる
 └───────────────────────────────────────┘
        │ イベント（客が来た / 売れた / 月が終わった / 星が上がった…）
        ▼
 View / UI（MonoBehaviour）… 受け取って描くだけ。ルールは持たない
```

- **Core は UnityEngine を参照しない**。Python シミュレーションと同じ中身を C# で持つので、数値の挙動をテストで比べられる
- 乱数は Core が持つ（シードを指定できる）。同じシードなら同じ展開になり、不具合を再現しやすい
- 客の位置もマス単位で Core が管理する。View はマスの間を滑らかに動かすだけ

### 3.2 アセンブリ定義（案）

| アセンブリ | 中身 | 参照先 |
|---|---|---|
| `Shop.Core` | ルール。`noEngineReferences: true` | なし |
| `Shop.Data` | ScriptableObject の定義と、Core 用の値への変換 | Core |
| `Shop.Game` | View・UI | Core, Data |
| `Shop.Editor` | エディタ拡張（データのアセットを一式作る、シーンを組むなど）。`Scripts/Editor/`、エディタのときだけ | Core, Data, Game |
| `Shop.Tests.EditMode` | Core のテストと、ScriptableObject の値の確認（K-09） | Core, Data |

### 3.3 Core の主なクラス（案）

| クラス | 役割 | 対応する要件 |
|---|---|---|
| `TimeConfig` | 時間の単位（1 日 20 Tick、1 か月 30 日、24 か月）。**実装済み** | FR-TIME-01, 02 |
| `GameClock` | ゲーム内の時間。日・月末・年末・終了を知らせる。倍速・一時停止は持たない（GameRunner 側）。**実装済み** | FR-TIME |
| `IRandom` / `SeededRandom` | シード指定できる乱数（xorshift64*。System.Random は使わない）。**実装済み** | — |
| `ShopState` | 所持金、評判ポイント、星、総売上 | FR-REP, FR-END |
| `Warehouse` | 倉庫の在庫 | FR-STOCK |
| `Wholesaler` | 問屋。星に応じた品揃え | FR-STOCK-06 |
| `ShopLayout` | グリッド、配置物、広さの段階 | FR-LAYOUT, FR-EXP |
| `Shelf` | 並べる商品、補充（並ぶ数と補充間隔。特売台もこれで表す） | FR-STOCK-03〜05, 08, FR-LAYOUT-06 |
| `CustomerSpawner` | 来店数・職業・欲しいもの・予算の決定 | FR-CUST |
| `Customer` | 入店 → 棚へ移動 → 購入 → 会計待ち → 退店 の状態 | FR-CUST |
| `GridPathfinder` | マス目上の経路探索（NavMesh は使わない） | FR-CUST-05 |
| `ReputationSystem` | 評判の増減、星の判定（月末、下がらない） | FR-REP |
| `Merchant` | 行商人の来訪と品揃え | FR-MERCH |
| `MonthlyReport` | 月の収支の集計 | FR-TIME-05 |

このほか `GameSession`・`Counter`・`PlacementValidator` などは [kihonsekkei.md 9.3](kihonsekkei.md#93-クラスの責務kouseimd-33-に追加するもの)。

### 3.4 時間の進め方（案）

- Core は固定の刻み（tick）で進める。View は `Update` で経過時間を tick に変換して Core に渡す（1 Tick = 0.1 秒。詳細は [kihonsekkei.md 2 章](kihonsekkei.md#2-時間の設計)）
- 倍速は tick の量を増やすだけ。`Time.timeScale` は使わない（UI アニメーションが速くならないように）
- 倍速は 1x / 2x の 2 段階から始める
- 倍速と一時停止は GameRunner（Game 側）が持ち、Core は呼ばれた回数だけ進む

---

## 4. 数値データ（ScriptableObject）

[youken.md の 5 章](youken.md#5-数値仕様step-1-初期値) の表と 1 対 1 で対応させる。

| アセット | 中身 | youken.md |
|---|---|---|
| `ItemData`（8 個） | 固定 ID、名前、系統、仕入れ値、売値、解禁の星、アイコン | 5.1 |
| `RareItemData`（5 個） | 名前、仕入れ値、売値、買う職業、評判ボーナス、出始める星 | 5.7 |
| `JobData`（3 個） | 職業名、欲しいものの重み | 5.3 |
| `StarTable` | 星ごとの必要評判、来店数、予算、行商人の出現確率 | 5.2, 5.4, 5.7 |
| `FixtureData`（棚 3・特売台・カウンター・装飾 3） | 費用、大きさ（マス）、通れるか、並ぶ数、補充間隔、置ける数、雰囲気値 | 5.5 |
| `ShopSizeTable` | 広さの段階ごとのマス数と費用 | 5.6 |
| `BalanceConfig` | 初期資金、評判の増減値、雰囲気の効果、時間、会計・待ち時間、初期配置、無料枠、払い戻し率 | 5.4, 5.5, FR-TIME |

基本設計で足した項目の詳細は [kihonsekkei.md 8.2](kihonsekkei.md#82-scriptableobjectkouseimd-4-章からの追加変更)。

- 数値を変えたら、youken.md・balance.md・[tools/balance_sim.py](tools/balance_sim.py) もそろえる
- 遊びながら調整するときは、プレイ中にインスペクターで変えて試し、決まった値をアセットに残す

---

## 5. 画面・表示（案）

| 項目 | 内容 |
|---|---|
| 解像度 | 960×600 を基準（unityroom の標準的な埋め込みサイズ。公開時に確認） |
| ドット絵 | Pixel Perfect Camera を使う。1 マスの大きさは素材が決まってから（T-11） |
| 日本語フォント | WebGL では OS のフォントが使えないため、**日本語フォントを TextMeshPro 用に同梱する**。フリーライセンス（OFL など）のものを選ぶ |
| 仮の絵 | 素材が決まるまでは、色付きの四角と文字で作る |

---

## 6. unityroom 対応（案）

- ランキングへのスコア送信は unityroom の公式ライブラリを使う（導入方法は公開前に最新の手順を確認する）
- スコア送信用の**秘密鍵（HMAC キー）はリポジトリに入れない**（公開リポジトリのため）
- Step 1 はセーブ機能なし（1 プレイ約 25 分で遊び切る）

---

## 7. Git・GitHub（公開リポジトリ）

### 7.1 管理しないもの（.gitignore）

Unity 標準のもの（`Library/` `Temp/` `Obj/` `Logs/` `UserSettings/` `Build/` など）に加えて、次を入れない。

| 入れないもの | 理由 |
|---|---|
| unityroom の HMAC キー | 公開されると、だれでもスコアを送れてしまう |
| 再配布が禁止されている素材 | 素材サイトの規約で、リポジトリでの公開が禁止されていることが多い。アセットストアの有料素材も同じ |

再配布できない素材は、`Assets/ThirdParty/`（.gitignore で除外）に置き、入手元を [README.md](README.md) に書いておく。秘密情報は `Assets/_Project/Secrets/`（同じく除外）に置く。

改行は `.gitattributes` で、Unity の YAML アセット（`.unity` `.prefab` `.asset` `.meta` など）を LF にそろえている。

### 7.2 気をつけること

- **コミットに書かれるメールアドレスも公開される**。気になる場合は GitHub の「noreply」アドレスをコミットに使う
- Git LFS は最初は使わない。画像や音が大きくなってきたら検討する

---

## 8. 作る順番（案）

小さく動かしながら積み上げる。各段階で遊べる状態にする。

| # | 段階 | できること |
|---|---|---|
| M1 | 土台 | プロジェクト作成、Git、フォルダ・アセンブリ、Core のテストが動く（**途中**。11 章） |
| M2 | 店内 | グリッド表示、棚・カウンターを置ける |
| M3 | 客と販売 | 客が来て棚へ歩き、買って帰る。お金が増える |
| M4 | 仕入れと時間 | 問屋で仕入れ、倉庫から補充、月末の収支、倍速 |
| M5 | 評判 | 評判ポイント、星、解禁、吹き出し |
| M6 | 行商人・拡張・装飾 | 快感①、店の拡張、雰囲気値 |
| M7 | 終わり方 | 年末審査、2 年後の結果発表、タイトル画面 |
| M8 | 公開準備 | WebGL ビルド、unityroom の限定公開でテスト、ランキング対応 |

---

## 9. テストの実行

| 方法 | 内容 |
|---|---|
| Unity エディタ | Window → General → Test Runner → EditMode → Run All |
| バッチモード | `bash tools/run_editmode_tests.sh`（エディタでプロジェクトを開いていないとき）。結果は `TestResults-EditMode.xml` |
| MCP 経由 | `Unity_RunCommand` で `TestRunnerApi` を使って EditMode テストを動かす。テストは非同期で終わるので、結果を `SessionState` に入れておき、もう一度 `Unity_RunCommand` を呼んで読み出す。`RunCommand` の中でファイルを読み書きすると「ユーザーの操作が必要」として拒否されるので、ファイルには書かない |

MCP で Play して確かめるときの注意:

- エディタが裏にあるとフレームが進まない。`EditorApplication.isPaused = true` → `EditorApplication.Step()` で 1 フレームずつ進める
- `Unity_Camera_Capture` にカメラの ID を渡すと失敗する。ID なし（Scene ビューの撮影）なら撮れる。IMGUI（OnGUI）のパネルは写らない
- `RunCommand` では `using System.Reflection` が使えない（許可されていない名前空間）
- `Step()` は、`Session.Tick()` などを回すのと同じ `RunCommand` の中で呼ばない（PlayerLoop の再帰のエラーが出る）。別の呼び出しにする
- Play 中にスクリプトを変えると再コンパイルで `GameSession` が消える。Play を止めてから入り直す
- ステップ実行では経過時間がほぼ 0 なので、時間を進めて確かめるときは `GameRunner.Advance(秒)` を呼ぶ

---

## 10. 決まっていないこと

| 項目 | メモ |
|---|---|
| コミットに使うメールアドレス | 最初のコミットは gmail のアドレスで行った。今後 GitHub の noreply アドレスに変えるかは未定（7.2） |

---

## 11. 進み具合

### M1（2026-10-24 完了）

| 項目 | 状態 |
|---|---|
| Unity プロジェクト作成 | ✓ Unity 6000.5.6f1 で開いた。`.meta` と `Packages/packages-lock.json` ができた |
| フォルダ・アセンブリ定義 | ✓ 2 章のとおり。asmdef 4 つ |
| Core のクラス | ✓ `TimeConfig`、`GameClock`、`IRandom` / `SeededRandom` |
| Core のテスト | ✓ EditMode テスト 11 件を Unity で動かし、全件成功（MCP 経由、9 章） |
| MCP | ✓ Unity AI Assistant の MCP サーバーでつないだ（1 章） |
| Git | ✓ 最初のコミットを行い、GitHub の公開リポジトリ（take0708/Adventurer-shop-sim）に push した |

### M2 店内（2026-10-26 完了）

| 項目 | 状態 |
|---|---|
| Core | ✓ `Tile`、`CommandResult`、Config 一式、`ConfigValidator`（V1〜V9）、`ShopLayout`、`PlacementValidator`、`GridPathfinder`、`GameSession`（配置コマンドと拡張）、`ShopState`（お金と無料枠だけ）、イベント（開始・日・お金・配置・拡張） |
| Data | ✓ ScriptableObject 7 種と `GameDatabase`、`ConfigBuilder`。アセットはメニュー **Shop/Create Step1 Data** で youken 5 章の値から作る（`Assets/_Project/Data/`） |
| Game | ✓ `GameRunner`、`EventRouter`、`GridView`、`FixtureView`、`PlacementMode`、`FailReasonText`。シーンはメニュー **Shop/Build Game Scene** で `Assets/_Project/Scenes/Game.unity` に作る |
| テスト | ✓ 47 件すべて成功（UT-CFG-01〜03、UT-LAY-01〜08、UT-PTH-01〜02 ほか） |
| 画面 | ✓ Play で床・壁・入口・道・配置物が出る。マウスでの配置・移動・撤去と左のパネルも手で確かめた（2026-10-26） |

詳細設計からの変更・先送り:

- `ShopState` はお金と無料枠だけ先に作った（L3 の判定に要るため）。評判・星・売上は M3〜M5
- `ExpandShop` は簡単なので M2 で入れた（詳細設計では M6）。UT-LAY-08 も M2 で通している
- UT-LAY-09（客の立っているマスに置くと客が動く）は客がいないので M3 に回す
- `JobDef` は欲しいものをコンストラクタで受け取る（欲しがるのは問屋の品だけなので循環しない）
- `BalanceConfig` は項目が多いので public フィールドにした（ほかの SO は `[SerializeField] private` ＋読み取りプロパティ）
- 配置モードのパネルは仮に IMGUI（OnGUI）。uGUI の画面は M4（`BottomMenu` など）で作り、そのときに置き換える
- エディタ拡張のために `Shop.Editor` アセンブリを足した（3.2）

### M3 客と販売（2026-10-26 完了）

| 項目 | 状態 |
|---|---|
| Core | ✓ `Warehouse`、`Shelf`（陳列・補充・戻す）、`Counter`（行列のマス・会計 24 Tick・K-02）、`Customer`、`CustomerFactory`、`CustomerSpawner`、`CustomerBrain`（棚へ・手に取る・しょんぼり・うろうろ・行列・待ちきれない・最長滞在 K-03・向かい直し K-06）、`WeightedPicker`、`AssignShelf`、配置替えのときの客の扱い（6.5）、`ShopState` に売上と星（★1 固定）、客・棚・会計のイベント |
| Game | ✓ `ShelfView`（並ぶ数を商品の色の四角で）、`CustomerView`（職業の色、移動の補間、頭の上の気持ちの色）、`CounterView`（会計のバー）、`FloatingText`（「+100G」）、`Hud`（日付・所持金・売上・客の数・⏸ ▶ ▶▶）。配置モードに棚の商品設定と「デバッグ: 倉庫に各 +10」 |
| テスト | ✓ 78 件すべて成功（UT-SHF-01〜06、UT-CUS-01〜05、UT-BHV-01〜07、UT-CNT-01〜04、UT-LAY-09 ほか） |
| 画面 | ✓ Play で棚を置いて商品を並べると、客が来て買い物・会計し、売上が増える。マウスでの商品設定、客が歩く見た目、しょんぼり、HUD の ⏸ ▶ ▶▶ も手で確かめた（2026-10-26） |

詳細設計からの変更・先送り:

- 評判の増減（UT-BHV の「評判 +2」など）は M5 の `ReputationSystem` で足す。M3 では結果（Full / Partial / None / QueueLeft）までを確かめている
- 月末の集計はまだない。月が変わると来店予定だけ作り直す（M4 で月末処理にする）
- 掘り出し物（特売台の限定品を欲しがる）の仕組みは入れたが、テスト（UT-CUS-06、UT-BHV-08）は行商人と合わせて M6 で書く
- 雰囲気値の来店数への効果（UT-CUS-04）は計算だけ先に入れてテストした
- 棚の商品設定は配置モードの中だけ（時間が止まっている）。時間を止めない `ShelfAssignPanel` は M4
- 倉庫への品の入れ方は、問屋（M4）ができるまで `GameSession.DebugAddStock` と配置モードのデバッグボタン
- 浮き文字と HUD は仮に IMGUI。TextMeshPro のフォントを用意するときに置き換える

### M4 仕入れと時間（2026-10-26 完了）

| 項目 | 状態 |
|---|---|
| Core | ✓ `Wholesaler`（判定順は詳細設計 5.4）、`BuyFromWholesaler`、`MonthlyReport`（売上・仕入れ・設備投資・来店・結果の内訳・限定品の販売数）、月末処理（集計を締めて `MonthEnded` → 翌月の集計と来店予定）、`MonthEndedThisTick` |
| Game | ✓ `GameRunner` が月末の Tick で止まる（`Advance(秒)` に切り出し）、`BottomMenu`（仕入れ / 配置）、配置モードにも「仕入れ（問屋）」ボタン（わかりにくかったため。2026-10-26）、`WholesalerPanel`（+1 / +5 / +10、合計、購入、未解禁はグレー）、`ShelfAssignPanel`（棚をクリックで開く。時間は止めない）、`ModalQueue` と `MonthlyReportDialog`（OK / Enter / Space で閉じて再開）、`ImGuiBlocker`（パネルの上のクリックを店内に通さない） |
| テスト | ✓ 85 件すべて成功（UT-STK-01〜04、UT-END-01 ほか） |
| 画面 | ✓ 仕入れパネル（下のメニューと配置モードの両方から）、棚の設定パネル、月末で止まって月末収支が出て閉じると翌月が始まるところまで手で確かめた（2026-10-26） |

詳細設計からの変更・先送り:

- 星の判定・評判の項目（`RepBefore` / `RepAfter` / `RepToNextStar`）は M5。M4 の月末収支には出さない
- 年末（`YearEnded`）と終了（`GameEnded`）は M7。24 か月目の月末も `MonthEnded` だけ出る
- デバッグ用の「倉庫に各 +10」ボタンは、問屋ができたので外した（`DebugAddStock` はテストのために残す）
- **画面は IMGUI の仮のままにした**。uGUI ＋ TextMeshPro は日本語フォントがないと文字が出ないため、フォント（T-10・M8）を決めてからまとめて置き換える

### M5 評判（2026-10-26 時点）

| 項目 | 状態 |
|---|---|
| Core | ✓ `ReputationSystem`（結果ごとの増減、限定品のボーナス、客ごとに 0 で止める K-12、月末の星の判定、一度に 2 段階）、`ShopState` に評判と `StarReachedMonth`、`StarRaised` と `ReputationChanged` のイベント、月末収支の評判と星の項目、`RepToNextStar` |
| Game | ✓ HUD に星と評判（次の星まで）、月末収支に評判と星の行、星アップの演出（月末収支の OK のあと 2 秒、止めない）、客の吹き出しのセリフ（`CommentTable` と `CommentBubbles`。仮のセリフ） |
| テスト | ✓ 93 件すべて成功（UT-REP-01〜04、結果ごとの評判、UT-BHV-08 ほか） |
| 画面 | △ Play で 1 か月回し、評判が増えて月末で止まるところまで確かめた。**星アップの演出、吹き出しのセリフ、HUD の表示は、まだ手で確かめていない** |

詳細設計からの変更・先送り:

- 月末の処理で、星の判定を集計を締める前にした（集計の「星」は判定のあとの星なので）
- 掘り出し物だけ買えたとき（UT-BHV-08）は M6 の予定だったが、評判のボーナスと一緒に M5 で確かめた
- 星アップを早く確かめるため、下のメニューに「デバッグ: 評判 +○」（`GameSession.DebugAddReputation`。エディタと開発ビルドだけ）を足した。星はふつうと同じく月末に上がる
- 吹き出しのセリフは仮（`Assets/_Project/Data/CommentTable.asset`）。セリフ集（T-13）が決まったらアセットを直す。メニューで作り直しても、既にあるアセットは上書きしない

---

## 変更履歴

| 版 | 日付 | 内容 |
|---|---|---|
| 0.1 | 2026-10-22 | 初版 |
| 0.2 | 2026-10-24 | M1 の実態に合わせて更新（テンプレート、パッケージ、asmdef の置き場所、実装済みクラス）。基本設計への参照を追加。9〜11 章（テストの実行、決まっていないこと、進み具合）を追加 |
| 0.3 | 2026-10-24 | 2 章のフォルダ構成を変更（Unity プロジェクトのルート＝リポジトリのルート、AdventurerShop/ を廃止） |
| 0.4 | 2026-10-24 | M1 完了。MCP の接続方法、Unity AI のアセット生成を使わないこと、MCP 経由のテストの動かし方を追記 |
| 0.5 | 2026-10-24 | M2 の進み具合と詳細設計からの変更を追記。3.2 に `Shop.Editor` を足し、テストの参照に `Shop.Data` を足した（K-09）。9 章に MCP で Play するときの注意 |
| 0.6 | 2026-10-26 | M2 完了。M3 の進み具合と詳細設計からの変更を追記。9 章に MCP で Play するときの注意を足した |
| 0.7 | 2026-10-26 | M3 完了。M4 の進み具合と詳細設計からの変更を追記（画面は IMGUI のまま、フォントを決めてから uGUI に置き換える）。9 章に `GameRunner.Advance` |
| 0.8 | 2026-10-26 | M4 完了。M5 の進み具合と詳細設計からの変更を追記 |
