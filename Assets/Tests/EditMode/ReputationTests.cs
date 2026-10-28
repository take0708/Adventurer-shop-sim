using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>評判と星（kihonsekkei.md 5.5, 7.2、shousaisekkei.md 5.2, 9.2、13.1 UT-REP、UT-BHV の評判）。</summary>
    public class ReputationTests
    {
        static ReputationChangedEvent RepOf(SessionBuilder b, Customer c)
        {
            // 客ごとの ReputationChanged は CustomerLeft の直前に積まれる
            var list = b.Events;
            int left = list.FindIndex(e => e is CustomerLeftEvent l && l.CustomerId == c.Id);
            Assert.That(left, Is.GreaterThan(0));
            return (ReputationChangedEvent)list[left - 1];
        }

        /// <summary>月末まで進めて MonthEnded の集計と、あれば StarRaised を返す。</summary>
        static (MonthlyReport Report, StarRaisedEvent Raised) EndMonth(SessionBuilder b)
        {
            b.Events.Clear();
            b.Tick();
            b.TickUntil(() => b.Session.MonthEndedThisTick, 700);
            return (b.Of<MonthEndedEvent>().Single().Report, b.Of<StarRaisedEvent>().SingleOrDefault());
        }

        [Test]
        public void ResultsChangeReputation_Full2_Partial0_None2_QueueLeft1()
        {
            var b = new SessionBuilder(s => s.QueuePatienceTicks = 20);
            b.Session.State.AddReputation(10);
            b.Shelf("shelf_tool", 1, 1, "potion", 50);
            b.Shelf("shelf_weapon", 0, 4, "copper_sword", 5);

            var full = b.Spawn("mage", 100, "potion");
            var partial = b.Spawn("mage", 100, "potion", "cloth_armor");
            var queueLeft = b.Spawn("mage", 100, "potion");
            var none = b.Spawn("warrior", 50, "copper_sword");
            foreach (var c in new[] { full, partial, queueLeft, none }) b.TickUntilGone(c);

            Assert.That(queueLeft.Result, Is.EqualTo(CustomerResult.QueueLeft));
            Assert.That(RepOf(b, full).Delta, Is.EqualTo(2));
            Assert.That(RepOf(b, partial).Delta, Is.EqualTo(0));
            Assert.That(RepOf(b, none).Delta, Is.EqualTo(-2));
            Assert.That(RepOf(b, queueLeft).Delta, Is.EqualTo(-1));
            Assert.That(b.Session.State.Reputation, Is.EqualTo(10 + 2 + 0 - 2 - 1));
        }

        [Test]
        public void UT_BHV_08_OnlyBargainBought_NoneMinus2PlusBonus()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(10);
            b.Shelf("bargain_table", 1, 1, "fairy_salve", 2);

            var c = b.Spawn("mage", 130, "fairy_salve");
            b.TickUntilGone(c);

            Assert.That(c.Result, Is.EqualTo(CustomerResult.None));
            Assert.That(b.Of<CustomerEmoteEvent>().Any(e => e.CustomerId == c.Id && e.Kind == EmoteKind.Treasure), Is.True);
            var rep = RepOf(b, c);
            Assert.That(rep.RareBonus, Is.EqualTo(3));
            Assert.That(rep.Delta, Is.EqualTo(-2 + 3));
            Assert.That(b.Session.CurrentReport.RareSold, Is.EqualTo(1));
        }

        [Test]
        public void UT_REP_01_NeverBelowZero()
        {
            var b = new SessionBuilder();
            var c = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntilGone(c);

            Assert.That(c.Result, Is.EqualTo(CustomerResult.None));
            Assert.That(RepOf(b, c).Delta, Is.EqualTo(0));
            Assert.That(b.Session.State.Reputation, Is.EqualTo(0));
        }

        [Test]
        public void UT_REP_02_StarRisesOnlyAtMonthEnd_WithUnlockedItems()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(60);
            b.Tick(10);
            Assert.That(b.Session.State.Star, Is.EqualTo(1));
            Assert.That(b.Session.BuyFromWholesaler(b.Item("antidote"), 1).Reason, Is.EqualTo(FailReason.Locked));

            var (report, raised) = EndMonth(b);
            Assert.That(raised, Is.Not.Null);
            Assert.That(raised.NewStar, Is.EqualTo(2));
            Assert.That(raised.Unlocked.Select(i => i.Id), Is.EqualTo(new[] { "antidote", "leather_shield" }));
            Assert.That(report.Star, Is.EqualTo(2));
            Assert.That(report.RepBefore, Is.EqualTo(0));
            Assert.That(report.RepAfter, Is.EqualTo(60));
            Assert.That(report.RepToNextStar, Is.EqualTo(220 - 60));

            // MonthEnded のあとに StarRaised
            Assert.That(b.Events.IndexOf(raised), Is.GreaterThan(b.Events.FindIndex(e => e is MonthEndedEvent)));
            Assert.That(b.Session.State.Star, Is.EqualTo(2));
            Assert.That(b.Session.State.StarReachedMonth[1], Is.EqualTo(2));
            Assert.That(b.Session.BuyFromWholesaler(b.Item("antidote"), 1).Ok, Is.True);
            Assert.That(b.Session.CurrentReport.RepBefore, Is.EqualTo(60));
        }

        [Test]
        public void UT_REP_02_NextMonthUsesNewStarForVisitors()
        {
            var b = new SessionBuilder(s => s.NoVisitors = false);
            // 棚がないので来た 15 人は不満（−2）で帰る。それでも ★2（50）に届くように多めに足す
            b.Session.State.AddReputation(100);
            EndMonth(b);
            Assert.That(b.Session.PlannedVisits, Is.EqualTo(22));
        }

        [Test]
        public void UT_REP_03_TwoStepsAtOnce()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(300);

            var (_, raised) = EndMonth(b);
            Assert.That(raised.NewStar, Is.EqualTo(3));
            Assert.That(raised.Unlocked.Select(i => i.Id), Is.EqualTo(new[] { "antidote", "iron_sword", "leather_shield" }));
            Assert.That(b.Session.State.StarReachedMonth.ToArray(), Is.EqualTo(new[] { 1, 2, 2, 0, 0 }));
        }

        [Test]
        public void UT_REP_04_StarNeverGoesDown()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(60);
            EndMonth(b);
            Assert.That(b.Session.State.Star, Is.EqualTo(2));

            b.Session.State.AddReputation(-60);
            var (report, raised) = EndMonth(b);
            Assert.That(raised, Is.Null);
            Assert.That(b.Session.State.Star, Is.EqualTo(2));
            Assert.That(report.Star, Is.EqualTo(2));
            Assert.That(report.RepDelta, Is.EqualTo(-60));
        }

        [Test]
        public void DebugStarUpAtMonthEnd_RaisesStarEvenIfCustomersLowerReputation()
        {
            // 棚がないので、来た 15 人はみな不満（−2）で帰る
            var b = new SessionBuilder(s => s.NoVisitors = false);
            b.Session.DebugAddReputation(40);
            b.Session.DebugStarUpAtMonthEnd();
            Assert.That(b.Session.DebugStarUpPending, Is.True);
            Assert.That(b.Session.State.Star, Is.EqualTo(1));

            var (report, raised) = EndMonth(b);
            Assert.That(raised.NewStar, Is.EqualTo(2));
            Assert.That(report.RepAfter, Is.EqualTo(50));
            Assert.That(b.Session.DebugStarUpPending, Is.False);

            // 次の月は予約していないので上がらない
            var (_, again) = EndMonth(b);
            Assert.That(again, Is.Null);
        }

        [Test]
        public void RepToNextStarIsZeroAtMaxStar()
        {
            var b = new SessionBuilder();
            b.Session.State.AddReputation(2000);
            EndMonth(b);
            Assert.That(b.Session.State.Star, Is.EqualTo(5));
            Assert.That(b.Session.RepToNextStar, Is.EqualTo(0));
        }
    }
}
