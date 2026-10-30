using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 画面上の情報（SC-02 上、shousaisekkei.md 11.4）。所持金、日付、月の進み、⏸ ▶ ▶▶。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        const float Width = 460f;
        /// <summary>同梱の日本語フォントは行が高いので、3 行が収まる高さにする。</summary>
        public const float Height = 104f;

        [SerializeField] GameRunner _runner;

        void OnGUI()
        {
            var session = _runner.Session;
            if (session == null) return;
            var clock = session.Clock;

            var area = new Rect(Screen.width - Width - 8, 8, Width, Height);
            ImGuiBlocker.Register(area);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{clock.Year} 年目 {clock.MonthOfYear} 月 {clock.Day} 日", GUILayout.Width(140));
            GUILayout.Label($"所持金 {session.State.Money}G", GUILayout.Width(120));
            GUILayout.Label($"売上 {session.State.TotalSales}G");
            GUILayout.EndHorizontal();

            // 星と評判（評判はその場で増える。星は月末に上がる）
            var state = session.State;
            GUILayout.BeginHorizontal();
            GUILayout.Label(new string('★', state.Star) + new string('☆', session.Config.MaxStar - state.Star), GUILayout.Width(90));
            int next = session.RepToNextStar;
            GUILayout.Label(next > 0
                ? $"評判 {state.Reputation}（次の星まで {next}。月末に判定）"
                : $"評判 {state.Reputation}（最高の星）");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            // 月の進み
            var bar = GUILayoutUtility.GetRect(130, 18, GUILayout.Width(130));
            GUI.Box(bar, GUIContent.none);
            GUI.Box(new Rect(bar.x, bar.y, bar.width * clock.MonthProgress, bar.height), GUIContent.none);
            GUILayout.Label($"客 {session.Customers.Count} 人（今月 {session.PlannedVisits} 人）", GUILayout.Width(160));

            bool userPaused = _runner.IsPausedBy(PauseReason.User);
            if (GUILayout.Toggle(userPaused, "||", GUI.skin.button, GUILayout.Width(30)) != userPaused)
            {
                if (userPaused) _runner.Resume(PauseReason.User);
                else _runner.Pause(PauseReason.User);
            }
            if (GUILayout.Toggle(!userPaused && _runner.Speed == 1, "▶", GUI.skin.button, GUILayout.Width(28)))
            {
                if (userPaused || _runner.Speed != 1) { _runner.Resume(PauseReason.User); _runner.SetSpeed(1); }
            }
            if (GUILayout.Toggle(!userPaused && _runner.Speed == 2, "▶▶", GUI.skin.button, GUILayout.Width(34)))
            {
                if (userPaused || _runner.Speed != 2) { _runner.Resume(PauseReason.User); _runner.SetSpeed(2); }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
