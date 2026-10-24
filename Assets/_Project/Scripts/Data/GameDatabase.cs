using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>数値のアセットをまとめて持つ 1 つのアセット（shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        public const string DefaultPath = "Assets/_Project/Data/GameDatabase.asset";

        [SerializeField] List<ItemData> _items = new List<ItemData>();
        [SerializeField] List<RareItemData> _rareItems = new List<RareItemData>();
        [SerializeField] List<JobData> _jobs = new List<JobData>();
        [SerializeField] StarTable _starTable;
        [SerializeField] List<FixtureData> _fixtures = new List<FixtureData>();
        [SerializeField] ShopSizeTable _sizes;
        [SerializeField] BalanceConfig _balance;

        public IReadOnlyList<ItemData> Items => _items;
        public IReadOnlyList<RareItemData> RareItems => _rareItems;
        public IReadOnlyList<JobData> Jobs => _jobs;
        public StarTable StarTable => _starTable;
        public IReadOnlyList<FixtureData> Fixtures => _fixtures;
        public ShopSizeTable Sizes => _sizes;
        public BalanceConfig Balance => _balance;

        /// <summary>ID から配置物のアセットを引く（View が見た目を取るため）。</summary>
        public FixtureData FixtureOf(string id)
        {
            foreach (var f in _fixtures) if (f != null && f.Id == id) return f;
            return null;
        }

        /// <summary>ID から商品のアイコンを引く。</summary>
        public Sprite IconOf(string id)
        {
            foreach (var i in _items) if (i != null && i.Id == id) return i.Icon;
            foreach (var r in _rareItems) if (r != null && r.Id == id) return r.Icon;
            return null;
        }

#if UNITY_EDITOR
        public void EditorSet(List<ItemData> items, List<RareItemData> rareItems, List<JobData> jobs, StarTable starTable,
            List<FixtureData> fixtures, ShopSizeTable sizes, BalanceConfig balance)
        {
            _items = items; _rareItems = rareItems; _jobs = jobs; _starTable = starTable;
            _fixtures = fixtures; _sizes = sizes; _balance = balance;
        }
#endif
    }
}
