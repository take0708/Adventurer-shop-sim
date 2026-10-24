using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    public class GameClockTests
    {
        static GameClock NewClock() => new GameClock(TimeConfig.Step1Default);

        static ClockEvents AdvanceTicks(GameClock clock, int ticks)
        {
            var all = ClockEvents.None;
            for (int i = 0; i < ticks; i++) all |= clock.Advance();
            return all;
        }

        [Test]
        public void Step1Default_Is600TicksPerMonthAnd24Months()
        {
            var config = TimeConfig.Step1Default;
            Assert.That(config.TicksPerMonth, Is.EqualTo(600));
            Assert.That(config.TotalTicks, Is.EqualTo(14400));
        }

        [Test]
        public void StartsAtYear1Month1Day1()
        {
            var clock = NewClock();
            Assert.That(clock.Year, Is.EqualTo(1));
            Assert.That(clock.Month, Is.EqualTo(1));
            Assert.That(clock.Day, Is.EqualTo(1));
            Assert.That(clock.IsFinished, Is.False);
        }

        [Test]
        public void DayChangesEvery20Ticks()
        {
            var clock = NewClock();
            Assert.That(AdvanceTicks(clock, 19) & ClockEvents.DayChanged, Is.EqualTo(ClockEvents.None));
            Assert.That(clock.Advance() & ClockEvents.DayChanged, Is.EqualTo(ClockEvents.DayChanged));
            Assert.That(clock.Day, Is.EqualTo(2));
        }

        [Test]
        public void MonthEndsAfter600Ticks()
        {
            var clock = NewClock();
            Assert.That(AdvanceTicks(clock, 599) & ClockEvents.MonthEnded, Is.EqualTo(ClockEvents.None));
            Assert.That(clock.Month, Is.EqualTo(1));
            Assert.That(clock.Day, Is.EqualTo(30));

            var events = clock.Advance();
            Assert.That(events & ClockEvents.MonthEnded, Is.EqualTo(ClockEvents.MonthEnded));
            Assert.That(events & ClockEvents.YearEnded, Is.EqualTo(ClockEvents.None));
            Assert.That(clock.Month, Is.EqualTo(2));
            Assert.That(clock.Day, Is.EqualTo(1));
        }

        [Test]
        public void YearEndsAfterMonth12()
        {
            var clock = NewClock();
            AdvanceTicks(clock, 600 * 12 - 1);
            var events = clock.Advance();
            Assert.That(events & ClockEvents.YearEnded, Is.EqualTo(ClockEvents.YearEnded));
            Assert.That(events & ClockEvents.GameEnded, Is.EqualTo(ClockEvents.None));
            Assert.That(clock.Year, Is.EqualTo(2));
            Assert.That(clock.MonthOfYear, Is.EqualTo(1));
            Assert.That(clock.Month, Is.EqualTo(13));
        }

        [Test]
        public void GameEndsAfter24Months_AndStopsAdvancing()
        {
            var clock = NewClock();
            AdvanceTicks(clock, 14400 - 1);
            Assert.That(clock.IsFinished, Is.False);

            var events = clock.Advance();
            Assert.That(events, Is.EqualTo(ClockEvents.DayChanged | ClockEvents.MonthEnded | ClockEvents.YearEnded | ClockEvents.GameEnded));
            Assert.That(clock.IsFinished, Is.True);

            Assert.That(clock.Advance(), Is.EqualTo(ClockEvents.None));
            Assert.That(clock.ElapsedTicks, Is.EqualTo(14400));
            Assert.That(clock.Month, Is.EqualTo(24));
            Assert.That(clock.Year, Is.EqualTo(2));
            Assert.That(clock.Day, Is.EqualTo(30));
        }

        [Test]
        public void MonthEndedFires24TimesAndYearEndedTwice()
        {
            var clock = NewClock();
            int months = 0, years = 0;
            while (!clock.IsFinished)
            {
                var e = clock.Advance();
                if ((e & ClockEvents.MonthEnded) != 0) months++;
                if ((e & ClockEvents.YearEnded) != 0) years++;
            }
            Assert.That(months, Is.EqualTo(24));
            Assert.That(years, Is.EqualTo(2));
        }
    }
}
