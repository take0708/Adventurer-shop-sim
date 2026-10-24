using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>マス ⇔ ワールド座標（shousaisekkei.md 11.3）。1 マスの大きさは素材が決まるまで仮に 1 ユニット。</summary>
    public static class TilePosition
    {
        public static Vector3 Origin = Vector3.zero;
        public static float TileSize = 1f;

        /// <summary>マスの中心。</summary>
        public static Vector3 ToWorld(Tile t) => Origin + new Vector3(t.X + 0.5f, t.Y + 0.5f) * TileSize;

        /// <summary>w×h の配置物を origin に置いたときの中心。</summary>
        public static Vector3 ToWorld(Tile origin, int w, int h) => Origin + new Vector3(origin.X + w * 0.5f, origin.Y + h * 0.5f) * TileSize;

        public static Tile FromWorld(Vector3 p)
        {
            var local = (p - Origin) / TileSize;
            return new Tile(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }
    }

    /// <summary>素材が決まるまでの仮の四角いスプライト。</summary>
    public static class PlaceholderSprite
    {
        static Sprite _square;

        /// <summary>1 ユニット四方の白い四角。SpriteRenderer.color で色をつける。</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point, name = "PlaceholderSquare" };
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return _square;
            }
        }

        public static SpriteRenderer Create(Transform parent, string name, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
