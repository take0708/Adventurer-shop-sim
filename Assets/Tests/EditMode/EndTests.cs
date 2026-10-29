using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>年末・終了・再現性（kihonsekkei.md 7.2〜7.5、shousaisekkei.md 9.2, 9.3、13.1 UT-END-02, 03、UT-DET）。</summary>
    public class EndTests
    {
        /// <summary>
        /// 決まった手順で 24 か月遊ぶ。最初に棚 3 台、毎月の初めに売れ筋を仕入れる。
        /// 返すのはイベント列（型と時刻）と月の集計。
        /// </summary>
        static (GameSession Session, List<string> Events) PlayTwoYears(ulong seed)
        {
            var session = new GameSession(TestConfig.Step1(), seed);
            var log = new List<string>();
            var cfg = session.Config;
            var shelves = new[] { ("shelf_tool", 0, 2, "potion"), ("shelf_weapon", 1, 2, "copper_sword"), ("shelf_armor", 2, 2, "cloth_armor") };
            foreach (var (fixture, x, y, item) in shelves)
            {
                Assert.That(session.PlaceFixture(cfg.FixtureById(fixture), new Tile(x, y)).Ok, Is.True);
                var id = session.Layout.Fixtures.Last().Id;
                session.AssignShelf(id, cfg.ItemById(item));
            }

            void Restock()
            {
                foreach (var (_, _, _, item) in shelves)
                {
                    var def = cfg.ItemById(item);
                    int want = 30 - session.Warehouse.Count(def);
                    int afford = session.State.Money / def.BuyPrice / 3;
                    int n = System.Math.Min(want, afford);
                    if (n > 0) session.BuyFromWholesaler(def, n);
                }
            }

            Restock();
            while (!session.IsEnded)
            {
                session.Tick();
                foreach (var e in session.DrainEvents()) log.Add($"{e.Tick}:{e.GetType().Name}");
                if (session.MonthEndedThisTick && !session.IsEnded) Restock();
            }
            return (session, log);
        }

        static string Describe(MonthlyReport r) =>
            $"{r.Month} {r.Sales} {r.Purchases} {r.Equipment} {r.Visitors} {r.Full} {r.Partial} {r.None} {r.QueueLeft} {r.RepBefore} {r.RepAfter} {r.Star}";

        [Test]
        public void UT_END_02_YearEndedAt12And24_GameEndedAt24_ThenNothing()
        {
            var b = new SessionBuilder();
            b.TickUntil(() => b.Session.IsEnded, 24 * 600 + 1);

            var years = b.Of<YearEndedEvent>().ToList();
            Assert.That(years.Select(y => y.Tick), Is.EqualTo(new[] { 12 * 600, 24 * 600 }));
            Assert.That(years.Select(y => y.Record.Year), Is.EqualTo(new[] { 1, 2 }));
            var ended = b.Of<GameEndedEvent>().Single();
            Assert.That(ended.Tick, Is.EqualTo(24 * 600));
            Assert.That(b.Of<MonthEndedEvent>().Count(), Is.EqualTo(24));
            Assert.That(b.Session.Result, Is.SameAs(ended.Result));
            Assert.That(b.Session.Reports.Count, Is.EqualTo(24));

            // 24 か月目の月末は MonthEnded → YearEnded → GameEnded の順
            var last = b.Events.Where(e => e.Tick == 24 * 600).Select(e => e.GetType()).ToList();
            Assert.That(last.IndexOf(typeof(MonthEndedEvent)), Is.LessThan(last.IndexOf(typeof(YearEndedEvent))));
            Assert.That(last.IndexOf(typeof(YearEndedEvent)), Is.LessThan(last.IndexOf(typeof(GameEndedEvent))));

            // 終わったあとは何もしない
            b.Events.Clear();
            int ticks = b.Session.Clock.ElapsedTicks;
            int money = b.Session.State.Money;
            b.Tick(10);
            Assert.That(b.Session.Clock.ElapsedTicks, Is.EqualTo(ticks));
            Assert.That(b.Session.BuyFromWholesaler(b.Item("potion"), 1).Reason, Is.EqualTo(FailReason.GameOver));
            Assert.That(b.Session.PlaceFixture(b.Config.FixtureById("plant"), new Tile(0, 4)).Reason, Is.EqualTo(FailReason.GameOver));
            Assert.That(b.Session.ExpandShop().Reason, Is.EqualTo(FailReason.GameOver));
            Assert.That(b.Session.BuyFromMerchant(1).Reason, Is.EqualTo(FailReason.GameOver));
            Assert.That(b.Session.AssignShelf(1, null).Reason, Is.EqualTo(FailReason.GameOver));
            b.Drain();
            Assert.That(b.Events, Is.Empty);
            Assert.That(b.Session.State.Money, Is.EqualTo(money));
        }

        [Test]
        public void YearReviewGradeByReputationGained([Values(0, 149, 150, 449, 450)] int gained)
        {
            var b = new SessionBuilder();
            b.TickUntil(() => b.Session.Clock.Month == 12, 12 * 600);
            b.Session.State.AddReputation(gained);
            b.TickUntil(() => b.Session.Years.Count == 1, 700);

            var y = b.Session.Years[0];
            var expected = gained >= 450 ? ReviewGrade.Excellent : gained >= 150 ? ReviewGrade.Good : ReviewGrade.Steady;
            Assert.That(y.RepGained, Is.EqualTo(gained));
            Assert.That(y.Grade, Is.EqualTo(expected));
            Assert.That(y.Star, Is.EqualTo(b.Session.State.Star));
        }

        [Test]
        public void SecondYearCountsOnlyItsOwnReputationAndSales()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(100);
            b.TickUntil(() => b.Session.Years.Count == 1, 12 * 600 + 1);
            b.Session.State.AddReputation(320);
            b.TickUntil(() => b.Session.IsEnded, 12 * 600 + 1);

            var years = b.Session.Years;
            Assert.That(years[0].RepGained, Is.EqualTo(100));
            Assert.That(years[1].RepGained, Is.EqualTo(320));
            Assert.That(years[1].Grade, Is.EqualTo(ReviewGrade.Good));   // 2 年目の境目は 300 / 900
            Assert.That(b.Session.Result.Years, Is.EqualTo(years));
        }

        [Test]
        public void UT_END_03_TotalSalesIsSumOfMonthlySales_WithoutRefunds()
        {
            var (session, _) = PlayTwoYears(7);
            var result = session.Result;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalSales, Is.GreaterThan(0));
            Assert.That(result.TotalSales, Is.EqualTo(session.Reports.Sum(r => r.Sales)));
            Assert.That(result.Years.Sum(y => y.Sales), Is.EqualTo(result.TotalSales));
            Assert.That(result.FinalStar, Is.EqualTo(session.State.Star));
            Assert.That(result.FinalReputation, Is.EqualTo(session.State.Reputation));
            Assert.That(result.StarReachedMonth[0], Is.EqualTo(1));
        }

        [Test]
        public void UT_END_03_RefundIsNotScore()
        {
            var b = new SessionBuilder();
            int id = b.Place("shelf_tool", 0, 2);
            Assert.That(b.Session.RemoveFixture(id).Ok, Is.True);
            b.TickUntil(() => b.Session.IsEnded, 24 * 600 + 1);
            Assert.That(b.Session.Result.TotalSales, Is.EqualTo(0));
        }

        [Test]
        public void UT_DET_01_SameSeedSameCommands_SameReportsAndEvents()
        {
            var (a, ea) = PlayTwoYears(42);
            var (b, eb) = PlayTwoYears(42);
            Assert.That(b.Reports.Select(Describe), Is.EqualTo(a.Reports.Select(Describe)));
            Assert.That(eb, Is.EqualTo(ea));
            Assert.That(b.Result.TotalSales, Is.EqualTo(a.Result.TotalSales));
        }

        [Test]
        public void UT_DET_02_DifferentSeedDifferentResult()
        {
            var (a, ea) = PlayTwoYears(42);
            var (b, eb) = PlayTwoYears(43);
            Assert.That(eb, Is.Not.EqualTo(ea));
            Assert.That(b.Reports.Select(Describe), Is.Not.EqualTo(a.Reports.Select(Describe)));
        }
    }
}
