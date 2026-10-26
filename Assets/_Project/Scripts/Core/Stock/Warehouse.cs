using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>倉庫の在庫（kihonsekkei.md 6.1、shousaisekkei.md 5.3）。容量は無制限。</summary>
    public sealed class Warehouse
    {
        readonly GameConfig _config;
        readonly GameSession _session;
        readonly int[] _counts;

        internal Warehouse(GameConfig config, GameSession session)
        {
            _config = config;
            _session = session;
            _counts = new int[config.Items.Count];
        }

        public int Count(ItemDef item) => _counts[IndexOf(item)];

        /// <summary>Config.Items の順。0 個も含む。</summary>
        public IReadOnlyList<(ItemDef Item, int Count)> Entries
        {
            get
            {
                var list = new List<(ItemDef Item, int Count)>(_counts.Length);
                for (int i = 0; i < _counts.Length; i++) list.Add((_config.Items[i], _counts[i]));
                return list;
            }
        }

        /// <summary>倉庫にある限定品（特売台の設定パネル用）。</summary>
        public IEnumerable<ItemDef> RareItemsInStock
        {
            get
            {
                for (int i = 0; i < _counts.Length; i++)
                    if (_config.Items[i].IsRare && _counts[i] > 0) yield return _config.Items[i];
            }
        }

        internal void Add(ItemDef item, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            int i = IndexOf(item);
            _counts[i] += count;
            _session.Emit(new WarehouseChangedEvent(_session.Now, item, _counts[i]));
        }

        /// <summary>1 個出す。なければ false。</summary>
        internal bool TryTake(ItemDef item)
        {
            int i = IndexOf(item);
            if (_counts[i] <= 0) return false;
            _counts[i]--;
            _session.Emit(new WarehouseChangedEvent(_session.Now, item, _counts[i]));
            return true;
        }

        int IndexOf(ItemDef item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            for (int i = 0; i < _config.Items.Count; i++) if (_config.Items[i] == item) return i;
            throw new ArgumentException($"{item.Id} は設定にない商品", nameof(item));
        }
    }
}
