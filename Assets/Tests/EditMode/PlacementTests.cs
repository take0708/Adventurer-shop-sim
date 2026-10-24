using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// 配置（shousaisekkei.md 6.3, 6.4、13.1 UT-LAY）。
    /// 段階 1 は 6×5、入口 (2, 0)、初期のカウンター (4, 1)〜(5, 1)、会計マス (4, 0)。
    ///
    ///   y4 . . . . . .
    ///   y3 . . . . . .
    ///   y2 . . . . . .
    ///   y1 . . . . C C
    ///   y0 . . E . P .
    /// </summary>
    public class PlacementTests
    {
        GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = new GameSession(TestConfig.Step1(), 1);
            _session.DrainEvents();
        }

        FixtureDef Def(string id) => _session.Config.FixtureById(id);
        CommandResult Place(string id, int x, int y) => _session.PlaceFixture(Def(id), new Tile(x, y));
        static Tile T(int x, int y) => new Tile(x, y);

        void AssertFail(CommandResult r, FailReason reason) => Assert.That(r.Reason, Is.EqualTo(reason));

        /// <summary>失敗したら状態もイベントも変わらない（shousaisekkei.md 10.2）。</summary>
        void AssertFailWithoutChange(System.Func<CommandResult> command, FailReason reason)
        {
            int money = _session.State.Money;
            int free = _session.State.FreeShelvesLeft;
            int version = _session.Layout.Version;
            int count = _session.Layout.Fixtures.Count;
            _session.DrainEvents();

            AssertFail(command(), reason);

            Assert.That(_session.State.Money, Is.EqualTo(money));
            Assert.That(_session.State.FreeShelvesLeft, Is.EqualTo(free));
            Assert.That(_session.Layout.Version, Is.EqualTo(version));
            Assert.That(_session.Layout.Fixtures.Count, Is.EqualTo(count));
            Assert.That(_session.DrainEvents(), Is.Empty);
        }

        [Test]
        public void StartsWithCounterAnd500GAnd3FreeShelves()
        {
            var session = new GameSession(TestConfig.Step1(), 1);
            Assert.That(session.State.Money, Is.EqualTo(500));
            Assert.That(session.State.FreeShelvesLeft, Is.EqualTo(3));
            Assert.That(session.Layout.CounterCount, Is.EqualTo(1));
            Assert.That(session.Layout.Fixtures[0].PaidCost, Is.EqualTo(0));
            Assert.That(session.Layout.Fixtures[0].PayTile, Is.EqualTo(T(4, 0)));
            Assert.That(session.DrainEvents().Single(), Is.InstanceOf<SessionStartedEvent>());
        }

        [Test]
        public void UT_LAY_01_OutOfBounds_Entrance_Occupied()
        {
            AssertFailWithoutChange(() => Place("shelf_tool", 6, 0), FailReason.OutOfBounds);
            AssertFailWithoutChange(() => Place("shelf_tool", -1, 2), FailReason.OutOfBounds);
            AssertFailWithoutChange(() => Place("counter", 5, 3), FailReason.OutOfBounds);
            AssertFailWithoutChange(() => Place("rug", 4, 4), FailReason.OutOfBounds);
            AssertFailWithoutChange(() => Place("shelf_tool", 2, 0), FailReason.BlocksEntrance);
            AssertFailWithoutChange(() => Place("rug", 1, 0), FailReason.BlocksEntrance);
            AssertFailWithoutChange(() => Place("shelf_tool", 5, 1), FailReason.Occupied);

            // 絨毯の上には置けず、絨毯も他の物に重ねられない
            Assert.That(Place("rug", 0, 2).Ok, Is.True);
            AssertFailWithoutChange(() => Place("shelf_tool", 1, 3), FailReason.Occupied);
            AssertFailWithoutChange(() => Place("rug", 1, 3), FailReason.Occupied);
        }

        [Test]
        public void UT_LAY_02_CounterNeedsPayTile()
        {
            AssertFailWithoutChange(() => Place("counter", 0, 0), FailReason.NoPayTile);
            // 会計マスが入口
            AssertFailWithoutChange(() => Place("counter", 2, 1), FailReason.NoPayTile);
            // 会計マスに通れない物
            Assert.That(Place("plant", 0, 2).Ok, Is.True);
            AssertFailWithoutChange(() => Place("counter", 0, 3), FailReason.NoPayTile);
            // 会計マスが絨毯なら置ける
            Assert.That(Place("rug", 2, 2).Ok, Is.True);
            Assert.That(Place("counter", 2, 4).Ok, Is.True);
        }

        [Test]
        public void UT_LAY_03_ShelfAndBargainTableLimits()
        {
            for (int x = 0; x < 6; x++) Assert.That(Place("shelf_weapon", x, 4).Ok, Is.True, $"x={x}");
            Assert.That(_session.Layout.ShelfCount, Is.EqualTo(6));
            AssertFailWithoutChange(() => Place("shelf_tool", 0, 2), FailReason.LimitReached);

            // 特売台は棚の上限に数えないが、1 台まで
            Assert.That(Place("bargain_table", 0, 2).Ok, Is.True);
            Assert.That(_session.Layout.ShelfCount, Is.EqualTo(6));
            AssertFailWithoutChange(() => Place("bargain_table", 1, 2), FailReason.LimitReached);
        }

        [Test]
        public void UT_LAY_03_CounterLimit()
        {
            _session.State.AddMoney(1000);
            Assert.That(Place("counter", 0, 2).Ok, Is.True);
            AssertFailWithoutChange(() => Place("counter", 0, 4), FailReason.LimitReached);
        }

        [Test]
        public void UT_LAY_04_NotEnoughMoney_FreeShelvesWorkWithZeroMoney()
        {
            // 200 + 300 で所持金をちょうど 0 にする
            Assert.That(Place("bargain_table", 0, 4).Ok, Is.True);
            Assert.That(Place("armor_stand", 1, 4).Ok, Is.True);
            Assert.That(_session.State.Money, Is.EqualTo(0));
            AssertFailWithoutChange(() => Place("plant", 0, 2), FailReason.NotEnoughMoney);

            // 無料枠が残っていれば所持金 0 でも棚を置ける
            Assert.That(_session.PlacementCost(Def("shelf_weapon")), Is.EqualTo(0));
            for (int x = 3; x <= 5; x++) Assert.That(Place("shelf_weapon", x, 4).Ok, Is.True, $"x={x}");
            Assert.That(_session.State.Money, Is.EqualTo(0));
            Assert.That(_session.State.FreeShelvesLeft, Is.EqualTo(0));

            Assert.That(_session.PlacementCost(Def("shelf_weapon")), Is.EqualTo(100));
            AssertFailWithoutChange(() => Place("shelf_weapon", 0, 2), FailReason.NotEnoughMoney);
        }

        [Test]
        public void UT_LAY_05_BlocksPath()
        {
            // 会計マス (4, 0) を孤立させる
            AssertFailWithoutChange(() => Place("plant", 3, 0), FailReason.BlocksPath);

            // 棚の 4 方向をふさぐ（角の棚は 2 方向）
            Assert.That(Place("shelf_tool", 0, 4).Ok, Is.True);
            Assert.That(Place("plant", 1, 4).Ok, Is.True);
            AssertFailWithoutChange(() => Place("plant", 0, 3), FailReason.BlocksPath);

            // 入口を囲む。(3, 0) をふさげるよう、先にカウンターを (0, 2) へ動かす
            Assert.That(_session.MoveFixture(_session.Layout.Fixtures[0].Id, T(0, 2)).Ok, Is.True);
            Assert.That(Place("plant", 1, 0).Ok, Is.True);
            Assert.That(Place("plant", 3, 0).Ok, Is.True);
            AssertFailWithoutChange(() => Place("plant", 2, 1), FailReason.BlocksPath);
        }

        [Test]
        public void UT_LAY_05_MoveCanBlockPath()
        {
            Assert.That(Place("plant", 0, 2).Ok, Is.True);
            int plantId = _session.Layout.Fixtures.Last().Id;
            AssertFailWithoutChange(() => _session.MoveFixture(plantId, T(3, 0)), FailReason.BlocksPath);
        }

        [Test]
        public void UT_LAY_06_LastCounterCannotBeRemoved()
        {
            int first = _session.Layout.Fixtures[0].Id;
            AssertFailWithoutChange(() => _session.RemoveFixture(first), FailReason.LastCounter);

            Assert.That(Place("counter", 0, 2).Ok, Is.True);
            Assert.That(_session.RemoveFixture(first).Ok, Is.True);
            Assert.That(_session.Layout.CounterCount, Is.EqualTo(1));
            int second = _session.Layout.Fixtures.Single(f => f.Def.Kind == FixtureKind.Counter).Id;
            AssertFailWithoutChange(() => _session.RemoveFixture(second), FailReason.LastCounter);
        }

        [Test]
        public void UT_LAY_07_RefundIsFullPaidCost_FreeShelfRefundsZero()
        {
            Assert.That(Place("plant", 0, 2).Ok, Is.True);
            Assert.That(_session.State.Money, Is.EqualTo(420));
            int plantId = _session.Layout.Fixtures.Last().Id;
            _session.DrainEvents();

            Assert.That(_session.RefundOf(plantId), Is.EqualTo(80));
            Assert.That(_session.RemoveFixture(plantId).Ok, Is.True);
            Assert.That(_session.State.Money, Is.EqualTo(500));
            var money = _session.DrainEvents().OfType<MoneyChangedEvent>().Single();
            Assert.That(money.Delta, Is.EqualTo(80));
            Assert.That(money.Reason, Is.EqualTo(MoneyReason.Refund));

            // 無料枠の棚は 0G 戻る。無料枠の残りは戻らない
            Assert.That(Place("shelf_tool", 0, 2).Ok, Is.True);
            Assert.That(_session.State.FreeShelvesLeft, Is.EqualTo(2));
            int shelfId = _session.Layout.Fixtures.Last().Id;
            _session.DrainEvents();
            Assert.That(_session.RemoveFixture(shelfId).Ok, Is.True);
            Assert.That(_session.State.Money, Is.EqualTo(500));
            Assert.That(_session.State.FreeShelvesLeft, Is.EqualTo(2));
            Assert.That(_session.DrainEvents().OfType<MoneyChangedEvent>(), Is.Empty);
        }

        [Test]
        public void UT_LAY_08_ExpandKeepsCoordinates_UntilMaxStage()
        {
            AssertFailWithoutChange(() => _session.ExpandShop(), FailReason.NotEnoughMoney);

            _session.State.AddMoney(10000);
            var counter = _session.Layout.Fixtures[0];
            Assert.That(_session.ExpandShop().Ok, Is.True);
            Assert.That(_session.Layout.Stage, Is.EqualTo(2));
            Assert.That(_session.Layout.Width, Is.EqualTo(8));
            Assert.That(_session.Layout.Height, Is.EqualTo(6));
            Assert.That(_session.State.Money, Is.EqualTo(10500 - 1500));
            Assert.That(counter.Origin, Is.EqualTo(T(4, 1)));
            Assert.That(_session.Layout.FixtureAt(T(5, 1)), Is.SameAs(counter));
            Assert.That(_session.Layout.Entrance, Is.EqualTo(T(2, 0)));

            var events = _session.DrainEvents();
            Assert.That(events.OfType<ShopExpandedEvent>().Single().Stage, Is.EqualTo(2));
            Assert.That(events.OfType<LayoutChangedEvent>().Count(), Is.EqualTo(1));

            // 広がったところに置ける
            Assert.That(Place("shelf_tool", 7, 5).Ok, Is.True);

            Assert.That(_session.ExpandShop().Ok, Is.True);
            Assert.That(_session.Layout.Stage, Is.EqualTo(3));
            Assert.That(_session.Layout.Width, Is.EqualTo(10));
            AssertFailWithoutChange(() => _session.ExpandShop(), FailReason.MaxStage);
        }

        [Test]
        public void MoveFixture_SamePlaceAndOverlappingItself()
        {
            Assert.That(Place("rug", 0, 2).Ok, Is.True);
            int rugId = _session.Layout.Fixtures.Last().Id;
            _session.DrainEvents();

            Assert.That(_session.MoveFixture(rugId, T(0, 2)).Ok, Is.True);
            Assert.That(_session.DrainEvents(), Is.Empty);

            // 自分が今いるマスと重なる移動はできる。移動は無料
            Assert.That(_session.MoveFixture(rugId, T(1, 3)).Ok, Is.True);
            Assert.That(_session.State.Money, Is.EqualTo(350));
            Assert.That(_session.Layout.FixtureAt(T(0, 2)), Is.Null);
            Assert.That(_session.Layout.FixtureAt(T(2, 4)).Id, Is.EqualTo(rugId));

            AssertFailWithoutChange(() => _session.MoveFixture(999, T(0, 0)), FailReason.NotFound);
            AssertFailWithoutChange(() => _session.MoveFixture(rugId, T(5, 3)), FailReason.OutOfBounds);
            AssertFailWithoutChange(() => _session.MoveFixture(rugId, T(3, 0)), FailReason.Occupied);
        }

        [Test]
        public void AmbienceIsSumOfDecorCappedAtMax()
        {
            _session.State.AddMoney(10000);
            Assert.That(Place("armor_stand", 0, 4).Ok, Is.True);
            Assert.That(Place("rug", 0, 1).Ok, Is.True);
            Assert.That(Place("plant", 5, 4).Ok, Is.True);
            Assert.That(_session.Layout.Ambience, Is.EqualTo(7));
            for (int x = 1; x <= 4; x++) Assert.That(Place("armor_stand", x, 4).Ok, Is.True, $"x={x}");
            Assert.That(_session.Layout.Ambience, Is.EqualTo(20));
        }
    }
}
