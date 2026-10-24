using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    public class PathfinderTests
    {
        // 段階 1（6×5）、入口 (2, 0)、カウンター (4, 1)〜(5, 1)
        static GameSession NewSession() => new GameSession(TestConfig.Step1(), 1);

        static void AssertValidPath(ShopLayout layout, Tile from, System.Collections.Generic.IReadOnlyList<Tile> path)
        {
            var prev = from;
            foreach (var t in path)
            {
                Assert.That(System.Math.Abs(t.X - prev.X) + System.Math.Abs(t.Y - prev.Y), Is.EqualTo(1), $"{prev} → {t} が隣でない");
                Assert.That(layout.IsWalkable(t), Is.True, $"{t} が通れない");
                prev = t;
            }
        }

        [Test]
        public void UT_PTH_01_ShortestPathAroundObstacle_AndDeterministic()
        {
            var session = NewSession();
            var plant = session.Config.FixtureById("plant");
            Assert.That(session.PlaceFixture(plant, new Tile(2, 2)).Ok, Is.True);

            var finder = new GridPathfinder(session.Layout);
            var from = new Tile(2, 0);
            var to = new Tile(2, 4);
            var path = finder.FindPath(from, to);

            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count, Is.EqualTo(6));
            Assert.That(path[path.Count - 1], Is.EqualTo(to));
            AssertValidPath(session.Layout, from, path);
            Assert.That(finder.Distance(from, to), Is.EqualTo(6));

            // 同じ長さの経路が複数あっても毎回同じ（上 → 右 → 下 → 左の順）
            var again = new GridPathfinder(session.Layout).FindPath(from, to);
            Assert.That(again, Is.EqualTo(path));
            Assert.That(path[0], Is.EqualTo(new Tile(2, 1)));
            Assert.That(path[1], Is.EqualTo(new Tile(3, 1)));
        }

        [Test]
        public void UT_PTH_01_SameTileIsEmptyPath()
        {
            var finder = new GridPathfinder(NewSession().Layout);
            Assert.That(finder.FindPath(new Tile(1, 1), new Tile(1, 1)), Is.Empty);
            Assert.That(finder.Distance(new Tile(1, 1), new Tile(1, 1)), Is.EqualTo(0));
        }

        [Test]
        public void UT_PTH_02_UnreachableIsNull()
        {
            var session = NewSession();
            var plant = session.Config.FixtureById("plant");
            // 配置の判定を通さずに、左上の角を囲む
            session.Layout.Add(plant, new Tile(1, 4), 0);
            session.Layout.Add(plant, new Tile(0, 3), 0);

            var finder = new GridPathfinder(session.Layout);
            Assert.That(finder.FindPath(new Tile(2, 0), new Tile(0, 4)), Is.Null);
            Assert.That(finder.Distance(new Tile(2, 0), new Tile(0, 4)), Is.EqualTo(int.MaxValue));
            // 通れないマスへの経路もない
            Assert.That(finder.FindPath(new Tile(2, 0), new Tile(4, 1)), Is.Null);
            // 通れないマスからは出られる
            Assert.That(finder.FindPath(new Tile(1, 4), new Tile(2, 4)), Is.Not.Null);
        }

        [Test]
        public void UT_PTH_02_RugIsWalkable()
        {
            var session = NewSession();
            var config = session.Config;
            Assert.That(session.PlaceFixture(config.FixtureById("rug"), new Tile(0, 1)).Ok, Is.True);
            // 絨毯 (0, 1)〜(1, 2) の上を通れば (1, 0) → (0, 3) は 4 歩。通れなければ遠回りになる
            Assert.That(session.PlaceFixture(config.FixtureById("plant"), new Tile(2, 2)).Ok, Is.True);
            Assert.That(session.PlaceFixture(config.FixtureById("plant"), new Tile(1, 3)).Ok, Is.True);

            var finder = new GridPathfinder(session.Layout);
            var from = new Tile(1, 0);
            var path = finder.FindPath(from, new Tile(0, 3));
            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count, Is.EqualTo(4));
            AssertValidPath(session.Layout, from, path);
            Assert.That(session.Layout.IsWalkable(new Tile(1, 2)), Is.True);
        }
    }
}
