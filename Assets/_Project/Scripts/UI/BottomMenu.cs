using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 画面下のメニュー（SC-02 下、shousaisekkei.md 11.4）: 仕入れ / 配置 / 拡張。行商人がいる間は [行商人 残り n 秒]。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class BottomMenu : MonoBehaviour
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] PlacementMode _placement;
        [SerializeField] WholesalerPanel _wholesaler;
        [SerializeField] ExpandPanel _expand;
        [SerializeField] MerchantPanel _merchant;
        [SerializeField] ModalQueue _modal;

        void OnGUI()
        {
            var session = _runner.Session;
            if (session == null || _placement.IsOpen || _expand.IsOpen || _merchant.IsOpen || (_modal != null && _modal.IsShowing)) return;
            var m = session.Merchant;
            bool merchant = m.State == MerchantState.Present;
            // デバッグ用のボタン（「D:」）はエディタと開発ビルドだけに出す
            bool debug = Debug.isDebugBuild;

            var rect = new Rect(8, Screen.height - 52, Screen.width - 16, 44);
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            if (Button(GUILayout.Toggle(_wholesaler.IsOpen, "仕入れ", GUI.skin.button, GUILayout.Height(40), GUILayout.Width(80)) != _wholesaler.IsOpen))
            {
                if (_wholesaler.IsOpen) _wholesaler.Close();
                else _wholesaler.Open();
            }
            if (Button(GUILayout.Button("配置", GUILayout.Height(40), GUILayout.Width(80))))
            {
                _wholesaler.Close();
                _placement.Open();
            }
            if (Button(GUILayout.Button("拡張", GUILayout.Height(40), GUILayout.Width(80))))
            {
                _wholesaler.Close();
                _expand.Open();
            }
            if (merchant)
            {
                int secs = Mathf.CeilToInt(m.TicksLeft * TimeConfig.SecondsPerTick);
                if (Button(GUILayout.Button($"行商人 残り {secs} 秒", GUILayout.Height(40), GUILayout.Width(150)))) _merchant.Open();
            }
            if (debug)
            {
                if (session.RepToNextStar > 0)
                {
                    // 月末の判定の直前に、次の星に足りない分を足す（月末までに評判が減っても必ず上がる）
                    GUI.enabled = !session.DebugStarUpPending;
                    string label = session.DebugStarUpPending
                        ? $"D: ★{session.State.Star + 1} 予約済み"
                        : $"D: 月末に ★{session.State.Star + 1}";
                    if (Button(GUILayout.Button(label, GUILayout.Height(40), GUILayout.Width(120)))) session.DebugStarUpAtMonthEnd();
                    GUI.enabled = true;
                }
                if (!merchant && Button(GUILayout.Button("D: 行商人", GUILayout.Height(40), GUILayout.Width(90))))
                    session.DebugCallMerchant();
                if (Button(GUILayout.Button("D: +2000G", GUILayout.Height(40), GUILayout.Width(90))))
                    session.DebugAddMoney(2000);
                if (Button(GUILayout.Button("D: 月末へ", GUILayout.Height(40), GUILayout.Width(90))))
                    _runner.DebugSkipToMonthEnd();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>直前に描いたボタンの範囲を、店内のクリックを通さない範囲として登録する。</summary>
        static bool Button(bool pressed)
        {
            var r = GUILayoutUtility.GetLastRect();
            ImGuiBlocker.Register(new Rect(r.x + 8, r.y + Screen.height - 52, r.width, r.height));
            return pressed;
        }
    }
}
