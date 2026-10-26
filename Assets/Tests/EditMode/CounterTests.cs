using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>会計と行列（kihonsekkei.md 6.3、shousaisekkei.md 7.2、13.1 UT-CNT）。</summary>
    public class CounterTests
    {
        static Customer[] SpawnBuyers(SessionBuilder b, int n) =>
            Enumerable.Range(0, n).Select(_ => b.Spawn("mage", 100, "potion")).ToArray();

        [Test]
        public void UT_CNT_01_CheckoutTakes24TicksEach()
        {
            var b = new SessionBuilder(s => s.QueuePatienceTicks = 1000);
            b.Shelf("shelf_tool", 1, 1, "potion", 50);
            var customers = SpawnBuyers(b, 5);
            foreach (var c in customers) b.TickUntilGone(c);

            var done = b.Of<CheckoutDoneEvent>().ToList();
            Assert.That(done.Select(e => e.CustomerId), Is.EqualTo(customers.Select(c => c.Id)));
            for (int i = 1; i < done.Count; i++)
                Assert.That(done[i].Tick - done[i - 1].Tick, Is.EqualTo(24), $"{i} 人目と {i + 1} 人目の間");
            Assert.That(customers.All(c => c.Result == CustomerResult.Full), Is.True);
            Assert.That(b.Session.State.TotalSales, Is.EqualTo(50));
        }

        [Test]
        public void UT_CNT_01_QueueTilesFollowPathToEntrance()
        {
            var b = new SessionBuilder();
            var counter = b.Session.Counters.Single();
            Assert.That(counter.PayTile, Is.EqualTo(new Tile(4, 0)));
            Assert.That(counter.QueueTiles, Is.EqualTo(new[] { new Tile(3, 0), new Tile(2, 0) }));
        }

        [Test]
        public void UT_CNT_02_GivesUpAfterPatience_PutsItemBack()
        {
            var b = new SessionBuilder(s => s.QueuePatienceTicks = 40);
            var shelf = b.Shelf("shelf_tool", 1, 1, "potion", 50);
            var potion = b.Item("potion");
            var customers = SpawnBuyers(b, 3);
            foreach (var c in customers) b.TickUntilGone(c);

            Assert.That(customers.Select(c => c.Result), Is.EqualTo(new CustomerResult?[] { CustomerResult.Full, CustomerResult.Full, CustomerResult.QueueLeft }));
            Assert.That(b.Session.State.TotalSales, Is.EqualTo(20));
            Assert.That(b.Of<CustomerLeftEvent>().Single(e => e.CustomerId == customers[2].Id).Bought, Is.Empty);
            // かごの品は棚か倉庫に戻っている
            Assert.That(shelf.Displayed + b.Session.Warehouse.Count(potion), Is.EqualTo(48));
        }

        [Test]
        public void UT_CNT_03_ChoosesCounterWithFewerWaiting()
        {
            var b = new SessionBuilder();
            b.Shelf("shelf_tool", 1, 1, "potion", 50);
            b.Place("counter", 0, 2);
            Assert.That(b.Session.Counters.Count, Is.EqualTo(2));

            var c1 = b.Spawn("mage", 100, "potion");
            var c2 = b.Spawn("mage", 100, "potion");
            b.TickUntil(() => c1.CounterId != 0 && c2.CounterId != 0);
            Assert.That(c1.CounterId, Is.Not.EqualTo(c2.CounterId));

            b.TickUntilGone(c1);
            b.TickUntilGone(c2);
            Assert.That(c1.Result, Is.EqualTo(CustomerResult.Full));
            Assert.That(c2.Result, Is.EqualTo(CustomerResult.Full));
        }

        [Test]
        public void UT_CNT_04_RemovingCounterRequeuesAtOtherKeepingWait()
        {
            var b = new SessionBuilder();
            b.Shelf("shelf_tool", 1, 1, "potion", 50);
            b.Place("counter", 0, 2);

            var c = b.Spawn("mage", 100, "potion");
            b.TickUntil(() => c.State == CustomerState.ToQueue || c.State == CustomerState.Queuing);
            b.Tick(2);
            int oldCounter = c.CounterId;
            int waited = c.QueueWaitTicks;
            Assert.That(waited, Is.GreaterThan(0));

            Assert.That(b.Session.RemoveFixture(oldCounter).Ok, Is.True);
            Assert.That(c.CounterId, Is.Not.EqualTo(oldCounter));
            Assert.That(b.Session.CounterById(c.CounterId), Is.Not.Null);
            Assert.That(c.QueueWaitTicks, Is.EqualTo(waited));

            b.TickUntilGone(c);
            Assert.That(c.Result, Is.EqualTo(CustomerResult.Full));
        }
    }
}
