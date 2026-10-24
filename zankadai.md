# 進み具合と残課題

更新日: 2026-10-24

新しいセッションでは、まず [README.md](README.md) と本書を読む。要件・設計の中身は各文書が正本で、本書は「いまどこにいて、次に何をするか」だけを書く。

---

## いまの状況（ひとことで）

冒険者相手のお店を経営する、カイロソフトライクな経営シミュレーション。
**Step 1（unityroom 公開を目指す最小版）の要件・数値・基本設計・詳細設計がそろい、Unity での実装の土台（M1）ができた。次は M2（店内）。**

## 終わったこと

| 日付 | 内容 | 文書 |
|---|---|---|
| 2026-10-21 | 方向性の壁打ち、Step 1 の骨格 | [kabeuchi.md](kabeuchi.md) |
| 2026-10-21 | 要件定義、数値バランスのたたき台とシミュレーション | [youken.md](youken.md), [balance.md](balance.md), [tools/balance_sim.py](tools/balance_sim.py) |
| 2026-10-22 | Unity プロジェクト構成 | [kousei.md](kousei.md) |
| 2026-10-22 | 基本設計（月単位の要件を実時間の動きに置き換える） | [kihonsekkei.md](kihonsekkei.md) |
| 2026-10-24 | 基本設計の決定事項 D-01〜D-06 | [kihonsekkei.md 11 章](kihonsekkei.md#11-決定事項d-01d-06) |
| 2026-10-24 | M1 の途中まで（フォルダ、asmdef、`GameClock` などの Core とテスト、git init） | [kousei.md 11 章](kousei.md#11-進み具合) |
| 2026-10-24 | 文書の整理（README 作成、表の崩れ・食い違いの修正） | — |
| 2026-10-24 | 詳細設計（クラス・処理順・テストケース・段階ごとの実装範囲）。基本設計への補足 K-01〜K-13 | [shousaisekkei.md](shousaisekkei.md) |
| 2026-10-24 | **M1 完了**。Unity で開いて `.meta` などを作成、Unity AI Assistant の MCP でつないだ、EditMode テスト 11 件が全件成功、最初のコミットと GitHub への push | [kousei.md 11 章](kousei.md#11-進み具合) |
| 2026-10-24 | 方針: Unity AI のアセット生成は使わない。M2〜M5 は仮の素材で作る | [kousei.md 1 章](kousei.md#1-開発環境) |

---

## 次にやること

### いますぐ（M2 店内）

範囲は [shousaisekkei.md の段階ごとの実装範囲](shousaisekkei.md)（M2 の行）。

- [ ] `Shop.Tests.EditMode.asmdef` に `Shop.Data` の参照を足し、[kousei.md 3.2](kousei.md#32-アセンブリ定義案) の表も直す（K-09）
- [ ] Core: `Tile`、`CommandResult`、Config 一式、`ConfigValidator`、`ShopLayout`、`PlacementValidator`、`GridPathfinder`、`GameSession`（配置コマンドだけ）
- [ ] Unity 側: `GameDatabase` と各 SO、`ConfigBuilder`、`GameRunner`、`EventRouter`、`GridView`、`FixtureView`、`PlacementMode`（仮の四角）
- [ ] テスト UT-CFG、UT-LAY、UT-PTH を足し、全件成功を確かめる

### そのあと（[kousei.md 8 章](kousei.md#8-作る順番案)）

M3 客と販売 → M4 仕入れと時間 → M5 評判 → M6 行商人・拡張・装飾 → M7 終わり方 → M8 公開準備

---

## 残課題

### 設計まわり

- [x] 詳細設計で基本設計を補った K-01〜K-13 を kihonsekkei.md に反映した（[kihonsekkei.md 11.1](kihonsekkei.md#111-詳細設計で補った事項k-01k-13)、2026-10-24）
- [ ] K-09 に合わせて、M2 で `Shop.Tests.EditMode.asmdef` に `Shop.Data` の参照を足し、[kousei.md 3.2](kousei.md#32-アセンブリ定義案) の表も直す（「次にやること」にも入れた）
- [ ] コミットに使うメールアドレスを GitHub の noreply アドレスに変えるか決める（最初のコミットは gmail のアドレス。[kousei.md 10 章](kousei.md#10-決まっていないこと)）

### 数値まわり

- [ ] [balance_sim.py](tools/balance_sim.py) に、その後の決定（特売台 200G、限定品が倉庫に残る）を入れて影響を確かめる。または実装後の比較テスト（[kihonsekkei.md 10.2](kihonsekkei.md#102-シミュレーションとの比較テスト案)）で確かめる → 違いの一覧は [balance.md](balance.md#シミュレーションと実ゲームの違い)
- [ ] 基本設計で足した数値の（案）を試作で調整し、決まったら youken.md 5 章に移す（棚に並ぶ数、補充間隔、会計 2.4 秒、待てる時間 12 秒、歩く速さ、特売台など）

### Step 1 の公開までに必要（[youken.md 10.3](youken.md#103-優先度-中step-1-の公開までに必要)）

- [ ] T-10 世界観・キャラ（店主、街、ゲームタイトル、ユーモアの方向性）
- [ ] T-11 グラフィック素材の調達（Unity AI の生成は使わない。素材集・自作などで用意する）
- [ ] T-12 商品名・職業名・行商人限定品名の最終決定
- [ ] T-13 客の吹き出しのセリフ集
- [ ] T-14 サウンドの調達

### Step 2 以降（[youken.md 10.4](youken.md#104-優先度-低step-2-以降)）

T-15〜T-21（製作システム、素材、レシピ、値付け、Step 3 の要素、YouTube Playables、Steam）。

---

## 次のセッションでの始め方（例）

> README.md と zankadai.md を読んで、続きをやりたい。
> 次は M2（店内）から進めたい。
