using System;

namespace Shop.Core
{
    /// <summary>時間の単位（kihonsekkei.md 2.1）。1 Tick = ゲーム内 0.1 秒。</summary>
    public sealed class TimeConfig
    {
        public const float SecondsPerTick = 0.1f;

        public int TicksPerDay { get; }
        public int DaysPerMonth { get; }
        public int MonthsPerYear { get; }
        public int TotalMonths { get; }

        public int TicksPerMonth => TicksPerDay * DaysPerMonth;
        public int TotalTicks => TicksPerMonth * TotalMonths;

        public TimeConfig(int ticksPerDay, int daysPerMonth, int monthsPerYear, int totalMonths)
        {
            if (ticksPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerDay));
            if (daysPerMonth <= 0) throw new ArgumentOutOfRangeException(nameof(daysPerMonth));
            if (monthsPerYear <= 0) throw new ArgumentOutOfRangeException(nameof(monthsPerYear));
            if (totalMonths <= 0) throw new ArgumentOutOfRangeException(nameof(totalMonths));
            TicksPerDay = ticksPerDay;
            DaysPerMonth = daysPerMonth;
            MonthsPerYear = monthsPerYear;
            TotalMonths = totalMonths;
        }

        /// <summary>Step 1 の初期値: 1 日 20 Tick、1 か月 30 日、12 か月で 1 年、24 か月で終了。</summary>
        public static TimeConfig Step1Default => new TimeConfig(20, 30, 12, 24);
    }
}
