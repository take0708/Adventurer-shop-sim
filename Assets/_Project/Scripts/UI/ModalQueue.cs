using System.Collections.Generic;
using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 月末からのダイアログの順番待ち（shousaisekkei.md 11.5）。列が空でなければ時間を止める（Modal）。
    /// 月末収支 → 星アップ。年末審査・結果発表は M7 で足す。どれも OK / Enter / Space で閉じて次へ。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ModalQueue : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;

        readonly Queue<GameEvent> _queue = new Queue<GameEvent>();

        public bool IsShowing => _queue.Count > 0;
        /// <summary>出すのを待っているダイアログの数（今出しているものを含む）。</summary>
        public int PendingCount => _queue.Count;
        /// <summary>今出しているダイアログのもとのイベント。なければ null。</summary>
        public GameEvent Current => _queue.Count > 0 ? _queue.Peek() : null;

        public void Handle(GameEvent e)
        {
            switch (e)
            {
                case SessionStartedEvent _:
                    _queue.Clear();
                    _runner.Resume(PauseReason.Modal);
                    break;
                case MonthEndedEvent _:
                case StarRaisedEvent _:
                    _queue.Enqueue(e);
                    _runner.Pause(PauseReason.Modal);
                    break;
            }
        }

        void Update()
        {
            if (!IsShowing) return;
            var k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) Next();
        }

        void Next()
        {
            if (_queue.Count > 0) _queue.Dequeue();
            if (_queue.Count == 0) _runner.Resume(PauseReason.Modal);
        }

        void OnGUI()
        {
            if (!IsShowing) return;
            bool ok = false;
            switch (_queue.Peek())
            {
                case MonthEndedEvent m:
                    ok = MonthlyReportDialog.Draw(m.Report, _runner.Session);
                    break;
                case StarRaisedEvent r:
                    ok = StarUpDialog.Draw(r, _runner.Session);
                    break;
            }
            if (ok) Next();
        }

        /// <summary>ダイアログの後ろを暗くし、店内のクリックを通さない。</summary>
        public static void DimBackground()
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
            ImGuiBlocker.Register(new Rect(0, 0, Screen.width, Screen.height));
        }
    }

    /// <summary>
    /// 星アップ（SC-09）。月末収支の OK のあとに出す。OK / Enter / Space で閉じる。
    /// 基本設計では「2 秒、止めない演出」だったが、見落としやすいのでダイアログにした（2026-10-29）。
    /// </summary>
    public static class StarUpDialog
    {
        const float Width = 460f, Height = 260f;
        static GUIStyle _big, _center;

        /// <summary>描いて、OK が押されたら true。</summary>
        public static bool Draw(StarRaisedEvent e, GameSession session)
        {
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _big.normal.textColor = new Color(1f, 0.85f, 0.2f);
                _center = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }
            ModalQueue.DimBackground();

            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label($"★{e.NewStar} になりました！", _big, GUILayout.Height(60));
            GUILayout.Space(6);
            if (e.Unlocked.Length > 0)
            {
                var names = new List<string>();
                foreach (var i in e.Unlocked) names.Add(i.Name);
                GUILayout.Label("仕入れられるようになった品: " + string.Join("、", names), _center);
            }
            else
            {
                GUILayout.Label("新しく仕入れられる品はありません", _center);
            }
            var star = session.Config.StarOf(e.NewStar);
            GUILayout.Label($"来店数と予算が増えます（月 {star.BaseVisits} 人、予算 {star.BudgetMin}〜{star.BudgetMax}G）", _center);
            GUILayout.FlexibleSpace();
            bool ok = GUILayout.Button("OK（Enter / Space）", GUILayout.Height(34));
            GUILayout.EndArea();
            return ok;
        }
    }

    /// <summary>月末収支（SC-08、shousaisekkei.md 11.4）。OK / Enter / Space で閉じる。</summary>
    public static class MonthlyReportDialog
    {
        const float Width = 340f, Height = 380f;

        /// <summary>描いて、OK が押されたら true。</summary>
        public static bool Draw(MonthlyReport r, GameSession session)
        {
            var cfg = session.Config.Time;
            int year = (r.Month - 1) / cfg.MonthsPerYear + 1;
            int month = (r.Month - 1) % cfg.MonthsPerYear + 1;

            ModalQueue.DimBackground();

            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label($"{year} 年目 {month} 月の収支");
            GUILayout.Space(4);
            Row("売上", $"{r.Sales}G");
            Row("仕入れ", $"−{r.Purchases}G");
            Row("設備投資", r.Equipment >= 0 ? $"−{r.Equipment}G" : $"+{-r.Equipment}G");
            Row("収支", $"{(r.Net >= 0 ? "+" : "")}{r.Net}G");
            GUILayout.Space(6);
            Row("来店", $"{r.Visitors} 人");
            Row("満足 / 一部 / 不満", $"{r.Full} / {r.Partial} / {r.None}");
            Row("行列で帰った", $"{r.QueueLeft} 人");
            Row("評判", $"{(r.RepDelta >= 0 ? "+" : "")}{r.RepDelta}（{r.RepAfter}）");
            Row("星", r.RepToNextStar > 0 ? $"★{r.Star}（次の星まで {r.RepToNextStar}）" : $"★{r.Star}（最高）");
            Row("所持金", $"{session.State.Money}G");
            GUILayout.FlexibleSpace();
            bool ok = GUILayout.Button("OK（Enter / Space）", GUILayout.Height(34));
            GUILayout.EndArea();
            return ok;
        }

        static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(160));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }
    }
}
