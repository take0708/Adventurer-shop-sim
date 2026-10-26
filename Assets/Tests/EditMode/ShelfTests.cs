using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>棚の陳列と補充（kihonsekkei.md 6.2、shousaisekkei.md 7.1、13.1 UT-SHF）。</summary>
    public class ShelfTests
    {
        [Test]
        public void UT_SHF_01_AssignFillsUpToDisplayMax()
        {
            var b = new SessionBuilder();
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 10);
            Assert.That(weapon.Displayed, Is.EqualTo(3));
            Assert.That(b.Session.Warehouse.Count(b.Item("copper_sword")), Is.EqualTo(7));

            var tool = b.Shelf("shelf_tool", 5, 3, "potion", 20);
            Assert.That(tool.Displayed, Is.EqualTo(8));
            Assert.That(b.Session.Warehouse.Count(b.Item("potion")), Is.EqualTo(12));
        }

        [Test]
        public void UT_SHF_02_RestocksOneAfterRestockTicks()
        {
            var b = new SessionBuilder();
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 10);
            var tool = b.Shelf("shelf_tool", 5, 3, "potion", 20);
            weapon.TryTake();
            tool.TryTake();

            b.Tick(14);
            Assert.That(tool.Displayed, Is.EqualTo(7));
            b.Tick(1);
            Assert.That(tool.Displayed, Is.EqualTo(8));

            b.Tick(24);
            Assert.That(weapon.Displayed, Is.EqualTo(2));
            b.Tick(1);
            Assert.That(weapon.Displayed, Is.EqualTo(3));
            Assert.That(b.Session.Warehouse.Count(b.Item("copper_sword")), Is.EqualTo(6));
        }

        [Test]
        public void UT_SHF_03_NoRestockFromEmptyWarehouse_AndEmptiedEvent()
        {
            var b = new SessionBuilder();
            // 問屋の品は倉庫に 0 個でも設定できる
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 1);
            Assert.That(weapon.Displayed, Is.EqualTo(1));

            weapon.TryTake();
            b.Drain();
            Assert.That(weapon.Displayed, Is.EqualTo(0));
            Assert.That(b.Of<ShelfEmptiedEvent>().Single().FixtureId, Is.EqualTo(weapon.FixtureId));

            b.Tick(100);
            Assert.That(weapon.Displayed, Is.EqualTo(0));
        }

        [Test]
        public void UT_SHF_04_ChangingItemReturnsDisplayedToWarehouse()
        {
            var b = new SessionBuilder();
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 10);
            b.Session.DebugAddStock(b.Item("wooden_staff"), 5);

            Assert.That(b.Session.AssignShelf(weapon.FixtureId, b.Item("wooden_staff")).Ok, Is.True);
            Assert.That(weapon.Item, Is.SameAs(b.Item("wooden_staff")));
            Assert.That(weapon.Displayed, Is.EqualTo(3));
            Assert.That(b.Session.Warehouse.Count(b.Item("copper_sword")), Is.EqualTo(10));
            Assert.That(b.Session.Warehouse.Count(b.Item("wooden_staff")), Is.EqualTo(2));

            Assert.That(b.Session.AssignShelf(weapon.FixtureId, null).Ok, Is.True);
            Assert.That(weapon.Item, Is.Null);
            Assert.That(b.Session.Warehouse.Count(b.Item("wooden_staff")), Is.EqualTo(5));
        }

        [Test]
        public void UT_SHF_04_RemovingShelfReturnsDisplayedToWarehouse()
        {
            var b = new SessionBuilder();
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 10);
            Assert.That(b.Session.RemoveFixture(weapon.FixtureId).Ok, Is.True);
            Assert.That(b.Session.Warehouse.Count(b.Item("copper_sword")), Is.EqualTo(10));
            Assert.That(b.Session.ShelfById(weapon.FixtureId), Is.Null);
        }

        [Test]
        public void UT_SHF_05_AssignFailures()
        {
            var b = new SessionBuilder();
            int weapon = b.Place("shelf_weapon", 0, 3);
            int bargain = b.Place("bargain_table", 5, 3);

            Assert.That(b.Session.AssignShelf(weapon, b.Item("potion")).Reason, Is.EqualTo(FailReason.WrongCategory));
            Assert.That(b.Session.AssignShelf(weapon, b.Item("fairy_salve")).Reason, Is.EqualTo(FailReason.WrongCategory));
            Assert.That(b.Session.AssignShelf(weapon, b.Item("iron_sword")).Reason, Is.EqualTo(FailReason.Locked));
            Assert.That(b.Session.AssignShelf(bargain, b.Item("copper_sword")).Reason, Is.EqualTo(FailReason.WrongCategory));
            Assert.That(b.Session.AssignShelf(bargain, b.Item("fairy_salve")).Reason, Is.EqualTo(FailReason.NotInWarehouse));
            Assert.That(b.Session.AssignShelf(999, b.Item("potion")).Reason, Is.EqualTo(FailReason.NotFound));

            b.Session.DebugAddStock(b.Item("fairy_salve"), 2);
            Assert.That(b.Session.AssignShelf(bargain, b.Item("fairy_salve")).Ok, Is.True);
            Assert.That(b.Session.ShelfById(bargain).Displayed, Is.EqualTo(2));
        }

        [Test]
        public void UT_SHF_06_WeaponShelfSupplies18InFirstMonthAnd15After()
        {
            var b = new SessionBuilder();
            var copper = b.Item("copper_sword");
            var weapon = b.Shelf("shelf_weapon", 0, 3, "copper_sword", 100);
            var warehouse = b.Session.Warehouse;
            Assert.That(warehouse.Count(copper), Is.EqualTo(97));

            // 毎 Tick、並んでいれば 1 個売れる
            for (int i = 0; i < 600; i++)
            {
                weapon.TryTake();
                b.Session.Tick();
            }
            b.Drain();
            // 倉庫から棚に出た数 = 最初に並ぶ 3 個 ＋ 補充 15 個
            Assert.That(100 - warehouse.Count(copper), Is.EqualTo(18));

            int before = warehouse.Count(copper);
            for (int i = 0; i < 600; i++)
            {
                weapon.TryTake();
                b.Session.Tick();
            }
            Assert.That(before - warehouse.Count(copper), Is.EqualTo(15));
        }
    }
}
