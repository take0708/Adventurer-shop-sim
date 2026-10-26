using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 棚に並んでいる数の表示（shousaisekkei.md 11.3）。素材が決まるまでは、商品の色の小さな四角を並ぶ数だけ出す。
    /// 商品を設定したのに 0 個なら、棚を暗くして空を見せる。
    /// </summary>
    public sealed class ShelfView : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] Color _emptyShade = new Color(0f, 0f, 0f, 0.45f);

        readonly Dictionary<int, Transform> _roots = new Dictionary<int, Transform>();

        public void Handle(GameEvent e)
        {
            switch (e)
            {
                case SessionStartedEvent _:
                case LayoutChangedEvent _:
                    RebuildAll();
                    break;
                case ShelfChangedEvent c:
                    Rebuild(_runner.Session.ShelfById(c.FixtureId));
                    break;
            }
        }

        void RebuildAll()
        {
            foreach (var r in _roots.Values) Destroy(r.gameObject);
            _roots.Clear();
            foreach (var s in _runner.Session.Shelves) Rebuild(s);
        }

        void Rebuild(Shelf shelf)
        {
            if (shelf == null) return;
            if (_roots.TryGetValue(shelf.FixtureId, out var old)) Destroy(old.gameObject);

            var root = new GameObject($"Shelf #{shelf.FixtureId}").transform;
            root.SetParent(transform, false);
            root.position = TilePosition.ToWorld(shelf.Fixture.Origin);
            _roots[shelf.FixtureId] = root;
            if (shelf.Item == null) return;

            if (shelf.Displayed == 0)
            {
                var shade = PlaceholderSprite.Create(root, "Empty", _emptyShade, 3);
                shade.transform.localPosition = Vector3.zero;
                shade.transform.localScale = Vector3.one * 0.88f * TilePosition.TileSize;
                return;
            }

            // 4 個ずつ 2 段に並べる（道具棚は最大 8 個）
            var color = ItemColor.Of(shelf.Item);
            float size = 0.16f * TilePosition.TileSize;
            for (int i = 0; i < shelf.Displayed; i++)
            {
                int col = i % 4, row = i / 4;
                var pip = PlaceholderSprite.Create(root, $"Item {i}", color, 3);
                pip.transform.localPosition = new Vector3((col - 1.5f) * 0.2f, 0.12f - row * 0.24f) * TilePosition.TileSize;
                pip.transform.localScale = Vector3.one * size;
            }
        }
    }

    /// <summary>素材が決まるまでの、商品ごとの仮の色。</summary>
    public static class ItemColor
    {
        public static Color Of(ItemDef item)
        {
            if (item == null) return Color.white;
            if (item.IsRare) return new Color(1f, 0.85f, 0.2f);
            // ID から色相を決める（同じ品はいつも同じ色）
            int h = 0;
            foreach (char ch in item.Id) h = h * 31 + ch;
            float hue = (h & 0xffff) / 65535f;
            return Color.HSVToRGB(hue, 0.55f, 0.95f);
        }
    }
}
