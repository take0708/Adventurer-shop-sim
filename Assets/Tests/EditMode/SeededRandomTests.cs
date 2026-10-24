using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 1000; i++)
                Assert.That(a.Range(0, 1000), Is.EqualTo(b.Range(0, 1000)));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
                if (a.Range(0, 1000) == b.Range(0, 1000)) same++;
            Assert.That(same, Is.LessThan(10));
        }

        [Test]
        public void Range_StaysWithinBounds_AndHitsBothEnds()
        {
            var r = new SeededRandom(7);
            bool hitMin = false, hitMax = false;
            for (int i = 0; i < 10000; i++)
            {
                int v = r.Range(40, 131); // ★1 の予算 40〜130G
                Assert.That(v, Is.InRange(40, 130));
                if (v == 40) hitMin = true;
                if (v == 130) hitMax = true;
            }
            Assert.That(hitMin && hitMax, Is.True);
        }

        [Test]
        public void NextDouble_IsInZeroToOne_AndRoughlyUniform()
        {
            var r = new SeededRandom(123);
            int below = 0;
            const int n = 10000;
            for (int i = 0; i < n; i++)
            {
                double v = r.NextDouble();
                Assert.That(v, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                if (v < 0.2) below++; // 行商人 ★1 の出現確率 20%
            }
            Assert.That(below / (double)n, Is.EqualTo(0.2).Within(0.02));
        }
    }
}
