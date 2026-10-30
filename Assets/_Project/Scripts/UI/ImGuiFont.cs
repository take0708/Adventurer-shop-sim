using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 仮の IMGUI の画面の文字を、同梱の日本語フォント（M PLUS Rounded 1c）にする。
    /// WebGL には OS のフォントがないので、これがないと日本語が「□」になる（kousei.md 11 章 M8）。
    /// どのパネルより先に OnGUI が呼ばれるよう、実行順を早くしている。uGUI に置き換えたら消す。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ImGuiFont : MonoBehaviour
    {
        [SerializeField] Font _font;
        [Tooltip("0 なら文字の大きさは変えない（パネルの大きさが今の文字に合わせてあるため）")]
        [SerializeField] int _fontSize;

        void OnGUI()
        {
            if (_font == null || GUI.skin.font == _font) return;
            // 既定のスキンを書き換える。各パネルが GUI.skin.label などから作るスタイルにも引き継がれる
            GUI.skin.font = _font;
            if (_fontSize <= 0) return;
            foreach (var style in new[] { GUI.skin.label, GUI.skin.button, GUI.skin.box, GUI.skin.window, GUI.skin.toggle, GUI.skin.textField })
                if (style.fontSize == 0) style.fontSize = _fontSize;
        }
    }
}
