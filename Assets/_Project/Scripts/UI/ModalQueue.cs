using System.Collections.Generic;
using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 月末からのダイアログの順番待ち（shousaisekkei.md 11.5）。列が空でなければ時間を止める（Modal）。
    /// 月末収支 → 星アップ。年末審査・結果発表は M7 で足す。
    /// 星アップは止めない演出（2 秒）。列にほかがなければ再開してから演出する。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ModalQueue : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] float _starUpSeconds = 2f;

        readonly Queue<GameEvent> _queue = new Queue<GameEvent>();
        float _effectLeft;

        /// <summary>操作を止めるダイアログを出している（止めない演出だけのときは false）。</summary>
        public bool IsShowing => _queue.Count > 0 && !(_queue.Peek() is StarRaisedEvent);

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
                case StarRaisedEvent _:
                    _queue.Enqueue(e);
                    _runner.Pause(PauseReason.Modal);
                    break;
            }
        }

        void Update()
        {
            if (_queue.Count == 0) return;
            if (_queue.Peek() is StarRaisedEvent)
            {
                _effectLeft -= Time.unscaledDeltaTime;
                if (_effectLeft <= 0f) Next();
                return;
            }
            var k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) Next();
        }

        void Next()
        {
            if (_queue.Count > 0) _queue.Dequeue();
            if (_queue.Count == 0)
            {
                _runner.Resume(PauseReason.Modal);
                return;
            }
            if (_queue.Peek() is StarRaisedEvent)
            {
                _effectLeft = _starUpSeconds;
                // 星アップの演出は止めない。ほかに待っているものがなければ再開してから演出する
                if (_queue.Count == 1) _runner.Resume(PauseReason.Modal);
            }
        }

        void OnGUI()
        {
            if (!IsShowing) return;
            switch (_queue.Peek())
            {
                case MonthEndedEvent m:
                    if (MonthlyReportDialog.Draw(m.Report, _runner.Session)) Next();
                    break;
                case StarRaisedEvent r:
                    StarUpEffect.Draw(r, _runner.Session, 1f - _effectLeft / _starUpSeconds);
                    break;
            }
        }
    }

    /// <summary>星アップの演出（SC-09）。2 秒、止めない。</summary>
    public static class StarUpEffect
    {
        static GUIStyle _big, _small;

        /// <param name="t">0〜1 の進み。</param>
        public static void Draw(StarRaisedEvent e, GameSession session, float t)
        {
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            }
            float alpha = t < 0.8f ? 1f : (1f - t) / 0.2f;
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.35f * alpha);
            GUI.DrawTexture(new Rect(0, Screen.height * 0.3f, Screen.width, Screen.height * 0.4f), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, alpha);

            float y = Screen.height * 0.32f;
            _big.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, y, Screen.width, 60), $"★{e.NewStar} になりました！", _big);
            _small.normal.textColor = Color.white;
            if (e.Unlocked.Length > 0)
            {
                var names = new List<string>();
                foreach (var i in e.Unlocked) names.Add(i.Name);
                GUI.Label(new Rect(0, y + 70, Screen.width, 30), "仕入れられるようになった品: " + string.Join("、", names), _small);
            }
            var star = session.Config.StarOf(e.NewStar);
            GUI.Label(new Rect(0, y + 100, Screen.width, 30), $"来店数と予算が増えます（月 {star.BaseVisits} 人、予算 {star.BudgetMin}〜{star.BudgetMax}G）", _small);
            GUI.color = old;
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
