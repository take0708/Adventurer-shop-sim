using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>会計の進み具合のバーをカウンターの上に出す（shousaisekkei.md 11.3）。素材が決まるまでの仮の表示。</summary>
    public sealed class CounterView : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] Color _bar = new Color(1f, 0.9f, 0.3f);

        readonly Dictionary<int, SpriteRenderer> _bars = new Dictionary<int, SpriteRenderer>();

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent || e is LayoutChangedEvent) Rebuild();
        }

        void Rebuild()
        {
            foreach (var b in _bars.Values) Destroy(b.gameObject);
            _bars.Clear();
            foreach (var c in _runner.Session.Counters)
            {
                var bar = PlaceholderSprite.Create(transform, $"Checkout #{c.FixtureId}", _bar, 4);
                bar.enabled = false;
                _bars[c.FixtureId] = bar;
            }
        }

        void Update()
        {
            var session = _runner.Session;
            if (session == null) return;
            int total = session.Config.Balance.CheckoutTicks;
            foreach (var c in session.Counters)
            {
                if (!_bars.TryGetValue(c.FixtureId, out var bar)) continue;
                bar.enabled = c.Serving != null;
                if (!bar.enabled) continue;
                float p = 1f - (float)c.Remaining / total;
                var o = c.Fixture.Origin;
                float s = TilePosition.TileSize;
                bar.transform.position = TilePosition.Origin + new Vector3(o.X + p, o.Y + 0.9f) * s;
                bar.transform.localScale = new Vector3(2f * p, 0.12f, 1f) * s;
            }
        }
    }
}
