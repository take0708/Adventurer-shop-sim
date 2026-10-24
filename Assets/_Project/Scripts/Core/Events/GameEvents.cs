namespace Shop.Core
{
    /// <summary>Core から View / UI へ知らせる出来事（shousaisekkei.md 10.1）。</summary>
    public abstract class GameEvent
    {
        /// <summary>起きた時点の ElapsedTicks。</summary>
        public int Tick { get; }

        protected GameEvent(int tick)
        {
            Tick = tick;
        }
    }

    public enum MoneyReason { Sale, Purchase, Equipment, Refund, Expansion }

    /// <summary>ゲームが始まった。UI はこれを見て配置モードで始める（kihonsekkei.md 4.5）。</summary>
    public sealed class SessionStartedEvent : GameEvent
    {
        public SessionStartedEvent(int tick) : base(tick) { }
    }

    public sealed class DayChangedEvent : GameEvent
    {
        public int Day { get; }
        public int Month { get; }

        public DayChangedEvent(int tick, int day, int month) : base(tick)
        {
            Day = day;
            Month = month;
        }
    }

    public sealed class MoneyChangedEvent : GameEvent
    {
        public int Delta { get; }
        public MoneyReason Reason { get; }
        /// <summary>変わったあとの所持金。</summary>
        public int Money { get; }

        public MoneyChangedEvent(int tick, int delta, MoneyReason reason, int money) : base(tick)
        {
            Delta = delta;
            Reason = reason;
            Money = money;
        }
    }

    /// <summary>配置が変わった。View は全部描き直す。</summary>
    public sealed class LayoutChangedEvent : GameEvent
    {
        public LayoutChangedEvent(int tick) : base(tick) { }
    }

    public sealed class ShopExpandedEvent : GameEvent
    {
        public int Stage { get; }

        public ShopExpandedEvent(int tick, int stage) : base(tick)
        {
            Stage = stage;
        }
    }
}
