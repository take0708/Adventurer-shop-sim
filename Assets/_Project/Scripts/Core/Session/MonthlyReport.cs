using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// 月の集計（月末収支 SC-08、shousaisekkei.md 9.3）。起きたときに足していく（K-10）。
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

    /// <summary>年末審査の講評（kihonsekkei.md 7.4）。数値上の効果はない。文言は UI 側。</summary>
    public enum ReviewGrade { Steady, Good, Excellent }

    /// <summary>1 年の記録（年末審査 SC-10、shousaisekkei.md 9.3）。</summary>
    public sealed class YearRecord
    {
        /// <summary>1 か 2。</summary>
        public int Year { get; }
        /// <summary>年末の評判 − 年初の評判。</summary>
        public int RepGained { get; }
        public int Sales { get; }
        /// <summary>年末の判定のあとの星。</summary>
        public int Star { get; }
        public ReviewGrade Grade { get; }

        internal YearRecord(int year, int repGained, int sales, int star, ReviewGrade grade)
        {
            Year = year;
            RepGained = repGained;
            Sales = sales;
            Star = star;
            Grade = grade;
        }
    }

    /// <summary>2 年が終わったときの結果（結果発表 SC-11、shousaisekkei.md 9.3）。</summary>
    public sealed class GameResult
    {
        /// <summary>スコア。会計の売上の合計（払い戻しを含まない）。</summary>
        public int TotalSales { get; }
        public int FinalStar { get; }
        public int FinalReputation { get; }
        /// <summary>各星の客が来始めた月（0 は届かなかった）。</summary>
        public IReadOnlyList<int> StarReachedMonth { get; }
        public IReadOnlyList<YearRecord> Years { get; }

        internal GameResult(int totalSales, int finalStar, int finalReputation, IReadOnlyList<int> starReachedMonth, IReadOnlyList<YearRecord> years)
        {
            TotalSales = totalSales;
            FinalStar = finalStar;
            FinalReputation = finalReputation;
            StarReachedMonth = new List<int>(starReachedMonth);
            Years = new List<YearRecord>(years);
        }
    }

    /// <summary>
    /// スコアの送信（shousaisekkei.md 9.4）。Core は置くだけで呼ばない。結果発表の画面が TotalSales を渡して呼ぶ。
    /// </summary>
    public interface IScoreSubmitter
    {
        /// <summary>成功・失敗をコールバックで返す（WebGL は非同期の通信になるため）。</summary>
        void Submit(int score, System.Action<bool> onDone);
    }
}
