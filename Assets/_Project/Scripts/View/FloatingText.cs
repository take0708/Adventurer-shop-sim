using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 「+100G」などの浮き文字（shousaisekkei.md 11.3）。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class FloatingText : MonoBehaviour, IGameEventHandler
    {
        sealed class Entry
        {
            public Vector3 World;
            public string Text;
            public Color Color;
            public float Age;
        }

        [SerializeField] GameRunner _runner;
        [SerializeField] Camera _camera;
        [SerializeField] float _seconds = 1.2f;
        [SerializeField] float _rise = 0.8f;

        readonly List<Entry> _entries = new List<Entry>();
        GUIStyle _style;

        public void Handle(GameEvent e)
        {
            if (e is CheckoutDoneEvent c && c.Amount > 0)
            {
                var counter = _runner.Session.CounterById(c.FixtureId);
                var at = counter != null ? TilePosition.ToWorld(counter.Fixture.Origin, 2, 1) : Vector3.zero;
                _entries.Add(new Entry { World = at + Vector3.up * 0.5f, Text = $"+{c.Amount}G", Color = new Color(1f, 0.9f, 0.2f) });
            }
        }

        void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                _entries[i].Age += Time.unscaledDeltaTime;
                if (_entries[i].Age >= _seconds) _entries.RemoveAt(i);
            }
        }

        void OnGUI()
        {
            if (_entries.Count == 0) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var e in _entries)
            {
                float t = e.Age / _seconds;
                var screen = _camera.WorldToScreenPoint(e.World + Vector3.up * _rise * t * TilePosition.TileSize);
                var rect = new Rect(screen.x - 60, Screen.height - screen.y - 15, 120, 30);
                var color = e.Color;
                color.a = 1f - t;
                _style.normal.textColor = new Color(0, 0, 0, color.a * 0.7f);
                GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), e.Text, _style);
                _style.normal.textColor = color;
                GUI.Label(rect, e.Text, _style);
            }
        }
    }
}
