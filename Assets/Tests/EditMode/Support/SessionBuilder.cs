using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// 状態を作ってから始めるための手助け（shousaisekkei.md 13 章）。
    /// 来店数 0 の設定で始め、客はテストで中身を決めて直接出す。
    ///
    ///   y4 . . . . . .
    ///   y3 . . . . . .
    ///   y2 . . . . . .
    ///   y1 . . . . C C      カウンター (4, 1)、会計マス (4, 0)
    ///   y0 . . E . P .      入口 (2, 0)
    /// </summary>
    public sealed class SessionBuilder
    {
        public GameSession Session { get; }
        /// <summary>DrainEvents で受け取ったイベントをすべてためる。</summary>
        public List<GameEvent> Events { get; } = new List<GameEvent>();

        public SessionBuilder(Action<TestConfig.Spec> modify = null)
        {
            Session = new GameSession(TestConfig.Step1(s =>
            {
                s.NoVisitors = true;
                modify?.Invoke(s);
            }), 1);
            Drain();
        }

        public GameConfig Config => Session.Config;
        public ItemDef Item(string id) => Config.ItemById(id);
        public JobDef Job(string id) => Config.Jobs.First(j => j.Id == id);

        /// <summary>配置物を置いて、その FixtureId を返す。</summary>
        public int Place(string fixtureId, int x, int y)
        {
            Session.State.AddMoney(1000);
            var r = Session.PlaceFixture(Config.FixtureById(fixtureId), new Tile(x, y));
            Assert.That(r.Ok, Is.True, $"{fixtureId} を ({x}, {y}) に置けない: {r}");
            Drain();
            return Session.Layout.Fixtures.Last().Id;
        }

        /// <summary>棚を置いて、倉庫に stock 個入れてから商品を設定する。</summary>
        public Shelf Shelf(string fixtureId, int x, int y, string itemId, int stock)
        {
            int id = Place(fixtureId, x, y);
            if (stock > 0) Session.DebugAddStock(Item(itemId), stock);
            var r = Session.AssignShelf(id, Item(itemId));
            Assert.That(r.Ok, Is.True, r.ToString());
            Drain();
            return Session.ShelfById(id);
        }

        /// <summary>中身を決めた客を入口に出す。</summary>
        public Customer Spawn(string jobId, int budget, params string[] wantIds)
        {
            var wants = wantIds.Select(id => new Want(Item(id), Item(id).IsRare)).ToList();
            var c = Session.SpawnCustomer(Job(jobId), wants, budget);
            Drain();
            return c;
        }

        public void Tick(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                Session.Tick();
                Drain();
            }
        }

        /// <summary>条件を満たすまで進める。max Tick で満たさなければ失敗。進めた Tick 数を返す。</summary>
        public int TickUntil(Func<bool> done, int max = 2000)
        {
            for (int i = 0; i < max; i++)
            {
                if (done()) return i;
                Tick();
            }
            Assert.Fail($"{max} Tick 進めても条件を満たさなかった");
            return max;
        }

        /// <summary>客が出口に着いて消えるまで進める。</summary>
        public void TickUntilGone(Customer c, int max = 2000) => TickUntil(() => c.State == CustomerState.Gone, max);

        public void Drain() => Events.AddRange(Session.DrainEvents());

        public IEnumerable<T> Of<T>() where T : GameEvent => Events.OfType<T>();
    }
}
