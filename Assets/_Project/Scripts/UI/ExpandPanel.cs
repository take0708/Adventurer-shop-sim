using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 拡張パネル（SC-06、shousaisekkei.md 11.4）。開いている間は時間を止める（Expand）。
    /// 次の段階の広さ・棚とカウンターの上限・費用 → ExpandShop。閉じる / Esc / Enter で閉じる。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ExpandPanel : MonoBehaviour, IGameEventHandler
    {
        const float Width = 420f, Height = 330f;

        [SerializeField] GameRunner _runner;

        string _message = "";

        public bool IsOpen { get; private set; }

        GameSession Session => _runner.Session;

        public void Open()
        {
            IsOpen = true;
            _message = "";
            _runner.Pause(PauseReason.Expand);
        }

        public void Close()
        {
            IsOpen = false;
            _runner.Resume(PauseReason.Expand);
        }

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent) Close();
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
            ModalQueue.DimBackground();

            // 行の数は拡張の前後で変えない（IMGUI は描く途中で行が増えるとずれる）
            var layout = Session.Layout;
            var cfg = Session.Config;
            var now = cfg.SizeOf(layout.Stage);
            bool canGrow = layout.Stage < cfg.MaxStage;
            var next = canGrow ? cfg.SizeOf(layout.Stage + 1) : null;
            bool enough = canGrow && Session.State.Money >= next.Cost;

            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("店の拡張");
            GUILayout.Space(4);
            Row("今", Describe(now));
            Row("次", canGrow ? Describe(next) : "これ以上は広げられません");
            Row("費用", canGrow ? $"{next.Cost}G（所持金 {Session.State.Money}G）" : "—");
            GUILayout.Label("配置物の場所はそのまま。払い戻しはありません");
            GUILayout.Space(4);
            GUI.enabled = enough;
            bool expand = GUILayout.Button(!canGrow ? "最大の広さです" : enough ? "拡張する" : "お金が足りません", GUILayout.Height(32));
            GUI.enabled = true;
            GUILayout.Label(_message);
            GUILayout.FlexibleSpace();
            bool close = GUILayout.Button("閉じる（Esc / Enter）", GUILayout.Height(32));
            GUILayout.EndArea();

            // 状態を変えるのは描き終えてから
            if (expand)
            {
                var r = Session.ExpandShop();
                _message = r.Ok ? $"段階 {layout.Stage} に広げました" : FailReasonText.Get(r.Reason);
            }
            if (close) Close();
        }

        static string Describe(ShopSizeDef s) => $"段階 {s.Stage}　{s.Width}×{s.Height}　棚 {s.ShelfMax}　カウンター {s.CounterMax}";

        static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(50));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }
    }
}
