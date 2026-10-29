using Shop.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shop.Game
{
    /// <summary>
    /// 結果発表（SC-11、kihonsekkei.md 7.5、shousaisekkei.md 9.4）。閉じない。もう一度 / タイトルへ。
    /// 開いたときにスコア（総売上）を 1 回送る。失敗したら「送信できませんでした」と再送ボタン。
    /// ModalQueue が順番が来たら Draw を呼ぶ。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ResultDialog : MonoBehaviour
    {
        const float Width = 440f, Height = 420f;
        public const string TitleScene = "Title";

        [SerializeField] GameRunner _runner;
        [Tooltip("IScoreSubmitter を実装したコンポーネント。なければ送らない")]
        [SerializeField] MonoBehaviour _submitter;

        enum SendState { NotSent, Sending, Sent, Failed, NoSubmitter }

        GameResult _sentFor;
        SendState _send;
        GUIStyle _title, _score;

        public void Draw(GameResult r, GameSession session)
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _score = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _score.normal.textColor = new Color(1f, 0.85f, 0.2f);
            }
            if (_sentFor != r)
            {
                _sentFor = r;
                Send(r);
            }
            ModalQueue.DimBackground();

            var cfg = session.Config;
            var rect = new Rect((Screen.width - Width) / 2, (Screen.height - Height) / 2, Width, Height);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("結果発表", _title, GUILayout.Height(40));
            GUILayout.Label("2 年間の総売上");
            GUILayout.Label($"{r.TotalSales:N0}G", _score, GUILayout.Height(48));
            GUILayout.Space(4);
            Row("最終の星", new string('★', r.FinalStar) + new string('☆', cfg.MaxStar - r.FinalStar) + $"（評判 {r.FinalReputation}）");
            for (int i = 1; i < r.StarReachedMonth.Count; i++)
                Row($"★{i + 1}", MonthText(r.StarReachedMonth[i], cfg.Time));
            foreach (var y in r.Years)
                Row($"{y.Year} 年目", $"売上 {y.Sales:N0}G　評判 +{y.RepGained}");
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label(SendText(), GUILayout.Width(300));
            if (_send == SendState.Failed && GUILayout.Button("再送")) Send(r);
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            bool again = GUILayout.Button("もう一度", GUILayout.Height(36));
            bool title = GUILayout.Button("タイトルへ", GUILayout.Height(36));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 状態を変えるのは描き終えてから
            if (again)
            {
                _sentFor = null;
                _runner.StartNewGame((ulong)System.DateTime.Now.Ticks);
            }
            else if (title)
            {
                SceneManager.LoadScene(TitleScene);
            }
        }

        /// <summary>星に上がった月（その星の客が来始める月）。25 か月目は最後の月末に上がったとき。</summary>
        static string MonthText(int month, TimeConfig time)
        {
            if (month == 0) return "届かなかった";
            if (month > time.TotalMonths) return "最後の月末に上がった";
            int year = (month - 1) / time.MonthsPerYear + 1;
            int m = (month - 1) % time.MonthsPerYear + 1;
            return $"{year} 年目 {m} 月から";
        }

        void Send(GameResult r)
        {
            if (!(_submitter is IScoreSubmitter submitter))
            {
                _send = SendState.NoSubmitter;
                return;
            }
            _send = SendState.Sending;
            submitter.Submit(r.TotalSales, ok =>
            {
                if (_sentFor == r) _send = ok ? SendState.Sent : SendState.Failed;
            });
        }

        string SendText()
        {
            switch (_send)
            {
                case SendState.Sending: return "ランキングに送信中…";
                case SendState.Sent: return "ランキングに送信しました";
                case SendState.Failed: return "送信できませんでした";
                case SendState.NoSubmitter: return "（ランキングの送信はありません）";
                default: return "";
            }
        }

        static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }
    }
}
