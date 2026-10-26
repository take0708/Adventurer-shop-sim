using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>客の生成と来店予定（kihonsekkei.md 5.1、shousaisekkei.md 8.1, 8.2、13.1 UT-CUS）。</summary>
    public class CustomerFactoryTests
    {
        const int N = 10000;

        static GameSession NewSession() => new GameSession(TestConfig.Step1(), 12345);

        [Test]
        public void UT_CUS_01_Star1WantsOnlyUnlockedItems()
        {
            var session = NewSession();
            for (int i = 0; i < N; i++)
            {
                var (_, wants, _) = session.Factory.Create(1, session.Shelves);
                Assert.That(wants.Count, Is.InRange(1, 2));
                foreach (var w in wants)
                {
                    Assert.That(w.Item.IsRare, Is.False);
                    Assert.That(w.Item.UnlockStar, Is.LessThanOrEqualTo(1), w.Item.Id);
                }
            }
        }

        [Test]
        public void UT_CUS_02_BudgetWithinStarRangeInclusive([Values(1, 3, 5)] int star)
        {
            var session = NewSession();
            var def = session.Config.StarOf(star);
            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < N; i++)
            {
                var (_, _, budget) = session.Factory.Create(star, session.Shelves);
                Assert.That(budget, Is.InRange(def.BudgetMin, def.BudgetMax));
                if (budget < min) min = budget;
                if (budget > max) max = budget;
            }
            Assert.That(min, Is.EqualTo(def.BudgetMin));
            Assert.That(max, Is.EqualTo(def.BudgetMax));
        }

        [Test]
        public void UT_CUS_03_TwoWantsShareMatches40PercentMinusDuplicates()
        {
            var session = NewSession();
            int two = 0;
            for (int i = 0; i < N; i++)
                if (session.Factory.Create(1, session.Shelves).Wants.Count == 2) two++;

            // ★1 で 2 品が別の品になる確率（職業ごと）: 戦士 0.48、魔法使い 52/81、僧侶 22/36
            double distinct = (0.48 + 52.0 / 81 + 22.0 / 36) / 3;
            double expected = 0.4 * distinct;
            Assert.That((double)two / N, Is.EqualTo(expected).Within(0.02));
        }

        [Test]
        public void UT_CUS_04_VisitsScaleWithAmbienceRounded()
        {
            var config = TestConfig.Step1();
            Assert.That(CustomerSpawner.VisitsFor(config, 1, 0), Is.EqualTo(15));
            Assert.That(CustomerSpawner.VisitsFor(config, 1, 10), Is.EqualTo(17));   // 17.25
            Assert.That(CustomerSpawner.VisitsFor(config, 1, 20), Is.EqualTo(20));   // 19.5 → 20（K-11）
            Assert.That(CustomerSpawner.VisitsFor(config, 1, 30), Is.EqualTo(20));   // 上限 20
            Assert.That(CustomerSpawner.VisitsFor(config, 5, 20), Is.EqualTo(75));   // 75.4
        }

        [Test]
        public void UT_CUS_05_SpawnTimesWithinWindow()
        {
            var session = NewSession();
            var spawner = session.Spawner;
            for (int i = 0; i < 1000; i++)
            {
                spawner.PlanMonth(5, 20);
                Assert.That(spawner.PlannedCount, Is.EqualTo(75));
                Assert.That(spawner.Plan.All(t => t >= 0 && t < 570), Is.True);
                Assert.That(spawner.Plan, Is.Ordered);
            }
        }

        [Test]
        public void FirstMonthSpawnsPlannedVisitors()
        {
            var session = NewSession();
            int spawned = 0;
            for (int i = 0; i < 600; i++)
            {
                session.Tick();
                spawned += session.DrainEvents().OfType<CustomerSpawnedEvent>().Count();
            }
            Assert.That(spawned, Is.EqualTo(15));
        }
    }
}
