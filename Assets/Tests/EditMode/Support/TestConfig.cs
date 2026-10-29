using System;
using System.Collections.Generic;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// youken.md 5 章（と kihonsekkei.md で足した値）を C# で組んだ GameConfig（shousaisekkei.md 13 章）。
    /// ScriptableObject のアセットはこれと一致することを ConfigAssetTests で確かめる。
    /// </summary>
    public static class TestConfig
    {
        /// <summary>組み立て前の値。テストで一部を変えて食い違いを作るのに使う。</summary>
        public sealed class Spec
        {
            public TimeConfig Time = TimeConfig.Step1Default;
            public int StartMoney = 500;
            public Tile Entrance = new Tile(2, 0);
            public int MerchantArriveMaxTick = 400;
            public int QueuePatienceTicks = 120;
            public int MaxCustomerStayTicks = 400;
            // shousaisekkei.md 9.3（案）
            public List<int> ReviewGoodRep = new List<int> { 150, 300 };
            public List<int> ReviewExcellentRep = new List<int> { 450, 900 };
            /// <summary>true なら来店数を 0 にする（客をテストで直接出すとき）。</summary>
            public bool NoVisitors;
            public List<ItemDef> Items = new List<ItemDef>();
            public List<JobDef> Jobs = new List<JobDef>();
            public List<StarDef> Stars = new List<StarDef>();
            public List<FixtureDef> Fixtures = new List<FixtureDef>();
            public List<ShopSizeDef> Sizes = new List<ShopSizeDef>();
            public List<(FixtureDef Def, Tile Origin)> InitialLayout = new List<(FixtureDef Def, Tile Origin)>();

            public FixtureDef Fixture(string id) => Fixtures.Find(f => f.Id == id);
            public ItemDef Item(string id) => Items.Find(i => i.Id == id);

            public GameConfig Build()
            {
                var balance = new BalanceDef(
                    startMoney: StartMoney,
                    repFull: 2, repPartial: 0, repNone: -2, repQueueLeft: -1,
                    ambiencePermille: 15, ambienceMax: 20,
                    twoWantsChancePercent: 40,
                    checkoutTicks: 24, queuePatienceTicks: QueuePatienceTicks, walkTicksPerTile: 3, pickTicks: 5, emoteTicks: 10,
                    maxCustomerStayTicks: MaxCustomerStayTicks, spawnWindowEndTick: 570,
                    merchantArriveMinTick: 60, merchantArriveMaxTick: MerchantArriveMaxTick, merchantStayTicks: 150, merchantMaxBuy: 2,
                    refundPercent: 100, freeShelfCount: 3,
                    entrance: Entrance, initialLayout: InitialLayout,
                    reviewGoodRep: ReviewGoodRep, reviewExcellentRep: ReviewExcellentRep);
                var stars = Stars;
                if (NoVisitors)
                    stars = Stars.ConvertAll(x => new StarDef(x.Star, x.RequiredRep, 0, x.BudgetMin, x.BudgetMax, x.MerchantChancePercent));
                return new GameConfig(Time, balance, Items, Jobs, stars, Fixtures, Sizes);
            }
        }

        public static GameConfig Step1() => Step1Spec().Build();

        public static GameConfig Step1(Action<Spec> modify)
        {
            var spec = Step1Spec();
            modify(spec);
            return spec.Build();
        }

        public static Spec Step1Spec()
        {
            var s = new Spec();

            // youken.md 5.1
            var potion = new ItemDef("potion", "回復薬", ItemCategory.Tool, 6, 10, 1);
            var antidote = new ItemDef("antidote", "解毒薬", ItemCategory.Tool, 9, 15, 2);
            var staff = new ItemDef("wooden_staff", "木の杖", ItemCategory.Weapon, 30, 50, 1);
            var copper = new ItemDef("copper_sword", "銅の剣", ItemCategory.Weapon, 60, 100, 1);
            var ironSword = new ItemDef("iron_sword", "鉄の剣", ItemCategory.Weapon, 180, 300, 3);
            var cloth = new ItemDef("cloth_armor", "布の服", ItemCategory.Armor, 24, 40, 1);
            var shield = new ItemDef("leather_shield", "革の盾", ItemCategory.Armor, 45, 75, 2);
            var ironArmor = new ItemDef("iron_armor", "鉄の鎧", ItemCategory.Armor, 240, 400, 4);

            // youken.md 5.3
            var warrior = new JobDef("warrior", "戦士", new[] { (potion, 2), (copper, 3), (ironSword, 3), (shield, 2), (ironArmor, 2) });
            var mage = new JobDef("mage", "魔法使い", new[] { (potion, 2), (antidote, 1), (staff, 4), (cloth, 3) });
            var priest = new JobDef("priest", "僧侶", new[] { (potion, 3), (antidote, 3), (staff, 1), (cloth, 2), (shield, 1) });

            s.Items.AddRange(new[] { potion, antidote, staff, copper, ironSword, cloth, shield, ironArmor });
            // youken.md 5.7
            s.Items.Add(new ItemDef("fairy_salve", "妖精の傷薬", ItemCategory.Rare, 45, 75, 1, null, 3));
            s.Items.Add(new ItemDef("elixir", "霊薬", ItemCategory.Rare, 70, 120, 2, priest, 4));
            s.Items.Add(new ItemDef("silver_robe", "銀糸のローブ", ItemCategory.Rare, 150, 250, 3, mage, 6));
            s.Items.Add(new ItemDef("stardust_staff", "星屑の杖", ItemCategory.Rare, 270, 450, 4, mage, 8));
            s.Items.Add(new ItemDef("dragon_shield", "竜鱗の盾", ItemCategory.Rare, 420, 700, 5, warrior, 10));
            s.Jobs.AddRange(new[] { warrior, mage, priest });

            // youken.md 5.2, 5.4, 5.7
            s.Stars.Add(new StarDef(1, 0, 15, 40, 130, 20));
            s.Stars.Add(new StarDef(2, 50, 22, 40, 180, 25));
            s.Stars.Add(new StarDef(3, 220, 32, 80, 380, 30));
            s.Stars.Add(new StarDef(4, 560, 44, 150, 650, 35));
            s.Stars.Add(new StarDef(5, 1250, 58, 250, 1000, 40));

            // youken.md 5.5、kihonsekkei.md 4.2, 6.2
            s.Fixtures.Add(new FixtureDef("shelf_weapon", "武器棚", FixtureKind.Shelf, ItemCategory.Weapon, 100, 1, 1, false, 3, 40, 0, 0, true, true));
            s.Fixtures.Add(new FixtureDef("shelf_armor", "防具棚", FixtureKind.Shelf, ItemCategory.Armor, 100, 1, 1, false, 3, 40, 0, 0, true, true));
            s.Fixtures.Add(new FixtureDef("shelf_tool", "道具棚", FixtureKind.Shelf, ItemCategory.Tool, 100, 1, 1, false, 8, 15, 0, 0, true, true));
            s.Fixtures.Add(new FixtureDef("counter", "カウンター", FixtureKind.Counter, ItemCategory.Tool, 250, 2, 1, false, 0, 0, 0, 0, false, false));
            s.Fixtures.Add(new FixtureDef("bargain_table", "特売台", FixtureKind.Shelf, ItemCategory.Rare, 200, 1, 1, false, 2, 15, 0, 1, false, false));
            s.Fixtures.Add(new FixtureDef("plant", "観葉植物", FixtureKind.Decor, ItemCategory.Tool, 80, 1, 1, false, 0, 0, 1, 0, false, false));
            s.Fixtures.Add(new FixtureDef("rug", "絨毯", FixtureKind.Decor, ItemCategory.Tool, 150, 2, 2, true, 0, 0, 2, 0, false, false));
            s.Fixtures.Add(new FixtureDef("armor_stand", "甲冑の飾り", FixtureKind.Decor, ItemCategory.Tool, 300, 1, 1, false, 0, 0, 4, 0, false, false));

            // youken.md 5.6
            s.Sizes.Add(new ShopSizeDef(1, 6, 5, 6, 2, 0));
            s.Sizes.Add(new ShopSizeDef(2, 8, 6, 10, 3, 1500));
            s.Sizes.Add(new ShopSizeDef(3, 10, 7, 15, 4, 5000));

            s.InitialLayout.Add((s.Fixture("counter"), new Tile(4, 1)));
            return s;
        }
    }
}
