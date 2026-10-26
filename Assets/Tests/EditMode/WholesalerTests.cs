using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>問屋（shousaisekkei.md 5.4、13.1 UT-STK）。</summary>
    public class WholesalerTests
    {
        [Test]
        public void UT_STK_01_BuyingSpendsMoneyAndFillsWarehouse()
        {
            var b = new SessionBuilder();
            var copper = b.Item("copper_sword");
            int money = b.Session.State.Money;

            Assert.That(b.Session.BuyFromWholesaler(copper, 3).Ok, Is.True);
            b.Drain();

            Assert.That(b.Session.State.Money, Is.EqualTo(money - 180));
            Assert.That(b.Session.Warehouse.Count(copper), Is.EqualTo(3));
            Assert.That(b.Session.CurrentReport.Purchases, Is.EqualTo(180));
            var m = b.Of<MoneyChangedEvent>().Single();
            Assert.That(m.Delta, Is.EqualTo(-180));
            Assert.That(m.Reason, Is.EqualTo(MoneyReason.Purchase));
            Assert.That(b.Of<WarehouseChangedEvent>().Single().Count, Is.EqualTo(3));
        }

        [Test]
        public void UT_STK_01_BoughtItemsAreNotDisplayedAtOnce()
        {
            var b = new SessionBuilder();
            var shelf = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 0);
            Assert.That(b.Session.BuyFromWholesaler(b.Item("copper_sword"), 3).Ok, Is.True);
            Assert.That(shelf.Displayed, Is.EqualTo(0));
            b.Tick(40);
            Assert.That(shelf.Displayed, Is.EqualTo(1));
        }

        [Test]
        public void UT_STK_02_LockedItem()
        {
            var b = new SessionBuilder();
            Assert.That(b.Session.BuyFromWholesaler(b.Item("iron_sword"), 1).Reason, Is.EqualTo(FailReason.Locked));
            Assert.That(b.Session.UnlockedItems.Select(i => i.Id),
                Is.EqualTo(new[] { "potion", "wooden_staff", "copper_sword", "cloth_armor" }));
        }

        [Test]
        public void UT_STK_03_NotEnoughMoneyChangesNothing()
        {
            var b = new SessionBuilder();
            var copper = b.Item("copper_sword");
            int money = b.Session.State.Money;
            b.Drain();
            b.Events.Clear();

            Assert.That(b.Session.BuyFromWholesaler(copper, 9).Reason, Is.EqualTo(FailReason.NotEnoughMoney));
            b.Drain();
            Assert.That(b.Session.State.Money, Is.EqualTo(money));
            Assert.That(b.Session.Warehouse.Count(copper), Is.EqualTo(0));
            Assert.That(b.Session.CurrentReport.Purchases, Is.EqualTo(0));
            Assert.That(b.Events, Is.Empty);

            // ちょうど払える額なら買える
            Assert.That(b.Session.BuyFromWholesaler(copper, money / 60).Ok, Is.True);
        }

        [Test]
        public void UT_STK_04_RareItemAndZeroCount()
        {
            var b = new SessionBuilder();
            Assert.That(b.Session.BuyFromWholesaler(b.Item("fairy_salve"), 1).Reason, Is.EqualTo(FailReason.NotSoldHere));
            Assert.That(b.Session.BuyFromWholesaler(b.Item("potion"), 0).Reason, Is.EqualTo(FailReason.InvalidCount));
            Assert.That(b.Session.BuyFromWholesaler(b.Item("potion"), -1).Reason, Is.EqualTo(FailReason.InvalidCount));
        }
    }
}
