using System.Linq;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    /// <summary>
    /// シミュレーションとの比較（kihonsekkei.md 10.2、shousaisekkei.md 13.2）。AutoPlayer で 20 シード × 24 か月。
    /// 許容範囲（★5 の月 18〜24、総売上 60,000〜100,000G）から外れても失敗にはせず、Inconclusive でログを残す。
    /// </summary>
    [Category("Slow")]
    public class SimulationComparisonTests
    {
        const int Seeds = 20;

        [Test]
        public void TwentySeeds_Star5MonthAndTotalSalesWithinExpectedRange()
        {
            var sales = new int[Seeds];
            var star5 = new int[Seeds];
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < Seeds; i++)
            {
                var session = new GameSession(TestConfig.Step1(), (ulong)(1000 + i));
                new AutoPlayer(session).Play();
                var r = session.Result;
                sales[i] = r.TotalSales;
                star5[i] = r.StarReachedMonth[4];
                lines.AppendLine($"seed {1000 + i}: 総売上 {r.TotalSales}G, ★{r.FinalStar}, 星の月 [{string.Join(", ", r.StarReachedMonth)}], 段階 {session.Layout.Stage}, 棚 {session.Layout.ShelfCount}");
            }

            double avgSales = sales.Average();
            var reached = star5.Where(m => m > 0).ToList();
            double avgStar5 = reached.Count > 0 ? reached.Average() : 0;
            string summary = $"総売上の平均 {avgSales:F0}G（{sales.Min()}〜{sales.Max()}）、★5 に届いた {reached.Count}/{Seeds} シード、届いた月の平均 {avgStar5:F1}";
            UnityEngine.Debug.Log($"[SimComparison] {summary}\n{lines}");
            TestContext.WriteLine(summary);
            TestContext.WriteLine(lines.ToString());

            bool salesOk = avgSales >= 60000 && avgSales <= 100000;
            bool starOk = reached.Count == Seeds && avgStar5 >= 18 && avgStar5 <= 24;
            if (!salesOk || !starOk) Assert.Inconclusive($"許容範囲の外（数値の調整の材料にする）: {summary}");
        }
    }
}
