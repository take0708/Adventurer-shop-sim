namespace Shop.Core
{
    /// <summary>
    /// 月の集計（月末収支 SC-08、shousaisekkei.md 9.3）。起きたときに足していく（K-10）。
    /// 評判と星の項目は M5 で入れる。それまでは 0 と ★1。
    /// </summary>
    public sealed class MonthlyReport
    {
        /// <summary>通算 1〜24。</summary>
        public int Month { get; }
        /// <summary>会計の売上。</summary>
        public int Sales { get; private set; }
        /// <summary>問屋＋行商人。</summary>
        public int Purchases { get; private set; }
        /// <summary>配置＋拡張 − 払い戻し（マイナスもありうる）。</summary>
        public int Equipment { get; private set; }
        public int Net => Sales - Purchases - Equipment;
        /// <summary>今月来店した人数。</summary>
        public int Visitors { get; private set; }
        public int Full { get; private set; }
        public int Partial { get; private set; }
        public int None { get; private set; }
        public int QueueLeft { get; private set; }
        public int RareSold { get; private set; }
        public int RepBefore { get; private set; }
        public int RepAfter { get; private set; }
        public int RepDelta => RepAfter - RepBefore;
        /// <summary>月末の判定のあとの星。</summary>
        public int Star { get; private set; }
        public int RepToNextStar { get; private set; }
        /// <summary>締めたあと（MonthEnded を積んだあと）は true。</summary>
        public bool IsClosed { get; private set; }

        internal MonthlyReport(int month, int repBefore)
        {
            Month = month;
            RepBefore = repBefore;
        }

        internal void AddSales(int amount) => Sales += amount;
        internal void AddPurchase(int amount) => Purchases += amount;
        internal void AddEquipment(int amount) => Equipment += amount;
        internal void AddVisitor() => Visitors++;

        internal void AddResult(CustomerResult result)
        {
            switch (result)
            {
                case CustomerResult.Full: Full++; break;
                case CustomerResult.Partial: Partial++; break;
                case CustomerResult.None: None++; break;
                case CustomerResult.QueueLeft: QueueLeft++; break;
            }
        }

        internal void AddRareSold(int count) => RareSold += count;

        internal void Close(int repAfter, int star, int repToNextStar)
        {
            RepAfter = repAfter;
            Star = star;
            RepToNextStar = repToNextStar;
            IsClosed = true;
        }
    }
}
