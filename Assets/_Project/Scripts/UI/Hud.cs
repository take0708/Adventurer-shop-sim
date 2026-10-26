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
        const float Width = 420f;

        [SerializeField] GameRunner _runner;

        void OnGUI()
        {
            var session = _runner.Session;
            if (session == null) return;
            var clock = session.Clock;

            var area = new Rect(Screen.width - Width - 8, 8, Width, 64);
            ImGuiBlocker.Register(area);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{clock.Year} 年目 {clock.MonthOfYear} 月 {clock.Day} 日", GUILayout.Width(140));
            GUILayout.Label($"所持金 {session.State.Money}G", GUILayout.Width(120));
            GUILayout.Label($"売上 {session.State.TotalSales}G");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            // 月の進み
            var bar = GUILayoutUtility.GetRect(150, 18, GUILayout.Width(150));
            GUI.Box(bar, GUIContent.none);
            GUI.Box(new Rect(bar.x, bar.y, bar.width * clock.MonthProgress, bar.height), GUIContent.none);
            GUILayout.Label($"客 {session.Customers.Count} 人（今月 {session.PlannedVisits} 人来店）", GUILayout.Width(170));

            bool userPaused = _runner.IsPausedBy(PauseReason.User);
            if (GUILayout.Toggle(userPaused, "⏸", GUI.skin.button, GUILayout.Width(28)) != userPaused)
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
