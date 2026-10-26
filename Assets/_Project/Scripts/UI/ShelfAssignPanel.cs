using System.Collections.Generic;
using Shop.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shop.Game
{
    /// <summary>
    /// 棚の設定パネル（SC-05、shousaisekkei.md 11.4）。時間は止めない。
    /// 配置モードでないときに棚をクリックすると開く。配置モードの中でも同じ内容（DrawItems）を出す。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class ShelfAssignPanel : MonoBehaviour, IGameEventHandler
    {
        const float Width = 240f;

        [SerializeField] GameRunner _runner;
        [SerializeField] Camera _camera;
        [SerializeField] PlacementMode _placement;

        int _shelfId;

        GameSession Session => _runner.Session;

        public void Handle(GameEvent e)
        {
            if (e is LayoutChangedEvent && _shelfId != 0 && Session.ShelfById(_shelfId) == null) _shelfId = 0;
            if (e is SessionStartedEvent) _shelfId = 0;
        }

        void Update()
        {
            if (Session == null || _placement.IsOpen) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 screen = mouse.position.ReadValue();
            if (ImGuiBlocker.Contains(screen)) return;

            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            var f = Session.Layout.FixtureAt(TilePosition.FromWorld(world));
            var shelf = f != null ? Session.ShelfById(f.Id) : null;
            _shelfId = shelf != null ? shelf.FixtureId : 0;
        }

        void OnGUI()
        {
            if (Session == null || _placement.IsOpen || _shelfId == 0) return;
            var shelf = Session.ShelfById(_shelfId);
            if (shelf == null) return;

            var rect = new Rect(8, 80, Width, Screen.height - 150);
            ImGuiBlocker.Register(rect);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label($"{shelf.Def.Name}（#{shelf.FixtureId}）");
            string message = DrawItems(Session, shelf);
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる")) _shelfId = 0;
            GUILayout.EndArea();
        }

        /// <summary>並べる商品の選択肢を描く。操作の結果のメッセージを返す（なければ null）。</summary>
        public static string DrawItems(GameSession session, Shelf shelf)
        {
            string message = null;
            GUILayout.Label($"並べる商品（今: {(shelf.Item != null ? shelf.Item.Name + " " + shelf.Displayed + "/" + shelf.Def.DisplayMax : "なし")}）");
            var items = new List<ItemDef>();
            if (shelf.IsBargainTable) items.AddRange(session.Warehouse.RareItemsInStock);
            else
                foreach (var item in session.UnlockedItems)
                    if (item.Category == shelf.Def.ShelfCategory) items.Add(item);
            if (shelf.Item != null && !items.Contains(shelf.Item)) items.Insert(0, shelf.Item);

            foreach (var item in items)
            {
                bool current = shelf.Item == item;
                if (GUILayout.Toggle(current, $"{item.Name} {item.SellPrice}G（倉庫 {session.Warehouse.Count(item)}）", GUI.skin.button) && !current)
                {
                    var r = session.AssignShelf(shelf.FixtureId, item);
                    message = r.Ok ? $"{item.Name}を並べました" : FailReasonText.Get(r.Reason);
                }
            }
            if (items.Count == 0) GUILayout.Label(shelf.IsBargainTable ? "倉庫に行商人の品がありません" : "並べられる品がありません");
            if (shelf.Item != null && GUILayout.Button("何も置かない")) session.AssignShelf(shelf.FixtureId, null);
            if (shelf.Item != null && session.Warehouse.Count(shelf.Item) == 0 && shelf.Displayed < shelf.Def.DisplayMax)
                GUILayout.Label("倉庫が空です。仕入れましょう");
            return message;
        }
    }
}
