using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 拡張パネル（SC-06、shousaisekkei.md 11.4）。開いている間は時間を止める（Expand）。
    /// 次の段階の広さ・棚とカウンターの上限・費用 → ExpandShop。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ExpandPanel : MonoBehaviour, IGameEventHandler
    {
        const float Width = 340f, Height = 250f;

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

        void OnGUI()
        {
            if (!IsOpen || Session == null) return;
            ModalQueue.DimBackground();

            var layout = Session.Layout;
            var cfg = Session.Config;
            var now = cfg.SizeOf(layout.Stage);
            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("店の拡張");
            GUILayout.Space(4);
            Row("今", $"段階 {now.Stage}　{now.Width}×{now.Height}　棚 {now.ShelfMax}　カウンター {now.CounterMax}");
            if (layout.Stage < cfg.MaxStage)
            {
                var next = cfg.SizeOf(layout.Stage + 1);
                Row("次", $"段階 {next.Stage}　{next.Width}×{next.Height}　棚 {next.ShelfMax}　カウンター {next.CounterMax}");
                Row("費用", $"{next.Cost}G（所持金 {Session.State.Money}G）");
                GUILayout.Label("配置物の場所はそのまま。払い戻しはありません");
                GUILayout.Space(4);
                GUI.enabled = Session.State.Money >= next.Cost;
                if (GUILayout.Button(Session.State.Money >= next.Cost ? "拡張する" : "お金が足りません", GUILayout.Height(32)))
                {
                    var r = Session.ExpandShop();
                    _message = r.Ok ? $"段階 {layout.Stage} に広げました" : FailReasonText.Get(r.Reason);
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("これ以上は広げられません");
            }
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる", GUILayout.Height(28))) Close();
            GUILayout.EndArea();
        }

        static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(50));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }
    }
}
