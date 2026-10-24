using System;
using System.Collections.Generic;
using Shop.Core;

namespace Shop.Data
{
    /// <summary>GameDatabase → GameConfig（shousaisekkei.md 11.1）。見た目（Sprite など）は Core に渡さない。</summary>
    public static class ConfigBuilder
    {
        public static GameConfig Build(GameDatabase db)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            var b = db.Balance ?? throw new InvalidOperationException("GameDatabase に BalanceConfig がない");

            var time = new TimeConfig(b.ticksPerDay, b.daysPerMonth, b.monthsPerYear, b.totalMonths);

            var items = new List<ItemDef>();
            var itemMap = new Dictionary<ItemData, ItemDef>();
            foreach (var d in db.Items)
            {
                var def = new ItemDef(d.Id, d.DisplayName, d.Category, d.BuyPrice, d.SellPrice, d.UnlockStar);
                items.Add(def);
                itemMap[d] = def;
            }

            var jobs = new List<JobDef>();
            var jobMap = new Dictionary<JobData, JobDef>();
            foreach (var d in db.Jobs)
            {
                var wants = new List<(ItemDef Item, int Weight)>();
                foreach (var w in d.Wants)
                {
                    if (w.item == null || w.weight <= 0) continue;
                    if (!itemMap.TryGetValue(w.item, out var item))
                        throw new InvalidOperationException($"{d.Id} の欲しいもの {w.item.Id} が GameDatabase の商品にない");
                    wants.Add((item, w.weight));
                }
                var def = new JobDef(d.Id, d.DisplayName, wants);
                jobs.Add(def);
                jobMap[d] = def;
            }

            foreach (var d in db.RareItems)
            {
                JobDef job = null;
                if (d.Job != null && !jobMap.TryGetValue(d.Job, out job))
                    throw new InvalidOperationException($"{d.Id} の職業 {d.Job.Id} が GameDatabase の職業にない");
                items.Add(new ItemDef(d.Id, d.DisplayName, ItemCategory.Rare, d.BuyPrice, d.SellPrice, d.AppearStar, job, d.RepBonus));
            }

            var stars = new List<StarDef>();
            for (int i = 0; i < db.StarTable.Rows.Count; i++)
            {
                var r = db.StarTable.Rows[i];
                stars.Add(new StarDef(i + 1, r.requiredRep, r.baseVisits, r.budgetMin, r.budgetMax, r.merchantChancePercent));
            }

            var fixtures = new List<FixtureDef>();
            var fixtureMap = new Dictionary<FixtureData, FixtureDef>();
            foreach (var d in db.Fixtures)
            {
                var def = new FixtureDef(d.Id, d.DisplayName, d.Kind, d.ShelfCategory, d.Cost, d.Width, d.Height, d.Walkable,
                    d.DisplayMax, d.RestockTicks, d.Ambience, d.MaxCount, d.CountsTowardShelfLimit, d.UsesFreeShelf);
                fixtures.Add(def);
                fixtureMap[d] = def;
            }

            var sizes = new List<ShopSizeDef>();
            for (int i = 0; i < db.Sizes.Rows.Count; i++)
            {
                var r = db.Sizes.Rows[i];
                sizes.Add(new ShopSizeDef(i + 1, r.width, r.height, r.shelfMax, r.counterMax, r.cost));
            }

            var initial = new List<(FixtureDef Def, Tile Origin)>();
            foreach (var f in b.initialLayout)
            {
                if (f.fixture == null || !fixtureMap.TryGetValue(f.fixture, out var def))
                    throw new InvalidOperationException("初期配置の配置物が GameDatabase にない");
                initial.Add((def, new Tile(f.origin.x, f.origin.y)));
            }

            var balance = new BalanceDef(
                startMoney: b.startMoney,
                repFull: b.repFull, repPartial: b.repPartial, repNone: b.repNone, repQueueLeft: b.repQueueLeft,
                ambiencePermille: b.ambiencePermille, ambienceMax: b.ambienceMax,
                twoWantsChancePercent: b.twoWantsChancePercent,
                checkoutTicks: b.checkoutTicks, queuePatienceTicks: b.queuePatienceTicks, walkTicksPerTile: b.walkTicksPerTile,
                pickTicks: b.pickTicks, emoteTicks: b.emoteTicks,
                maxCustomerStayTicks: b.maxCustomerStayTicks, spawnWindowEndTick: b.spawnWindowEndTick,
                merchantArriveMinTick: b.merchantArriveMinTick, merchantArriveMaxTick: b.merchantArriveMaxTick,
                merchantStayTicks: b.merchantStayTicks, merchantMaxBuy: b.merchantMaxBuy,
                refundPercent: b.refundPercent, freeShelfCount: b.freeShelfCount,
                entrance: new Tile(b.entrance.x, b.entrance.y), initialLayout: initial);

            return new GameConfig(time, balance, items, jobs, stars, fixtures, sizes);
        }
    }
}
