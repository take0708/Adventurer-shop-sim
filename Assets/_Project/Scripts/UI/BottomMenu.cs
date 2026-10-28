using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 画面下のメニュー（SC-02 下、shousaisekkei.md 11.4）: 仕入れ / 配置。拡張と行商人は M6 で足す。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class BottomMenu : MonoBehaviour
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] PlacementMode _placement;
        [SerializeField] WholesalerPanel _wholesaler;
        [SerializeField] ModalQueue _modal;

        void OnGUI()
        {
            var session = _runner.Session;
            if (session == null || _placement.IsOpen || (_modal != null && _modal.IsShowing)) return;
            // デバッグ用のボタンはエディタと開発ビルドだけに出す
            bool debug = Debug.isDebugBuild && session.RepToNextStar > 0;
            var rect = new Rect(8, Screen.height - 52, debug ? 460 : 260, 44);
            ImGuiBlocker.Register(rect);
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_wholesaler.IsOpen, "仕入れ", GUI.skin.button, GUILayout.Height(40)) != _wholesaler.IsOpen)
            {
                if (_wholesaler.IsOpen) _wholesaler.Close();
                else _wholesaler.Open();
            }
            if (GUILayout.Button("配置", GUILayout.Height(40)))
            {
                _wholesaler.Close();
                _placement.Open();
            }
            if (debug)
            {
                // 月末の判定の直前に、次の星に足りない分を足す（月末までに評判が減っても必ず上がる）
                GUI.enabled = !session.DebugStarUpPending;
                string label = session.DebugStarUpPending
                    ? $"デバッグ: 月末に ★{session.State.Star + 1}\n（予約済み）"
                    : $"デバッグ: 月末に ★{session.State.Star + 1} へ";
                if (GUILayout.Button(label, GUILayout.Height(40), GUILayout.Width(190))) session.DebugStarUpAtMonthEnd();
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
