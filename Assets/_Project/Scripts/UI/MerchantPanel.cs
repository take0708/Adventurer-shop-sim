using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 行商人パネル（SC-07、shousaisekkei.md 11.4）。開いている間は時間を止める（Merchant、D-03）。
    /// 品、欲しがる職業、仕入れ値・売値、1〜2 個、残り時間のゲージ。特売台がなければ注意書き（買えなくはしない）。
    /// 閉じる / Esc / Enter で閉じる。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class MerchantPanel : MonoBehaviour, IGameEventHandler
    {
        const float Width = 420f, Height = 420f;

        [SerializeField] GameRunner _runner;

        string _message = "";

        public bool IsOpen { get; private set; }

        GameSession Session => _runner.Session;

        public void Open()
        {
            if (Session == null || Session.Merchant.State != MerchantState.Present) return;
            IsOpen = true;
            _message = "";
            _runner.Pause(PauseReason.Merchant);
        }

        public void Close()
        {
            IsOpen = false;
            _runner.Resume(PauseReason.Merchant);
        }

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent || e is MerchantLeftEvent) Close();
        }

        void Update()
        {
            if (!IsOpen) return;
            var k = Keyboard.current;
            if (k != null && (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) Close();
        }

        void OnGUI()
        {
            if (!IsOpen || Session == null) return;
            var m = Session.Merchant;
            if (m.State != MerchantState.Present)
            {
                Close();
                return;
            }
            ModalQueue.DimBackground();

            // 行の数は買う前後で変えない（IMGUI は描く途中で行が増えるとずれる）
            var item = m.Item;
            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("行商人が来ています");
            GUILayout.Space(4);
            GUILayout.Label($"{item.Name}（限定品）");
            GUILayout.Label(item.RareJob != null ? $"{item.RareJob.Name}が欲しがる品" : "どの職業も欲しがる品");
            GUILayout.Label($"仕入れ値 {item.BuyPrice}G → 売値 {item.SellPrice}G　評判 +{item.RareRepBonus}（売れたとき）");
            GUILayout.Label($"所持金 {Session.State.Money}G　倉庫 {Session.Warehouse.Count(item)} 個　あと {m.Remaining} 個買える");

            // 残り時間（開いている間は減らない）
            int stay = Session.Config.Balance.MerchantStayTicks;
            float secs = m.TicksLeft * TimeConfig.SecondsPerTick;
            GUILayout.BeginHorizontal();
            var bar = GUILayoutUtility.GetRect(200, 16, GUILayout.Width(200));
            GUI.Box(bar, GUIContent.none);
            GUI.Box(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01((float)m.TicksLeft / stay), bar.height), GUIContent.none);
            GUILayout.Label($"残り {Mathf.CeilToInt(secs)} 秒（開いている間は止まる）");
            GUILayout.EndHorizontal();

            bool hasTable = false;
            foreach (var s in Session.Shelves) if (s.IsBargainTable) hasTable = true;
            GUILayout.Label(hasTable ? "特売台に並べられます" : "※ 特売台がないと並べられません（買った品は倉庫に入ります）");

            GUILayout.Space(4);
            int buy = 0;
            GUILayout.BeginHorizontal();
            for (int n = 1; n <= Session.Config.Balance.MerchantMaxBuy; n++)
            {
                GUI.enabled = n <= m.Remaining && item.BuyPrice * n <= Session.State.Money;
                if (GUILayout.Button($"{n} 個買う（{item.BuyPrice * n}G）", GUILayout.Height(32))) buy = n;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(m.Remaining <= 0 ? "売り切れました" : _message);
            GUILayout.FlexibleSpace();
            bool close = GUILayout.Button("閉じる（Esc / Enter）", GUILayout.Height(32));
            GUILayout.EndArea();

            // 状態を変えるのは描き終えてから
            if (buy > 0)
            {
                var r = Session.BuyFromMerchant(buy);
                _message = r.Ok ? $"{buy} 個買いました" : FailReasonText.Get(r.Reason);
            }
            if (close) Close();
        }
    }
}
