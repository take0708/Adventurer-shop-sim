using System;
using System.Collections.Generic;
using System.Linq;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// Core のコマンドだけで遊ぶ自動の店主（shousaisekkei.md 13.2）。balance_sim.py の run() と同じ方針。
    /// シミュレーションの choose_display は Core の乱数を引いてしまうので使わず、期待値で棚を割り当てる。
    /// </summary>
    public sealed class AutoPlayer
    {
        const int ShelfReserve = 150, CounterReserve = 150, ExpandReserve = 400, DecorReserve = 800;
        const int CustomersPerCounter = 25, RepValue = 15;

        readonly GameSession _s;
        readonly GameConfig _cfg;

        public AutoPlayer(GameSession session)
        {
            _s = session;
            _cfg = session.Config;
        }

        /// <summary>24 か月遊ぶ。</summary>
        public void Play()
        {
            MonthStart();
            while (!_s.IsEnded)
            {
                _s.Tick();
                foreach (var e in _s.DrainEvents())
                    if (e is MerchantArrivedEvent) OnMerchant();
                if (_s.MonthEndedThisTick && !_s.IsEnded) MonthStart();
            }
        }

        void MonthStart()
        {
            PlaceShelves();
            PlaceCounters();
            Expand();
            PlaceDecor();
            var plan = AssignShelves();
            Buy(plan);
        }

        // ---- 見込み ----

        /// <summary>品ごとの今月の見込み需要（来店数 × 職業ごとの欲しがる確率の平均）。</summary>
        Dictionary<ItemDef, double> Demand()
        {
            var demand = new Dictionary<ItemDef, double>();
            double wantsPerCustomer = 1 + _cfg.Balance.TwoWantsChancePercent / 100.0;
            foreach (var job in _cfg.Jobs)
            {
                var unlocked = job.Wants.Where(w => !w.Item.IsRare && w.Item.UnlockStar <= _s.State.Star).ToList();
                double sum = unlocked.Sum(w => w.Weight);
                foreach (var (item, weight) in unlocked)
                {
                    demand.TryGetValue(item, out double d);
                    demand[item] = d + _s.PlannedVisits * wantsPerCustomer * weight / sum / _cfg.Jobs.Count;
                }
            }
            return demand;
        }

        int MonthlyCap(FixtureDef shelf) => _cfg.Time.TicksPerMonth / shelf.RestockTicks;

        double Value(ItemDef item) => item.SellPrice - item.BuyPrice + RepValue * _cfg.Balance.RepFull / 2.0;

        /// <summary>棚の系統の並びに、利益＋評判の見込みが大きくなるよう品を割り当てる（貪欲）。</summary>
        List<(FixtureDef Shelf, ItemDef Item, double Gain)> Allocate(IEnumerable<FixtureDef> shelves)
        {
            var left = Demand();
            var result = new List<(FixtureDef, ItemDef, double)>();
            foreach (var shelf in shelves)
            {
                ItemDef best = null;
                double bestGain = 0;
                foreach (var kv in left)
                {
                    if (kv.Key.Category != shelf.ShelfCategory) continue;
                    double gain = Math.Min(kv.Value, MonthlyCap(shelf)) * Value(kv.Key);
                    if (gain > bestGain) { best = kv.Key; bestGain = gain; }
                }
                if (best != null) left[best] = Math.Max(0, left[best] - MonthlyCap(shelf));
                result.Add((shelf, best, bestGain));
            }
            return result;
        }

        IEnumerable<Shelf> NormalShelves => _s.Shelves.Where(s => !s.IsBargainTable);

        // ---- 1〜4: 置く・広げる ----

        void PlaceShelves()
        {
            var types = _cfg.Fixtures.Where(f => f.Kind == FixtureKind.Shelf && !f.IsBargainTable).ToList();
            while (_s.Layout.ShelfCount < _s.Layout.Size.ShelfMax)
            {
                var current = NormalShelves.Select(s => s.Def).ToList();
                double baseGain = Allocate(current).Sum(a => a.Gain);
                FixtureDef pick = null;
                double pickGain = 0;
                foreach (var t in types)
                {
                    double gain = Allocate(current.Append(t)).Sum(a => a.Gain) - baseGain;
                    if (gain > pickGain) { pick = t; pickGain = gain; }
                }
                if (pick == null || _s.State.Money < _s.PlacementCost(pick) + ShelfReserve) return;
                if (!PlaceAnywhere(pick)) return;
            }
        }

        void PlaceCounters()
        {
            var counter = _cfg.Fixtures.First(f => f.Kind == FixtureKind.Counter);
            while (_s.Layout.CounterCount * CustomersPerCounter < _s.PlannedVisits
                   && _s.Layout.CounterCount < _s.Layout.Size.CounterMax
                   && _s.State.Money >= counter.Cost + CounterReserve)
                if (!PlaceAnywhere(counter)) return;
        }

        void Expand()
        {
            if (_s.Layout.Stage >= _cfg.MaxStage) return;
            bool full = _s.Layout.ShelfCount >= _s.Layout.Size.ShelfMax || _s.Layout.CounterCount >= _s.Layout.Size.CounterMax;
            if (full && _s.State.Money >= _s.NextExpansionCost + ExpandReserve) _s.ExpandShop();
        }

        void PlaceDecor()
        {
            if (_s.Layout.Ambience >= _cfg.Balance.AmbienceMax) return;
            foreach (var d in _cfg.Fixtures.Where(f => f.Kind == FixtureKind.Decor).OrderByDescending(f => f.Cost))
            {
                if (_s.State.Money < d.Cost + DecorReserve) continue;
                if (PlaceAnywhere(d)) return;
            }
        }

        /// <summary>上の行から左→右に見て、最初に置けるマスに置く。</summary>
        bool PlaceAnywhere(FixtureDef def)
        {
            for (int y = _s.Layout.Height - 1; y >= 0; y--)
                for (int x = 0; x < _s.Layout.Width; x++)
                {
                    var t = new Tile(x, y);
                    if (_s.CanPlace(def, t).Ok) return _s.PlaceFixture(def, t).Ok;
                }
            return false;
        }

        // ---- 5〜6: 棚と仕入れ ----

        List<(Shelf Shelf, ItemDef Item)> AssignShelves()
        {
            var shelves = NormalShelves.ToList();
            var alloc = Allocate(shelves.Select(s => s.Def));
            var plan = new List<(Shelf, ItemDef)>();
            for (int i = 0; i < shelves.Count; i++)
            {
                var item = alloc[i].Item;
                if (item != null && shelves[i].Item != item) _s.AssignShelf(shelves[i].FixtureId, item);
                plan.Add((shelves[i], item));
            }
            return plan;
        }

        void Buy(List<(Shelf Shelf, ItemDef Item)> plan)
        {
            var demand = Demand();
            foreach (var group in plan.Where(p => p.Item != null).GroupBy(p => p.Item))
            {
                var item = group.Key;
                int cap = group.Sum(p => MonthlyCap(p.Shelf.Def));
                int onShelves = _s.Shelves.Where(s => s.Item == item).Sum(s => s.Displayed);
                int need = (int)Math.Min(Math.Ceiling(demand[item]), cap) - _s.Warehouse.Count(item) - onShelves;
                int n = Math.Min(need, _s.State.Money / item.BuyPrice);
                if (n > 0) _s.BuyFromWholesaler(item, n);
            }
        }

        // ---- 行商人 ----

        void OnMerchant()
        {
            var m = _s.Merchant;
            int n = Math.Min(m.Remaining, _s.State.Money / m.Item.BuyPrice);
            if (n <= 0) return;
            _s.BuyFromMerchant(n);

            var table = _s.Shelves.FirstOrDefault(s => s.IsBargainTable);
            var def = _cfg.Fixtures.First(f => f.IsBargainTable);
            if (table == null && _s.State.Money >= def.Cost && PlaceAnywhere(def))
                table = _s.Shelves.FirstOrDefault(s => s.IsBargainTable);
            if (table != null && (table.Item == null || table.Displayed == 0 && _s.Warehouse.Count(table.Item) == 0))
                _s.AssignShelf(table.FixtureId, m.Item);
        }
    }
}
