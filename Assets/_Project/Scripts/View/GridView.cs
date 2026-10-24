using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>床のマス、壁、入口、街からの道（shousaisekkei.md 11.3）。素材が決まるまでは色つきの四角。</summary>
    public sealed class GridView : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] Camera _camera;
        [SerializeField] Color _floorA = new Color(0.86f, 0.78f, 0.62f);
        [SerializeField] Color _floorB = new Color(0.82f, 0.73f, 0.57f);
        [SerializeField] Color _wall = new Color(0.4f, 0.3f, 0.22f);
        [SerializeField] Color _entrance = new Color(0.95f, 0.9f, 0.55f);
        [SerializeField] Color _road = new Color(0.6f, 0.6f, 0.55f);
        [Tooltip("画面の左に配置モードのパネルがあるぶん、店を右に寄せる（ユニット）")]
        [SerializeField] float _leftMargin = 3f;

        int _builtStage;

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent || e is ShopExpandedEvent) Rebuild();
        }

        void Rebuild()
        {
            var layout = _runner.Session.Layout;
            if (_builtStage == layout.Stage && transform.childCount > 0) return;
            _builtStage = layout.Stage;

            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                {
                    var t = new Tile(x, y);
                    var color = t == layout.Entrance ? _entrance : ((x + y) % 2 == 0 ? _floorA : _floorB);
                    var sr = PlaceholderSprite.Create(transform, $"Floor {t}", color, 0);
                    sr.transform.position = TilePosition.ToWorld(t);
                    sr.transform.localScale = Vector3.one * TilePosition.TileSize;
                }

            // 壁（左・右・上）。入口のある下は開けて、入口の下に道を描く
            float s = TilePosition.TileSize;
            AddBar("Wall Left", new Vector3(-0.15f, layout.Height * 0.5f), new Vector3(0.3f, layout.Height + 0.3f));
            AddBar("Wall Right", new Vector3(layout.Width + 0.15f, layout.Height * 0.5f), new Vector3(0.3f, layout.Height + 0.3f));
            AddBar("Wall Top", new Vector3(layout.Width * 0.5f, layout.Height + 0.15f), new Vector3(layout.Width + 0.6f, 0.3f));
            var road = PlaceholderSprite.Create(transform, "Road", _road, 0);
            road.transform.position = TilePosition.Origin + new Vector3(layout.Entrance.X + 0.5f, -1.5f) * s;
            road.transform.localScale = new Vector3(1f, 3f) * s;

            FitCamera(layout);
        }

        void AddBar(string name, Vector3 center, Vector3 size)
        {
            var sr = PlaceholderSprite.Create(transform, name, _wall, 0);
            sr.transform.position = TilePosition.Origin + center * TilePosition.TileSize;
            sr.transform.localScale = size * TilePosition.TileSize;
        }

        void FitCamera(ShopLayout layout)
        {
            if (_camera == null) return;
            float s = TilePosition.TileSize;
            // 下に道の 2 マス、上と左右に 1 マスの余白
            float worldW = (layout.Width + 2 + _leftMargin) * s;
            float worldH = (layout.Height + 3) * s;
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(worldH * 0.5f, worldW * 0.5f / _camera.aspect);
            var center = TilePosition.Origin + new Vector3(layout.Width * 0.5f - _leftMargin * 0.5f, layout.Height * 0.5f - 1f) * s;
            _camera.transform.position = new Vector3(center.x, center.y, -10f);
        }
    }
}
