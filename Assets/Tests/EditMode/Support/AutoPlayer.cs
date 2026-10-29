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
        const int RepValue = 15;
        /// <summary>月の途中でも、この Tick ごとに仕入れを足す（人も品切れを見て足すため）。</summary>
        const int RestockEveryTicks = 150;

        readonly GameSession _s;
        readonly GameConfig _cfg;

        /// <summary>
        /// 月初に、設備に使う前に取っておく仕入れのお金の割合（%）。調べるときに変える。
        /// 0 が 2 年の結果がいちばんよかった（2026-10-29 の比較。月の途中でも仕入れを足すので、取っておかなくても回る）。
        /// </summary>
        public int ReservePercent { get; set; } = 0;

        /// <summary>カウンター 1 台で受け持つ月の客数（シミュレーションは 25）。調べるときに変える。</summary>
        public int CustomersPerCounter { get; set; } = 25;

        /// <summary>遊んでいる間のイベント（調べるときに使う）。</summary>
        public event Action<GameEvent> EventSeen;

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
                {
                    EventSeen?.Invoke(e);
                    if (e is MerchantArrivedEvent) OnMerchant();
                }
                if (_s.MonthEndedThisTick && !_s.IsEnded) MonthStart();
                else if (!_s.IsEnded && _s.Clock.TickInMonth % RestockEveryTicks == 0) Buy(CurrentPlan());
            }
        }

        void MonthStart()
        {
            // 設備にお金を使う前に、今月の仕入れのぶんを取っておく（シミュレーションは売れたときに仕入れ値を引くので、この心配がない）
            _reserve = StockCost() * ReservePercent / 100;
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

        int _reserve;

        /// <summary>設備に使ってよいお金。</summary>
        int Spare => _s.State.Money - _reserve;

        /// <summary>今の棚の割り当てで、今月の見込みぶんを仕入れるのに要るお金。</summary>
        int StockCost()
        {
            var shelves = NormalShelves.ToList();
            var alloc = Allocate(shelves.Select(s => s.Def));
            var demand = Demand();
            int cost = 0;
            foreach (var g in alloc.Where(a => a.Item != null).GroupBy(a => a.Item))
            {
                int cap = g.Sum(a => MonthlyCap(a.Shelf));
                int need = (int)Math.Min(Math.Ceiling(demand[g.Key]), cap) - _s.Warehouse.Count(g.Key);
                if (need > 0) cost += need * g.Key.BuyPrice;
            }
            return cost;
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
                if (pick == null || Spare < _s.PlacementCost(pick) + ShelfReserve) return;
                if (!PlaceAnywhere(pick)) return;
            }
        }

        void PlaceCounters()
        {
            var counter = _cfg.Fixtures.First(f => f.Kind == FixtureKind.Counter);
            while (_s.Layout.CounterCount * CustomersPerCounter < _s.PlannedVisits
                   && _s.Layout.CounterCount < _s.Layout.Size.CounterMax
                   && Spare >= counter.Cost + CounterReserve)
                if (!PlaceAnywhere(counter)) return;
        }

        void Expand()
        {
            if (_s.Layout.Stage >= _cfg.MaxStage) return;
            bool full = _s.Layout.ShelfCount >= _s.Layout.Size.ShelfMax || _s.Layout.CounterCount >= _s.Layout.Size.CounterMax;
            if (full && Spare >= _s.NextExpansionCost + ExpandReserve) _s.ExpandShop();
        }

        void PlaceDecor()
        {
            if (_s.Layout.Ambience >= _cfg.Balance.AmbienceMax) return;
            foreach (var d in _cfg.Fixtures.Where(f => f.Kind == FixtureKind.Decor).OrderByDescending(f => f.Cost))
            {
                if (Spare < d.Cost + DecorReserve) continue;
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

        List<(Shelf Shelf, ItemDef Item)> CurrentPlan() => NormalShelves.Select(s => (s, s.Item)).ToList();

        /// <summary>
        /// 品ごとに「見込み需要と棚の販売上限の小さいほう」のうち、月の残りのぶんまで倉庫と棚にあるようにする。
        /// お金が足りないときは、1 個ずつ順番に買って、どの品にも行き渡らせる。
        /// </summary>
        void Buy(List<(Shelf Shelf, ItemDef Item)> plan)
        {
            var demand = Demand();
            double rest = 1.0 - (double)_s.Clock.TickInMonth / _cfg.Time.TicksPerMonth;
            var need = new Dictionary<ItemDef, int>();
            foreach (var group in plan.Where(p => p.Item != null && !p.Item.IsRare).GroupBy(p => p.Item))
            {
                var item = group.Key;
                int cap = group.Sum(p => MonthlyCap(p.Shelf.Def));
                int onShelves = _s.Shelves.Where(s => s.Item == item).Sum(s => s.Displayed);
                int n = (int)Math.Ceiling(Math.Min(demand[item], cap) * rest) - _s.Warehouse.Count(item) - onShelves;
                if (n > 0) need[item] = n;
            }
            var order = need.Keys.OrderBy(i => i.BuyPrice).ToList();
            var buy = order.ToDictionary(i => i, i => 0);
            int money = _s.State.Money;
            bool any = true;
            while (any)
            {
                any = false;
                foreach (var item in order)
                    if (buy[item] < need[item] && money >= item.BuyPrice)
                    {
                        buy[item]++;
                        money -= item.BuyPrice;
                        any = true;
                    }
            }
            foreach (var kv in buy)
                if (kv.Value > 0) _s.BuyFromWholesaler(kv.Key, kv.Value);
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
