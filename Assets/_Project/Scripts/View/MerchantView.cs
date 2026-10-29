using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 行商人を入口の横に立たせ、頭上に「!」を出す（shousaisekkei.md 11.3）。クリックで行商人パネルを開く。
    /// 素材が決まるまでの仮の表示（四角と IMGUI の文字）。
    /// </summary>
    public sealed class MerchantView : MonoBehaviour, IGameEventHandler
    {
        [SerializeField] GameRunner _runner;
        [SerializeField] Camera _camera;
        [SerializeField] MerchantPanel _panel;
        [SerializeField] PlacementMode _placement;
        [SerializeField] Color _color = new Color(0.55f, 0.3f, 0.7f);

        SpriteRenderer _body;
        GUIStyle _mark;

        GameSession Session => _runner.Session;
        bool Present => Session != null && Session.Merchant.State == MerchantState.Present;

        /// <summary>立つマス。入口の下の道の右隣（店の外）。</summary>
        Tile Spot => new Tile(Session.Layout.Entrance.X + 1, -1);

        public void Handle(GameEvent e)
        {
            if (_body == null)
            {
                _body = PlaceholderSprite.Create(transform, "Merchant", _color, 5);
                _body.enabled = false;
            }
            switch (e)
            {
                case SessionStartedEvent _:
                case MerchantLeftEvent _:
                    _body.enabled = false;
                    break;
                case MerchantArrivedEvent _:
                    _body.transform.position = TilePosition.ToWorld(Spot);
                    _body.transform.localScale = new Vector3(0.7f, 0.9f, 1f) * TilePosition.TileSize;
                    _body.enabled = true;
                    break;
            }
        }

        void Update()
        {
            if (_body != null && !Present) _body.enabled = false;
            if (!Present || _panel.IsOpen || _placement.IsOpen) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 screen = mouse.position.ReadValue();
            if (ImGuiBlocker.Contains(screen)) return;
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            if (TilePosition.FromWorld(world) == Spot) _panel.Open();
        }

        void OnGUI()
        {
            if (!Present || _body == null) return;
            if (_mark == null)
            {
                _mark = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _mark.normal.textColor = new Color(1f, 0.85f, 0.2f);
            }
            var p = _camera.WorldToScreenPoint(_body.transform.position + Vector3.up * 0.8f * TilePosition.TileSize);
            GUI.Label(new Rect(p.x - 40, Screen.height - p.y - 14, 80, 28), "!", _mark);
            GUI.Label(new Rect(p.x - 40, Screen.height - p.y + 36, 80, 20), "行商人", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
    }
}
