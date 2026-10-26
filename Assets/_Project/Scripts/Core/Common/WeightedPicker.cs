using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>重みつき抽選（shousaisekkei.md 8.2）。</summary>
    internal static class WeightedPicker
    {
        /// <summary>重みの合計 S に対して r = Range(0, S)、累積で最初に r &lt; 累積となる品。空なら default。</summary>
        public static T Pick<T>(IReadOnlyList<(T Item, int Weight)> candidates, IRandom random)
        {
            int total = 0;
            foreach (var c in candidates) total += c.Weight;
            if (total <= 0) return default;
            int r = random.Range(0, total);
            int acc = 0;
            foreach (var c in candidates)
            {
                acc += c.Weight;
                if (r < acc) return c.Item;
            }
            throw new InvalidOperationException("到達しない");
        }
    }
}
