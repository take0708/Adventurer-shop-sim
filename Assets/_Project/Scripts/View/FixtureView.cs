using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 配置物の表示（shousaisekkei.md 11.3）。LayoutChanged で全部描き直す。
    /// 素材が決まるまでは FixtureData の仮の色の四角。
    /// </summary>
    public sealed class FixtureView : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;

        readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent || e is LayoutChangedEvent) Rebuild();
        }

        void Rebuild()
        {
            foreach (var v in _views.Values) Destroy(v.gameObject);
            _views.Clear();

            var db = _runner.Database;
            foreach (var f in _runner.Session.Layout.Fixtures)
            {
                var data = db.FixtureOf(f.Def.Id);
                var color = data != null ? data.PlaceholderColor : Color.magenta;
                // 絨毯は床の上、ほかはその上
                var sr = PlaceholderSprite.Create(transform, $"#{f.Id} {f.Def.Id}", color, f.Def.Walkable ? 1 : 2);
                if (data != null && data.Sprite != null) { sr.sprite = data.Sprite; sr.color = Color.white; }
                Place(sr.transform, f.Def, f.Origin);
                _views[f.Id] = sr;
            }
        }

        /// <summary>配置物の見た目を origin に合わせる。少し小さくしてマスの境目を見せる。</summary>
        public static void Place(Transform t, FixtureDef def, Tile origin)
        {
            float inset = def.Walkable ? 0.05f : 0.12f;
            t.position = TilePosition.ToWorld(origin, def.Width, def.Height);
            t.localScale = new Vector3(def.Width - inset, def.Height - inset, 1f) * TilePosition.TileSize;
        }
    }
}
