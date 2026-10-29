using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>行商人と掘り出し物（kihonsekkei.md 6.2.1、shousaisekkei.md 9.1、13.1 UT-MER、UT-CUS-06）。</summary>
    public class MerchantTests
    {
        const int N = 10000;

        /// <summary>来ると決まるまで抽選し直して、来るまで進める。</summary>
        static Merchant Arrive(SessionBuilder b, int star = 1)
        {
            var m = b.Session.Merchant;
            for (int i = 0; i < 1000 && m.State != MerchantState.Scheduled; i++) m.PlanMonth(star);
            Assert.That(m.State, Is.EqualTo(MerchantState.Scheduled));
            b.TickUntil(() => m.State == MerchantState.Present, 600);
            return m;
        }

        [Test]
        public void UT_MER_01_Star1ComesAbout20Percent()
        {
            var b = new SessionBuilder();
            var m = b.Session.Merchant;
            int come = 0;
            for (int i = 0; i < N; i++)
            {
                m.PlanMonth(1);
                if (m.State == MerchantState.Scheduled) come++;
            }
            Assert.That((double)come / N, Is.EqualTo(0.20).Within(0.02));
        }

        [Test]
        public void UT_MER_02_ItemsAreCurrentOrOneLowerStar([Values(1, 2, 3, 5)] int star)
        {
            var b = new SessionBuilder();
            var m = b.Session.Merchant;
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < N; i++)
            {
                m.PlanMonth(star);
                if (m.State != MerchantState.Scheduled) continue;
                Assert.That(m.Item.IsRare, Is.True);
                Assert.That(m.Item.UnlockStar, Is.InRange(star - 1, star), m.Item.Id);
                seen.Add(m.Item.Id);
            }
            int expected = star == 1 ? 1 : 2;
            Assert.That(seen.Count, Is.EqualTo(expected));
        }

        [Test]
        public void UT_MER_03_ArrivesBetween60And399_Stays150Ticks()
        {
            var b = new SessionBuilder();
            var m = b.Session.Merchant;
            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < N; i++)
            {
                m.PlanMonth(5);
                if (m.State != MerchantState.Scheduled) continue;
                Assert.That(m.LeaveTick - m.ArrivalTick, Is.EqualTo(150));
                min = System.Math.Min(min, m.ArrivalTick);
                max = System.Math.Max(max, m.ArrivalTick);
            }
            Assert.That(min, Is.EqualTo(60));
            Assert.That(max, Is.EqualTo(399));

            Arrive(b);
            Assert.That(b.Session.Clock.TickInMonth, Is.EqualTo(m.ArrivalTick + 1));
            var arrived = b.Of<MerchantArrivedEvent>().Single();
            Assert.That(arrived.Item, Is.SameAs(m.Item));
            Assert.That(arrived.LeaveTick, Is.EqualTo(m.LeaveTick));
            Assert.That(m.TicksLeft, Is.EqualTo(150));

            b.TickUntil(() => m.State == MerchantState.Left, 200);
            Assert.That(b.Of<MerchantLeftEvent>().Count(), Is.EqualTo(1));
            Assert.That(b.Session.BuyFromMerchant(1).Reason, Is.EqualTo(FailReason.MerchantGone));
        }

        [Test]
        public void UT_MER_04_BuyUpToTwo_WithoutBargainTable()
        {
            var b = new SessionBuilder();
            Assert.That(b.Session.BuyFromMerchant(1).Reason, Is.EqualTo(FailReason.MerchantGone));

            var m = Arrive(b);
            var item = m.Item;
            Assert.That(b.Session.Shelves.Any(s => s.IsBargainTable), Is.False);
            int money = b.Session.State.Money;

            b.Events.Clear();
            Assert.That(b.Session.BuyFromMerchant(3).Reason, Is.EqualTo(FailReason.InvalidCount));
            Assert.That(b.Session.BuyFromMerchant(0).Reason, Is.EqualTo(FailReason.InvalidCount));
            b.Drain();
            Assert.That(b.Events, Is.Empty);
            Assert.That(b.Session.State.Money, Is.EqualTo(money));

            Assert.That(b.Session.BuyFromMerchant(2).Ok, Is.True);
            b.Drain();
            Assert.That(b.Session.Warehouse.Count(item), Is.EqualTo(2));
            Assert.That(b.Session.State.Money, Is.EqualTo(money - item.BuyPrice * 2));
            Assert.That(b.Session.CurrentReport.Purchases, Is.EqualTo(item.BuyPrice * 2));
            Assert.That(b.Of<MoneyChangedEvent>().Single().Reason, Is.EqualTo(MoneyReason.Purchase));

            // 売り切れても帰る時刻まではいる
            Assert.That(m.Remaining, Is.EqualTo(0));
            Assert.That(b.Session.BuyFromMerchant(1).Reason, Is.EqualTo(FailReason.InvalidCount));
            Assert.That(m.State, Is.EqualTo(MerchantState.Present));
        }

        [Test]
        public void UT_MER_04_NotEnoughMoneyChangesNothing()
        {
            var b = new SessionBuilder(s => s.StartMoney = 0);
            var m = Arrive(b);
            b.Events.Clear();
            Assert.That(b.Session.BuyFromMerchant(1).Reason, Is.EqualTo(FailReason.NotEnoughMoney));
            b.Drain();
            Assert.That(b.Events, Is.Empty);
            Assert.That(m.Remaining, Is.EqualTo(2));
            Assert.That(b.Session.Warehouse.Count(m.Item), Is.EqualTo(0));
        }

        [Test]
        public void UT_MER_05_RareItemsStayInWarehouseAcrossMonths()
        {
            var b = new SessionBuilder();
            var m = Arrive(b);
            var item = m.Item;
            Assert.That(b.Session.BuyFromMerchant(2).Ok, Is.True);

            b.TickUntil(() => b.Session.MonthEndedThisTick, 700);
            b.Tick();
            Assert.That(b.Session.Clock.Month, Is.EqualTo(2));
            Assert.That(b.Session.Warehouse.Count(item), Is.EqualTo(2));
            Assert.That(b.Session.Warehouse.RareItemsInStock, Does.Contain(item));
        }

        [Test]
        public void UT_CUS_06_BargainWantedFirstWhenJobMatchesAndBudgetEnough()
        {
            var b = new SessionBuilder();
            b.Shelf("bargain_table", 1, 2, "silver_robe", 2);   // 魔法使い向け、250G
            var robe = b.Item("silver_robe");
            Assert.That(b.Session.ShelfById(b.Session.Shelves[0].FixtureId).Displayed, Is.GreaterThan(0));

            int matched = 0, notMatched = 0;
            for (int i = 0; i < N; i++)
            {
                var (job, wants, budget) = b.Session.Factory.Create(3, b.Session.Shelves);
                bool expect = job.Id == "mage" && budget >= robe.SellPrice;
                bool first = wants.Count > 0 && wants[0].Item == robe;
                Assert.That(first, Is.EqualTo(expect), $"{job.Id} {budget}");
                if (first)
                {
                    Assert.That(wants[0].IsRare, Is.True);
                    Assert.That(wants.Count(w => w.Item == robe), Is.EqualTo(1));
                    matched++;
                }
                else notMatched++;
            }
            Assert.That(matched, Is.GreaterThan(0));
            Assert.That(notMatched, Is.GreaterThan(0));
        }

        [Test]
        public void UT_CUS_06_NoJobLimitItemWantedByAnyJob()
        {
            var b = new SessionBuilder();
            b.Shelf("bargain_table", 1, 2, "fairy_salve", 2);   // 職業の指定なし、75G
            var salve = b.Item("fairy_salve");
            var jobs = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 2000; i++)
            {
                var (job, wants, budget) = b.Session.Factory.Create(1, b.Session.Shelves);
                Assert.That(wants[0].Item == salve, Is.EqualTo(budget >= salve.SellPrice));
                if (wants[0].Item == salve) jobs.Add(job.Id);
            }
            Assert.That(jobs.Count, Is.EqualTo(3));
        }
    }
}
