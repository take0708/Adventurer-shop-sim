namespace Shop.Core
{
    /// <summary>星の段階ごとの値（youken.md 5.2, 5.4, 5.7。shousaisekkei.md 3.1）。</summary>
    public sealed class StarDef
    {
        /// <summary>1〜5。</summary>
        public int Star { get; }
        public int RequiredRep { get; }
        public int BaseVisits { get; }
        /// <summary>予算の下限（含む）。</summary>
        public int BudgetMin { get; }
        /// <summary>予算の上限（含む）。</summary>
        public int BudgetMax { get; }
        /// <summary>行商人が来る確率。20 = 20%。</summary>
        public int MerchantChancePercent { get; }

        public StarDef(int star, int requiredRep, int baseVisits, int budgetMin, int budgetMax, int merchantChancePercent)
        {
            Star = star;
            RequiredRep = requiredRep;
            BaseVisits = baseVisits;
            BudgetMin = budgetMin;
            BudgetMax = budgetMax;
            MerchantChancePercent = merchantChancePercent;
        }
    }
}
