using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 帰る客の頭の上に感想の吹き出しを出す（kihonsekkei.md 5.5、shousaisekkei.md 11.6）。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class CommentBubbles : MonoBehaviour, IGameEventHandler
    {
        sealed class Bubble
        {
            public int CustomerId;
            public string Text;
            public float Left;
            public Vector3 LastPosition;
        }

        [SerializeField] CommentTable _table;
        [SerializeField] CustomerView _customers;
        [SerializeField] Camera _camera;
        [SerializeField] float _seconds = 2f;

        readonly List<Bubble> _bubbles = new List<Bubble>();
        GUIStyle _style;

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent) _bubbles.Clear();
            if (!(e is CustomerLeftEvent l) || _table == null) return;
            var text = _table.Pick(l.Result, l.Bought);
            if (string.IsNullOrEmpty(text)) return;
            _bubbles.Add(new Bubble { CustomerId = l.CustomerId, Text = text, Left = _seconds });
        }

        void Update()
        {
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var b = _bubbles[i];
                b.Left -= Time.unscaledDeltaTime;
                if (_customers.TryGetPosition(b.CustomerId, out var p)) b.LastPosition = p;
                if (b.Left <= 0f) _bubbles.RemoveAt(i);
            }
        }

        void OnGUI()
        {
            if (_bubbles.Count == 0) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { fontSize = 14, wordWrap = false, alignment = TextAnchor.MiddleCenter };
            foreach (var b in _bubbles)
            {
                if (b.LastPosition == Vector3.zero) continue;
                var screen = _camera.WorldToScreenPoint(b.LastPosition + Vector3.up * 0.7f * TilePosition.TileSize);
                var size = _style.CalcSize(new GUIContent(b.Text));
                var rect = new Rect(screen.x - size.x / 2, Screen.height - screen.y - size.y, size.x + 8, size.y + 4);
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(b.Left / 0.4f));
                GUI.Box(rect, b.Text, _style);
                GUI.color = old;
            }
        }
    }
}
