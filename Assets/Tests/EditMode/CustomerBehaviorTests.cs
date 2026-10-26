using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// 客の行動（kihonsekkei.md 5.3、shousaisekkei.md 8.4, 8.5、13.1 UT-BHV）。
    /// 評判の増減は M5 で ReputationSystem を作るときに確かめる。
    /// </summary>
    public class CustomerBehaviorTests
    {
        static CustomerLeftEvent LeftOf(SessionBuilder b, Customer c) => b.Of<CustomerLeftEvent>().Single(e => e.CustomerId == c.Id);

        static EmoteKind[] EmotesOf(SessionBuilder b, Customer c) =>
            b.Of<CustomerEmoteEvent>().Where(e => e.CustomerId == c.Id).Select(e => e.Kind).ToArray();

        [Test]
        public void UT_BHV_01_WalksToShelf_Picks_PaysAndLeavesFull()
        {
            var b = new SessionBuilder();
            var shelf = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 5);
            int money = b.Session.State.Money;

            var c = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntilGone(c);

            var left = LeftOf(b, c);
            Assert.That(left.Result, Is.EqualTo(CustomerResult.Full));
            Assert.That(left.Bought.Select(i => i.Id), Is.EqualTo(new[] { "copper_sword" }));
            Assert.That(b.Session.State.Money, Is.EqualTo(money + 100));
            Assert.That(b.Session.State.TotalSales, Is.EqualTo(100));
            Assert.That(b.Of<CheckoutDoneEvent>().Single().Amount, Is.EqualTo(100));
            Assert.That(b.Of<ItemTakenEvent>().Single().FixtureId, Is.EqualTo(shelf.FixtureId));
            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.Picked }));
            Assert.That(b.Of<CustomerExitedEvent>().Single().CustomerId, Is.EqualTo(c.Id));
            Assert.That(b.Session.Customers, Is.Empty);
        }

        [Test]
        public void UT_BHV_02_EmptyShelf_OutOfStock_None()
        {
            var b = new SessionBuilder();
            b.Shelf("shelf_weapon", 0, 3, "copper_sword", 0);
            int money = b.Session.State.Money;

            var c = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntilGone(c);

            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.OutOfStock }));
            Assert.That(c.Wants[0].Fail, Is.EqualTo(FailKind.OutOfStock));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
            Assert.That(b.Session.State.Money, Is.EqualTo(money));
            Assert.That(b.Of<CheckoutDoneEvent>(), Is.Empty);
        }

        [Test]
        public void UT_BHV_03_TooExpensive()
        {
            var b = new SessionBuilder();
            var shelf = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 5);

            var c = b.Spawn("warrior", 99, "copper_sword");
            b.TickUntilGone(c);

            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.TooExpensive }));
            Assert.That(c.Wants[0].Fail, Is.EqualTo(FailKind.TooExpensive));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
            Assert.That(shelf.Displayed, Is.EqualTo(3));
        }

        [Test]
        public void UT_BHV_04_OneOfTwo_Partial()
        {
            var b = new SessionBuilder();
            b.Shelf("shelf_weapon", 0, 3, "copper_sword", 5);
            int money = b.Session.State.Money;

            var c = b.Spawn("warrior", 130, "copper_sword", "potion");
            b.TickUntilGone(c);

            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.Partial));
            Assert.That(c.Wants[1].Fail, Is.EqualTo(FailKind.NotCarried));
            Assert.That(b.Session.State.Money, Is.EqualTo(money + 100));
        }

        [Test]
        public void UT_BHV_05_NotCarried_GoesToSameCategoryShelf()
        {
            var b = new SessionBuilder();
            int tool = b.Place("shelf_tool", 5, 3);

            var c = b.Spawn("priest", 130, "potion");
            b.TickUntil(() => c.State == CustomerState.Disappointed);

            Assert.That(c.TargetShelfId, Is.EqualTo(tool));
            var shelfTile = b.Session.Layout.FindById(tool).Origin;
            Assert.That(System.Math.Abs(c.Tile.X - shelfTile.X) + System.Math.Abs(c.Tile.Y - shelfTile.Y), Is.EqualTo(1));
            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.NotCarried }));

            b.TickUntilGone(c);
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
        }

        [Test]
        public void UT_BHV_05_NotCarried_NoShelfOfCategory_Wanders()
        {
            var b = new SessionBuilder();
            var c = b.Spawn("priest", 130, "potion");
            b.TickUntil(() => c.State == CustomerState.Wandering);

            int fromEntrance = b.Session.Pathfinder.Distance(b.Session.Layout.Entrance, c.Dest);
            Assert.That(fromEntrance, Is.InRange(2, 4));

            b.TickUntilGone(c);
            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.NotCarried }));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
        }

        [Test]
        public void UT_BHV_06_SoldOutOnArrival_RetargetsOnceToStockedShelf()
        {
            var b = new SessionBuilder();
            var near = b.Shelf("shelf_weapon", 0, 1, "copper_sword", 1);
            var far = b.Shelf("shelf_weapon", 0, 4, "copper_sword", 1);
            Assert.That(near.Displayed, Is.EqualTo(1));
            Assert.That(far.Displayed, Is.EqualTo(1));

            var c1 = b.Spawn("warrior", 130, "copper_sword");
            b.Tick();
            var c2 = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntil(() => c2.State != CustomerState.Entering);
            Assert.That(c2.TargetShelfId, Is.EqualTo(near.FixtureId));

            b.TickUntilGone(c2);
            Assert.That(LeftOf(b, c1).Result, Is.EqualTo(CustomerResult.Full));
            Assert.That(LeftOf(b, c2).Result, Is.EqualTo(CustomerResult.Full));
            Assert.That(EmotesOf(b, c2), Is.EqualTo(new[] { EmoteKind.Picked }));
            Assert.That(b.Of<ItemTakenEvent>().Single(e => e.CustomerId == c2.Id).FixtureId, Is.EqualTo(far.FixtureId));
        }

        [Test]
        public void UT_BHV_06_RetargetsOnlyOnce()
        {
            var b = new SessionBuilder();
            var near = b.Shelf("shelf_weapon", 0, 1, "copper_sword", 1);
            var far = b.Shelf("shelf_weapon", 0, 4, "copper_sword", 1);

            var c = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntil(() => c.State == CustomerState.ToShelf);
            // 着く前に両方売り切れる
            near.TryTake();
            b.TickUntil(() => c.TargetShelfId == far.FixtureId);
            far.TryTake();

            b.TickUntilGone(c);
            Assert.That(EmotesOf(b, c), Is.EqualTo(new[] { EmoteKind.OutOfStock }));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
        }

        [Test]
        public void UT_BHV_07_MaxStay_GivesUpAndLeaves()
        {
            var b = new SessionBuilder(s => s.MaxCustomerStayTicks = 10);
            b.Shelf("shelf_weapon", 0, 4, "copper_sword", 5);

            var c = b.Spawn("warrior", 130, "copper_sword");
            b.TickUntilGone(c);

            Assert.That(c.Wants[0].Fail, Is.EqualTo(FailKind.GaveUp));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.None));
        }

        [Test]
        public void UT_BHV_07_MaxStay_GoesToCounterWithBasket()
        {
            var b = new SessionBuilder(s => s.MaxCustomerStayTicks = 20);
            b.Shelf("shelf_tool", 1, 1, "potion", 5);
            b.Shelf("shelf_weapon", 0, 4, "copper_sword", 5);
            int money = b.Session.State.Money;

            var c = b.Spawn("warrior", 130, "potion", "copper_sword");
            b.TickUntilGone(c);

            Assert.That(c.Wants[0].Status, Is.EqualTo(WantStatus.InBasket));
            Assert.That(c.Wants[1].Fail, Is.EqualTo(FailKind.GaveUp));
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.Partial));
            Assert.That(b.Session.State.Money, Is.EqualTo(money + 10));
        }

        [Test]
        public void UT_LAY_09_PlacingOnCustomerMovesThemToWalkableTile()
        {
            var b = new SessionBuilder();
            b.Shelf("shelf_weapon", 0, 4, "copper_sword", 5);
            var c = b.Spawn("warrior", 130, "copper_sword");
            var spot = new Tile(2, 1);
            b.TickUntil(() => c.Tile == spot);

            b.Events.Clear();
            Assert.That(b.Session.PlaceFixture(b.Config.FixtureById("plant"), spot).Ok, Is.True);
            b.Drain();

            Assert.That(c.Tile, Is.Not.EqualTo(spot));
            Assert.That(b.Session.Layout.IsWalkable(c.Tile), Is.True);
            Assert.That(b.Of<CustomerMovedEvent>().Any(e => e.CustomerId == c.Id && e.Ticks == 0 && e.To == c.Tile), Is.True);

            b.TickUntilGone(c);
            Assert.That(LeftOf(b, c).Result, Is.EqualTo(CustomerResult.Full));
        }
    }
}
