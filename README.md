# 冒険者向けお店経営シミュレーション（仮）

オリジナルのファンタジー世界で、冒険者を相手にお店を経営する、カイロソフトライクな経営シミュレーション。
小さく作って公開することを繰り返し、unityroom → （YouTube Playables）→ Steam の順にリリースを目指す。

**いまの段階**: Step 1（unityroom 公開版）の要件・数値・基本設計がそろい、Unity での実装の土台（M1）を作っている途中。詳しくは [zankadai.md](zankadai.md)。

---

## 文書

| 文書 | 中身 | いつ読むか |
|---|---|---|
| [zankadai.md](zankadai.md) | 進み具合と残課題、次にやること | **最初に読む** |
| [youken.md](youken.md) | 要件定義。**要件と数値の正本**（5 章が数値）。決定履歴もここ | 何を作るかを確かめるとき |
| [kihonsekkei.md](kihonsekkei.md) | 基本設計。月単位の要件を、客が歩き回るゲームの動きにどう置き換えるか。画面・データ・Core と View のやりとり・テスト | 実装するとき |
| [shousaisekkei.md](shousaisekkei.md) | 詳細設計。クラス・メソッド・処理の順番・判定の順番・イベント・テストケース・段階ごとに作るクラス | コードを書くとき |
| [kousei.md](kousei.md) | Unity プロジェクト構成。開発環境、フォルダ、アセンブリ、作る順番（M1〜M8）、M1 の進み具合 | 実装するとき |
| [balance.md](balance.md) | 数値をどういう考えで決めたか。シミュレーション結果と、実ゲームとの違い | 数値を変えるとき |
| [kabeuchi.md](kabeuchi.md) | 最初の壁打ちの記録（2026-10-21）。アイデアの出どころ | 参考 |

文書どうしの関係:

```
kabeuchi（記録）
    │ 整理
    ▼
youken（何を作るか・数値の正本） ←── balance（数値の根拠）＋ tools/balance_sim.py
    │
    ▼
kihonsekkei（どう動かすか） ── kousei（Unity でどう組むか）
    │
    ▼
shousaisekkei（どのクラスで書くか）
    │
    ▼
Assets/（Unity プロジェクト）

zankadai（いまどこか・次に何をするか）
```

## 文書のルール

- 数値の正本は [youken.md 5 章](youken.md#5-数値仕様step-1-初期値)。数値を変えたら youken.md・balance.md・tools/balance_sim.py・ScriptableObject をそろえる
- 決めたことは youken.md の「決定履歴」に日付つきで残す
- 表記: **（確定）** = 相談して決めたもの / **（案）** = 試作で変えてよいもの / **（TBD）** = 未決
- 各文書の末尾に変更履歴を書く

---

## フォルダ

```
C:\dev\Adventurer-shop-sim/     ← Unity プロジェクト（Unity 6000.5.6f1、2D URP）兼リポジトリのルート
├─ README.md / zankadai.md / youken.md / kihonsekkei.md / shousaisekkei.md / kousei.md / balance.md / kabeuchi.md
├─ tools/
│   ├─ balance_sim.py          ← 数値シミュレーション（月単位）
│   └─ run_editmode_tests.sh   ← Unity の EditMode テストをバッチモードで実行
└─ Assets/
    ├─ _Project/               ← 自作のものはすべてここ
    │   └─ Scripts/Core/       ← ゲームのルール（UnityEngine に依存しない C#）
    └─ Tests/EditMode/         ← Core のテスト
```

## 動かし方

数値シミュレーション（Python 3）:

```bash
python tools/balance_sim.py
```

Core のテスト（Unity でプロジェクトを開いていないとき）:

```bash
bash tools/run_editmode_tests.sh
```

Unity エディタからは Window → General → Test Runner → EditMode → Run All。Unity の操作は MCP でつないで行う予定（[kousei.md 10 章](kousei.md#10-決まっていないこと)）。

## 素材・ライセンス

- 再配布できない素材は `Assets/ThirdParty/` に置き、Git には入れない。入手元はこの節に書く（まだなし）
- unityroom のスコア送信用の秘密鍵などは `Assets/_Project/Secrets/` に置き、Git には入れない
