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
            if (_runner.Session == null || _placement.IsOpen || (_modal != null && _modal.IsShowing)) return;
            var rect = new Rect(8, Screen.height - 52, 260, 44);
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
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
