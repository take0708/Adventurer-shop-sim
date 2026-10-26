using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 配置モード（SC-04、shousaisekkei.md 11.4）。開いている間は時間を止める。
    /// マウスのマスごとに CanPlace / CanMove を呼んで緑・赤と理由を出し、クリックで置く。
    /// 置いた物をクリックで選び、移動・撤去する。
    ///
    /// M2 の仮の UI: パネルは IMGUI（OnGUI）で描く。uGUI の画面（BottomMenu など）は M4 で作り、そのときに置き換える。
    /// </summary>
    public sealed class PlacementMode : MonoBehaviour, IGameEventHandler
    {
        enum Tool { None, Place, Move }

        const float PanelWidth = 220f;

        [SerializeField] GameRunner _runner;
        [SerializeField] Camera _camera;
        [SerializeField] Color _ok = new Color(0.3f, 0.9f, 0.3f, 0.55f);
        [SerializeField] Color _ng = new Color(0.95f, 0.25f, 0.25f, 0.55f);
        [SerializeField] Color _selection = new Color(1f, 1f, 1f, 0.35f);

        bool _open;
        Tool _tool;
        FixtureDef _placing;
        int _selectedId;
        string _message = "";
        SpriteRenderer _ghost;
        SpriteRenderer _highlight;
        CommandResult _hoverResult;
        bool _hoverInside;

        GameSession Session => _runner.Session;

        public bool IsOpen => _open;

        public void Handle(GameEvent e)
        {
            // 開始直後は配置モードで始める（kihonsekkei.md 4.5）
            if (e is SessionStartedEvent)
            {
                Open();
                _message = "まずは棚を 3 台置きましょう（無料）";
            }
            else if (e is LayoutChangedEvent && _selectedId != 0 && Session.Layout.FindById(_selectedId) == null)
            {
                Deselect();
            }
        }

        public void Open()
        {
            _open = true;
            _runner.Pause(PauseReason.Placement);
            _message = "";
        }

        public void Close()
        {
            _open = false;
            _tool = Tool.None;
            _placing = null;
            Deselect();
            HideGhost();
            _runner.Resume(PauseReason.Placement);
        }

        void Awake()
        {
            _ghost = PlaceholderSprite.Create(transform, "Ghost", _ok, 10);
            _highlight = PlaceholderSprite.Create(transform, "Selection", _selection, 9);
            HideGhost();
            _highlight.enabled = false;
        }

        void Update()
        {
            if (!_open || Session == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            bool overPanel = screen.x <= PanelWidth + 16f;
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            var tile = TilePosition.FromWorld(world);

            if (mouse.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                CancelTool();
                return;
            }

            UpdateGhost(tile, overPanel);

            if (!mouse.leftButton.wasPressedThisFrame || overPanel) return;

            switch (_tool)
            {
                case Tool.Place:
                {
                    var r = Session.PlaceFixture(_placing, OriginFor(_placing, tile));
                    // 選んだままにして、続けて同じ物を置けるようにする
                    _message = r.Ok ? $"{_placing.Name}を置きました" : FailReasonText.Get(r.Reason);
                    break;
                }
                case Tool.Move:
                {
                    var f = Session.Layout.FindById(_selectedId);
                    var r = Session.MoveFixture(_selectedId, OriginFor(f.Def, tile));
                    _message = r.Ok ? $"{f.Def.Name}を動かしました" : FailReasonText.Get(r.Reason);
                    if (r.Ok) { _tool = Tool.None; HideGhost(); }
                    break;
                }
                default:
                {
                    var f = Session.Layout.FixtureAt(tile);
                    if (f != null) Select(f.Id);
                    else Deselect();
                    break;
                }
            }
            UpdateHighlight();
        }

        /// <summary>マウスのマスを配置物の左下ではなく中心寄りにする（2×2 の絨毯などで置きやすく）。</summary>
        static Tile OriginFor(FixtureDef def, Tile mouse) => mouse.Offset(-(def.Width - 1) / 2, -(def.Height - 1) / 2);

        void UpdateGhost(Tile tile, bool overPanel)
        {
            FixtureDef def = null;
            if (_tool == Tool.Place) def = _placing;
            else if (_tool == Tool.Move) def = Session.Layout.FindById(_selectedId)?.Def;

            if (def == null || overPanel)
            {
                HideGhost();
                return;
            }

            var origin = OriginFor(def, tile);
            _hoverResult = _tool == Tool.Place ? Session.CanPlace(def, origin) : Session.CanMove(_selectedId, origin);
            _hoverInside = true;
            _ghost.enabled = true;
            _ghost.color = _hoverResult.Ok ? _ok : _ng;
            FixtureView.Place(_ghost.transform, def, origin);
        }

        void HideGhost()
        {
            _hoverInside = false;
            if (_ghost != null) _ghost.enabled = false;
        }

        void Select(int id)
        {
            _selectedId = id;
            _tool = Tool.None;
            _placing = null;
            UpdateHighlight();
        }

        void Deselect()
        {
            _selectedId = 0;
            UpdateHighlight();
        }

        void CancelTool()
        {
            _tool = Tool.None;
            _placing = null;
            HideGhost();
            UpdateHighlight();
        }

        void UpdateHighlight()
        {
            if (_highlight == null) return;
            var f = _selectedId != 0 ? Session.Layout.FindById(_selectedId) : null;
            _highlight.enabled = f != null && _tool != Tool.Move;
            if (f != null)
            {
                _highlight.transform.position = TilePosition.ToWorld(f.Origin, f.Def.Width, f.Def.Height);
                _highlight.transform.localScale = new Vector3(f.Def.Width + 0.1f, f.Def.Height + 0.1f, 1f) * TilePosition.TileSize;
            }
        }

        void OnGUI()
        {
            if (Session == null) return;
            GUI.skin.label.wordWrap = true;

            // 閉じているときの「配置」ボタンは Hud が出す
            if (!_open) return;

            GUILayout.BeginArea(new Rect(8, 8, PanelWidth, Screen.height - 16), GUI.skin.box);
            GUILayout.Label("<b>配置モード</b>（時間は止まっています）", new GUIStyle(GUI.skin.label) { richText = true });
            var state = Session.State;
            var layout = Session.Layout;
            GUILayout.Label($"所持金 {state.Money}G　無料枠 残り {state.FreeShelvesLeft}");
            GUILayout.Label($"棚 {layout.ShelfCount}/{layout.Size.ShelfMax}　カウンター {layout.CounterCount}/{layout.Size.CounterMax}　雰囲気 {layout.Ambience}");
            GUILayout.Space(6);

            foreach (var def in Session.Config.Fixtures)
            {
                int cost = Session.PlacementCost(def);
                string label = $"{def.Name}　{(cost == 0 ? "無料" : cost + "G")}";
                bool selected = _tool == Tool.Place && _placing == def;
                if (GUILayout.Toggle(selected, label, GUI.skin.button) && !selected)
                {
                    _tool = Tool.Place;
                    _placing = def;
                    Deselect();
                    _message = "置きたいマスをクリック（右クリックでやめる）";
                }
            }

            GUILayout.Space(6);
            var sel = _selectedId != 0 ? layout.FindById(_selectedId) : null;
            if (sel != null)
            {
                GUILayout.Label($"選択中: {sel.Def.Name}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("移動"))
                {
                    _tool = Tool.Move;
                    _message = "動かす先をクリック（右クリックでやめる）";
                    UpdateHighlight();
                }
                var canRemove = Session.CanRemove(sel.Id);
                GUI.enabled = canRemove.Ok;
                if (GUILayout.Button($"撤去（+{Session.RefundOf(sel.Id)}G）"))
                {
                    var r = Session.RemoveFixture(sel.Id);
                    _message = r.Ok ? $"{sel.Def.Name}を撤去しました" : FailReasonText.Get(r.Reason);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (!canRemove.Ok) GUILayout.Label(FailReasonText.Get(canRemove.Reason));

                var shelf = Session.ShelfById(sel.Id);
                if (shelf != null) ShelfAssignGui(shelf);
            }
            else
            {
                GUILayout.Label("置いた物をクリックすると、移動・撤去できます");
            }

            GUILayout.Space(6);
            if (_hoverInside && !_hoverResult.Ok) GUILayout.Label($"<color=#ff8080>{FailReasonText.Get(_hoverResult.Reason)}</color>", new GUIStyle(GUI.skin.label) { richText = true });
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);

            GUILayout.FlexibleSpace();
            // M3 の仮: 問屋（M4）ができるまで、倉庫に品を直接入れる
            if (GUILayout.Button("デバッグ: 倉庫に各 +10"))
            {
                foreach (var item in Session.UnlockedItems) Session.DebugAddStock(item, 10);
                _message = "倉庫に解禁済みの品を 10 個ずつ入れました";
            }
            if (GUILayout.Button("完了", GUILayout.Height(36))) Close();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 棚に並べる商品を選ぶ（SC-05 の代わり）。
        /// M3 の仮: 配置モードの中だけで設定できる。ShelfAssignPanel（時間を止めない）は M4 で作る。
        /// </summary>
        void ShelfAssignGui(Shelf shelf)
        {
            GUILayout.Label($"並べる商品（今: {(shelf.Item != null ? shelf.Item.Name + " " + shelf.Displayed + "/" + shelf.Def.DisplayMax : "なし")}）");
            var items = new System.Collections.Generic.List<ItemDef>();
            if (shelf.IsBargainTable) items.AddRange(Session.Warehouse.RareItemsInStock);
            else
                foreach (var item in Session.UnlockedItems)
                    if (item.Category == shelf.Def.ShelfCategory) items.Add(item);

            foreach (var item in items)
            {
                bool current = shelf.Item == item;
                if (GUILayout.Toggle(current, $"{item.Name}（倉庫 {Session.Warehouse.Count(item)}）", GUI.skin.button) && !current)
                {
                    var r = Session.AssignShelf(shelf.FixtureId, item);
                    _message = r.Ok ? $"{item.Name}を並べました" : FailReasonText.Get(r.Reason);
                }
            }
            if (items.Count == 0) GUILayout.Label(shelf.IsBargainTable ? "倉庫に行商人の品がありません" : "並べられる品がありません");
            if (shelf.Item != null && GUILayout.Button("何も置かない")) Session.AssignShelf(shelf.FixtureId, null);
        }
    }
}
