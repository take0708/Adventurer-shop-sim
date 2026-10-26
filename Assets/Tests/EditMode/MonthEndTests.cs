using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>月末の処理と集計（kihonsekkei.md 7.1、shousaisekkei.md 9.2, 9.3、13.1 UT-END）。</summary>
    public class MonthEndTests
    {
        /// <summary>1 か月目の初めに、仕入れて棚を置き、ふつうに客を来させる。</summary>
        static GameSession OpenShop(List<GameEvent> events)
        {
            var s = new GameSession(TestConfig.Step1(), 7);
            var cfg = s.Config;
            Place(s, "shelf_tool", 0, 3, "potion");
            Place(s, "shelf_weapon", 1, 4, "copper_sword");
            Place(s, "shelf_armor", 5, 3, "cloth_armor");
            Assert.That(s.BuyFromWholesaler(cfg.ItemById("potion"), 20).Ok, Is.True);
            Assert.That(s.BuyFromWholesaler(cfg.ItemById("copper_sword"), 3).Ok, Is.True);
            Assert.That(s.BuyFromWholesaler(cfg.ItemById("cloth_armor"), 5).Ok, Is.True);
            foreach (var shelf in s.Shelves)
            {
                var item = s.UnlockedItems.First(i => i.Category == shelf.Def.ShelfCategory && s.Warehouse.Count(i) > 0);
                Assert.That(s.AssignShelf(shelf.FixtureId, item).Ok, Is.True);
            }
            // 置いて撤去した分は設備投資から引かれる
            Assert.That(s.PlaceFixture(cfg.FixtureById("plant"), new Tile(0, 1)).Ok, Is.True);
            Assert.That(s.RemoveFixture(s.Layout.Fixtures.Last().Id).Ok, Is.True);
            Assert.That(s.PlaceFixture(cfg.FixtureById("plant"), new Tile(0, 1)).Ok, Is.True);
            events.AddRange(s.DrainEvents());
            return s;
        }

        static void Place(GameSession s, string fixtureId, int x, int y, string itemId)
        {
            Assert.That(s.PlaceFixture(s.Config.FixtureById(fixtureId), new Tile(x, y)).Ok, Is.True);
        }

        [Test]
        public void UT_END_01_MonthEndsAt600_ReportMatchesMoneyFlow()
        {
            var events = new List<GameEvent>();
            var s = OpenShop(events);
            Assert.That(s.CurrentReport.Purchases, Is.EqualTo(120 + 180 + 120));
            Assert.That(s.CurrentReport.Equipment, Is.EqualTo(80));

            for (int i = 0; i < 599; i++)
            {
                s.Tick();
                Assert.That(s.MonthEndedThisTick, Is.False);
            }
            events.AddRange(s.DrainEvents());
            Assert.That(events.OfType<MonthEndedEvent>(), Is.Empty);

            s.Tick();
            Assert.That(s.MonthEndedThisTick, Is.True);
            events.AddRange(s.DrainEvents());
            var report = events.OfType<MonthEndedEvent>().Single().Report;

            Assert.That(report.Month, Is.EqualTo(1));
            Assert.That(report.IsClosed, Is.True);
            Assert.That(s.State.Money - 500, Is.EqualTo(report.Net));
            Assert.That(report.Sales, Is.EqualTo(s.State.TotalSales));
            Assert.That(report.Sales, Is.EqualTo(events.OfType<CheckoutDoneEvent>().Sum(e => e.Amount)));
            Assert.That(report.Visitors, Is.EqualTo(15));
            Assert.That(report.Full + report.Partial + report.None + report.QueueLeft,
                Is.EqualTo(events.OfType<CustomerLeftEvent>().Count()));
            Assert.That(report.Full, Is.EqualTo(events.OfType<CustomerLeftEvent>().Count(e => e.Result == CustomerResult.Full)));
            Assert.That(report.Star, Is.EqualTo(1));

            // 翌月の集計が始まっている
            Assert.That(s.CurrentReport, Is.Not.SameAs(report));
            Assert.That(s.CurrentReport.Month, Is.EqualTo(2));
            Assert.That(s.CurrentReport.Sales, Is.EqualTo(0));
            s.Tick();
            Assert.That(s.MonthEndedThisTick, Is.False);
        }

        [Test]
        public void UT_END_01_SecondMonthReportCountsOnlyThatMonth()
        {
            var events = new List<GameEvent>();
            var s = OpenShop(events);
            for (int i = 0; i < 600; i++) s.Tick();
            int moneyAfterMonth1 = s.State.Money;
            int salesAfterMonth1 = s.State.TotalSales;
            s.DrainEvents();

            Assert.That(s.BuyFromWholesaler(s.Config.ItemById("potion"), 10).Ok, Is.True);
            for (int i = 0; i < 600; i++) s.Tick();
            var report = s.DrainEvents().OfType<MonthEndedEvent>().Single().Report;

            Assert.That(report.Month, Is.EqualTo(2));
            Assert.That(report.Purchases, Is.EqualTo(60));
            Assert.That(report.Equipment, Is.EqualTo(0));
            Assert.That(report.Sales, Is.EqualTo(s.State.TotalSales - salesAfterMonth1));
            Assert.That(s.State.Money - moneyAfterMonth1, Is.EqualTo(report.Net));
        }
    }
}
