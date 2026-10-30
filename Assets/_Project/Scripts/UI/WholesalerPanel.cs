using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 仕入れパネル（SC-03、shousaisekkei.md 11.4）。時間は止めない。
    /// 問屋の品を並べ、未解禁はグレーで「★n で解禁」。+1 / +5 / +10、合計、購入。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class WholesalerPanel : MonoBehaviour, IGameEventHandler
    {
        const float Width = 330f;

        [SerializeField] GameRunner _runner;

        readonly Dictionary<ItemDef, int> _counts = new Dictionary<ItemDef, int>();
        string _message = "";
        Vector2 _scroll;

        public bool IsOpen { get; private set; }

        GameSession Session => _runner.Session;

        public void Open()
        {
            IsOpen = true;
            _message = "";
        }

        public void Close()
        {
            IsOpen = false;
            _counts.Clear();
        }

        public void Handle(GameEvent e)
        {
            if (e is SessionStartedEvent) Close();
        }

        void OnGUI()
        {
            if (!IsOpen || Session == null) return;
            var rect = new Rect(Screen.width - Width - 8, Hud.Height + 16, Width, Screen.height - Hud.Height - 82);
            ImGuiBlocker.Register(rect);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("仕入れ（問屋）");
            GUILayout.Label($"所持金 {Session.State.Money}G");

            int total = 0;
            // 画面が小さい（WebGL は 960×600）と品が収まらないので、品の一覧はスクロールにする
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var item in Session.Config.Items)
            {
                if (item.IsRare) continue;
                bool unlocked = item.UnlockStar <= Session.State.Star;
                _counts.TryGetValue(item, out int n);
                total += n * item.BuyPrice;

                GUI.enabled = unlocked;
                GUILayout.BeginHorizontal();
                GUILayout.Label(unlocked ? $"{item.Name} {item.BuyPrice}→{item.SellPrice}G" : $"{item.Name}（★{item.UnlockStar} で解禁）", GUILayout.Width(150));
                if (unlocked)
                {
                    GUILayout.Label($"倉庫 {Session.Warehouse.Count(item)} 店頭 {OnShelves(item)}", GUILayout.Width(100));
                    GUILayout.Label(n > 0 ? $"×{n}" : "", GUILayout.Width(30));
                }
                GUILayout.EndHorizontal();
                if (unlocked)
                {
                    GUILayout.BeginHorizontal();
                    foreach (int add in new[] { 1, 5, 10 })
                        if (GUILayout.Button($"+{add}")) _counts[item] = n + add;
                    if (GUILayout.Button("0")) _counts.Remove(item);
                    GUILayout.EndHorizontal();
                }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.Label($"合計 {total}G" + (total > Session.State.Money ? "（お金が足りません）" : ""));
            GUI.enabled = total > 0 && total <= Session.State.Money;
            if (GUILayout.Button("購入", GUILayout.Height(32))) Buy();
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる")) Close();
            GUILayout.EndArea();
        }

        void Buy()
        {
            int bought = 0;
            foreach (var item in Session.Config.Items)
            {
                if (!_counts.TryGetValue(item, out int n) || n <= 0) continue;
                var r = Session.BuyFromWholesaler(item, n);
                if (!r.Ok)
                {
                    _message = $"{item.Name}: {FailReasonText.Get(r.Reason)}";
                    return;
                }
                _counts.Remove(item);
                bought += n;
            }
            _message = $"{bought} 個仕入れました";
        }

        int OnShelves(ItemDef item)
        {
            int n = 0;
            foreach (var s in Session.Shelves) if (s.Item == item) n += s.Displayed;
            return n;
        }
    }
}
