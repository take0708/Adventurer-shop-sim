using System.Collections.Generic;
using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 月末からのダイアログの順番待ち（shousaisekkei.md 11.5）。列が空でなければ時間を止める（Modal）。
    /// M4 では月末収支だけ。星アップ（M5）、年末審査・結果発表（M7）をあとで足す。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ModalQueue : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;

        readonly Queue<GameEvent> _queue = new Queue<GameEvent>();

        public bool IsShowing => _queue.Count > 0;

        public void Handle(GameEvent e)
        {
            switch (e)
            {
                case SessionStartedEvent _:
                    _queue.Clear();
                    _runner.Resume(PauseReason.Modal);
                    break;
                case MonthEndedEvent _:
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
            switch (_queue.Peek())
            {
                case MonthEndedEvent m:
                    if (MonthlyReportDialog.Draw(m.Report, _runner.Session)) Next();
                    break;
            }
        }
    }

    /// <summary>月末収支（SC-08、shousaisekkei.md 11.4）。OK / Enter / Space で閉じる。</summary>
    public static class MonthlyReportDialog
    {
        const float Width = 340f, Height = 330f;

        /// <summary>描いて、OK が押されたら true。</summary>
        public static bool Draw(MonthlyReport r, GameSession session)
        {
            var cfg = session.Config.Time;
            int year = (r.Month - 1) / cfg.MonthsPerYear + 1;
            int month = (r.Month - 1) % cfg.MonthsPerYear + 1;

            // 後ろを暗くする
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;

            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            ImGuiBlocker.Register(new Rect(0, 0, Screen.width, Screen.height));
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
