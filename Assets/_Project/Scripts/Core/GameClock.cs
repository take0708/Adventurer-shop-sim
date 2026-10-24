using System;

namespace Shop.Core
{
    /// <summary>1 Tick 進めたときに起きた区切り。</summary>
    [Flags]
    public enum ClockEvents
    {
        None = 0,
        DayChanged = 1 << 0,
        MonthEnded = 1 << 1,
        YearEnded = 1 << 2,
        GameEnded = 1 << 3,
    }

    /// <summary>
    /// ゲーム内の時間（FR-TIME, kihonsekkei.md 2 章）。
    /// 倍速や一時停止は持たない。呼び出し側が Advance を呼ぶ回数で決める。
    /// </summary>
    public sealed class GameClock
    {
        readonly TimeConfig _config;

        /// <summary>開始からの経過 Tick。</summary>
        public int ElapsedTicks { get; private set; }

        public bool IsFinished => ElapsedTicks >= _config.TotalTicks;

        /// <summary>今の月（1 始まり、通算）。終了後は最終月のまま。</summary>
        public int Month => Math.Min(ElapsedTicks / _config.TicksPerMonth, _config.TotalMonths - 1) + 1;

        /// <summary>今の年（1 始まり）。</summary>
        public int Year => (Month - 1) / _config.MonthsPerYear + 1;

        /// <summary>年の中の月（1〜12）。</summary>
        public int MonthOfYear => (Month - 1) % _config.MonthsPerYear + 1;

        /// <summary>月の中の日（1 始まり）。終了後は最終日のまま。</summary>
        public int Day => IsFinished ? _config.DaysPerMonth : TickInMonth / _config.TicksPerDay + 1;

        /// <summary>月の中の Tick（0 始まり）。</summary>
        public int TickInMonth => IsFinished ? _config.TicksPerMonth - 1 : ElapsedTicks % _config.TicksPerMonth;

        /// <summary>今の月の進み具合（0〜1）。HUD のバー用。</summary>
        public float MonthProgress => IsFinished ? 1f : (float)TickInMonth / _config.TicksPerMonth;

        public GameClock(TimeConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>1 Tick 進める。終了後は何もしない。</summary>
        public ClockEvents Advance()
        {
            if (IsFinished) return ClockEvents.None;

            ElapsedTicks++;
            var events = ClockEvents.None;

            if (ElapsedTicks % _config.TicksPerDay == 0) events |= ClockEvents.DayChanged;

            if (ElapsedTicks % _config.TicksPerMonth == 0)
            {
                events |= ClockEvents.MonthEnded;
                int endedMonth = ElapsedTicks / _config.TicksPerMonth;
                if (endedMonth % _config.MonthsPerYear == 0) events |= ClockEvents.YearEnded;
                if (endedMonth == _config.TotalMonths) events |= ClockEvents.GameEnded;
            }

            return events;
        }
    }
}
