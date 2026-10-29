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

    /// <summary>月が終わった。UI は月末収支を出して時間を止める（D-06）。</summary>
    public sealed class MonthEndedEvent : GameEvent
    {
        public MonthlyReport Report { get; }

        public MonthEndedEvent(int tick, MonthlyReport report) : base(tick)
        {
            Report = report;
        }
    }

    /// <summary>ゲームが始まった。UI はこれを見て配置モードで始める（kihonsekkei.md 4.5）。</summary>
    public sealed class SessionStartedEvent : GameEvent
    {
        public SessionStartedEvent(int tick) : base(tick) { }
    }

    /// <summary>月末の判定で星が上がった。UI は月末収支の OK のあとに演出を出す。</summary>
    public sealed class StarRaisedEvent : GameEvent
    {
        public int NewStar { get; }
        /// <summary>間の星も含めて、解禁された問屋の品。</summary>
        public ItemDef[] Unlocked { get; }

        public StarRaisedEvent(int tick, int newStar, ItemDef[] unlocked) : base(tick)
        {
            NewStar = newStar;
            Unlocked = unlocked;
        }
    }

    public sealed class ReputationChangedEvent : GameEvent
    {
        /// <summary>実際に変わった量（0 で止めたあと）。</summary>
        public int Delta { get; }
        public int Reputation { get; }
        public CustomerResult Result { get; }
        /// <summary>会計した限定品のボーナス（Delta に含まれる）。</summary>
        public int RareBonus { get; }

        public ReputationChangedEvent(int tick, int delta, int reputation, CustomerResult result, int rareBonus) : base(tick)
        {
            Delta = delta;
            Reputation = reputation;
            Result = result;
            RareBonus = rareBonus;
        }
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

    /// <summary>行商人が来た。LeaveTick は月の中の Tick。</summary>
    public sealed class MerchantArrivedEvent : GameEvent
    {
        public ItemDef Item { get; }
        public int LeaveTick { get; }

        public MerchantArrivedEvent(int tick, ItemDef item, int leaveTick) : base(tick)
        {
            Item = item;
            LeaveTick = leaveTick;
        }
    }

    public sealed class MerchantLeftEvent : GameEvent
    {
        public MerchantLeftEvent(int tick) : base(tick) { }
    }

    public sealed class WarehouseChangedEvent : GameEvent
    {
        public ItemDef Item { get; }
        /// <summary>変わったあとの数。</summary>
        public int Count { get; }

        public WarehouseChangedEvent(int tick, ItemDef item, int count) : base(tick)
        {
            Item = item;
            Count = count;
        }
    }

    // ---- 棚・会計 ----

    public sealed class ShelfChangedEvent : GameEvent
    {
        public int FixtureId { get; }
        /// <summary>null = 何も置かない。</summary>
        public ItemDef Item { get; }
        public int Displayed { get; }

        public ShelfChangedEvent(int tick, int fixtureId, ItemDef item, int displayed) : base(tick)
        {
            FixtureId = fixtureId;
            Item = item;
            Displayed = displayed;
        }
    }

    public sealed class ShelfEmptiedEvent : GameEvent
    {
        public int FixtureId { get; }

        public ShelfEmptiedEvent(int tick, int fixtureId) : base(tick)
        {
            FixtureId = fixtureId;
        }
    }

    public sealed class ItemTakenEvent : GameEvent
    {
        public int FixtureId { get; }
        public int CustomerId { get; }
        public ItemDef Item { get; }

        public ItemTakenEvent(int tick, int fixtureId, int customerId, ItemDef item) : base(tick)
        {
            FixtureId = fixtureId;
            CustomerId = customerId;
            Item = item;
        }
    }

    public sealed class QueueChangedEvent : GameEvent
    {
        public int FixtureId { get; }
        /// <summary>並んでいる人数 ＋ 会計中。</summary>
        public int Load { get; }

        public QueueChangedEvent(int tick, int fixtureId, int load) : base(tick)
        {
            FixtureId = fixtureId;
            Load = load;
        }
    }

    public sealed class CheckoutDoneEvent : GameEvent
    {
        public int FixtureId { get; }
        public int CustomerId { get; }
        public int Amount { get; }

        public CheckoutDoneEvent(int tick, int fixtureId, int customerId, int amount) : base(tick)
        {
            FixtureId = fixtureId;
            CustomerId = customerId;
            Amount = amount;
        }
    }

    // ---- 客 ----

    public enum EmoteKind { OutOfStock, TooExpensive, NotCarried, Picked, Treasure }

    public sealed class CustomerSpawnedEvent : GameEvent
    {
        public int CustomerId { get; }
        public JobDef Job { get; }
        public Tile Tile { get; }

        public CustomerSpawnedEvent(int tick, int customerId, JobDef job, Tile tile) : base(tick)
        {
            CustomerId = customerId;
            Job = job;
            Tile = tile;
        }
    }

    /// <summary>From から To へ Ticks かけて歩く。Ticks = 0 はその場で移す（配置替えでどかしたとき）。</summary>
    public sealed class CustomerMovedEvent : GameEvent
    {
        public int CustomerId { get; }
        public Tile From { get; }
        public Tile To { get; }
        public int Ticks { get; }

        public CustomerMovedEvent(int tick, int customerId, Tile from, Tile to, int ticks) : base(tick)
        {
            CustomerId = customerId;
            From = from;
            To = to;
            Ticks = ticks;
        }
    }

    public sealed class CustomerEmoteEvent : GameEvent
    {
        public int CustomerId { get; }
        public EmoteKind Kind { get; }

        public CustomerEmoteEvent(int tick, int customerId, EmoteKind kind) : base(tick)
        {
            CustomerId = customerId;
            Kind = kind;
        }
    }

    /// <summary>結果が決まって、店を出ることになった（出口へ歩き始める）。</summary>
    public sealed class CustomerLeftEvent : GameEvent
    {
        public int CustomerId { get; }
        public CustomerResult Result { get; }
        public ItemDef[] Bought { get; }

        public CustomerLeftEvent(int tick, int customerId, CustomerResult result, ItemDef[] bought) : base(tick)
        {
            CustomerId = customerId;
            Result = result;
            Bought = bought;
        }
    }

    /// <summary>出口に着いて消えた。</summary>
    public sealed class CustomerExitedEvent : GameEvent
    {
        public int CustomerId { get; }

        public CustomerExitedEvent(int tick, int customerId) : base(tick)
        {
            CustomerId = customerId;
        }
    }
}
