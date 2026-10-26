using System.Collections.Generic;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// IMGUI のパネルが描いた範囲を覚えておき、その上のクリックを店内のクリックとして扱わないようにする。
    /// 仮の IMGUI の UI のため。uGUI に置き換えたら EventSystem の判定にする。
    /// </summary>
    public static class ImGuiBlocker
    {
        static readonly List<(int Frame, Rect Rect)> _rects = new List<(int Frame, Rect Rect)>();

        /// <summary>OnGUI の中で、パネルの範囲（GUI 座標）を登録する。</summary>
        public static void Register(Rect guiRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            _rects.Add((Time.frameCount, guiRect));
        }

        /// <summary>スクリーン座標（左下が原点）が、直近のフレームで描いたパネルの上か。</summary>
        public static bool Contains(Vector2 screen)
        {
            var gui = new Vector2(screen.x, Screen.height - screen.y);
            int frame = Time.frameCount;
            _rects.RemoveAll(r => r.Frame < frame - 1);
            foreach (var r in _rects)
                if (r.Rect.Contains(gui)) return true;
            return false;
        }
    }
}
