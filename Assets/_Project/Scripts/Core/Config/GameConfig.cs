using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>youken.md 5 章と kihonsekkei.md で決めた、表にならない数値（shousaisekkei.md 3.2）。</summary>
    public sealed class BalanceDef
    {
        public int StartMoney { get; }
        public int RepFull { get; }
        public int RepPartial { get; }
        public int RepNone { get; }
        public int RepQueueLeft { get; }
        /// <summary>雰囲気 1 あたりの来店数の増加（千分率。15 = 1.5%）。</summary>
        public int AmbiencePermille { get; }
        public int AmbienceMax { get; }
        public int TwoWantsChancePercent { get; }
        public int CheckoutTicks { get; }
        public int QueuePatienceTicks { get; }
        public int WalkTicksPerTile { get; }
        public int PickTicks { get; }
        public int EmoteTicks { get; }
        public int MaxCustomerStayTicks { get; }
        /// <summary>これ未満の Tick に来店させる。</summary>
        public int SpawnWindowEndTick { get; }
        public int MerchantArriveMinTick { get; }
        /// <summary>含まない。</summary>
        public int MerchantArriveMaxTick { get; }
        public int MerchantStayTicks { get; }
        public int MerchantMaxBuy { get; }
        public int RefundPercent { get; }
        public int FreeShelfCount { get; }
        public Tile Entrance { get; }
        /// <summary>開始時に無料で置く配置物（カウンター 1 台）。</summary>
        public IReadOnlyList<(FixtureDef Def, Tile Origin)> InitialLayout { get; }
        /// <summary>年末審査の講評の境目（年ごと）。その年に増えた評判がこれ以上なら Good。</summary>
        public IReadOnlyList<int> ReviewGoodRep { get; }
        /// <summary>これ以上なら Excellent。</summary>
        public IReadOnlyList<int> ReviewExcellentRep { get; }

        public BalanceDef(
            int startMoney,
            int repFull, int repPartial, int repNone, int repQueueLeft,
            int ambiencePermille, int ambienceMax,
            int twoWantsChancePercent,
            int checkoutTicks, int queuePatienceTicks, int walkTicksPerTile, int pickTicks, int emoteTicks,
            int maxCustomerStayTicks, int spawnWindowEndTick,
            int merchantArriveMinTick, int merchantArriveMaxTick, int merchantStayTicks, int merchantMaxBuy,
            int refundPercent, int freeShelfCount,
            Tile entrance, IEnumerable<(FixtureDef Def, Tile Origin)> initialLayout,
            IEnumerable<int> reviewGoodRep, IEnumerable<int> reviewExcellentRep)
        {
            StartMoney = startMoney;
            RepFull = repFull;
            RepPartial = repPartial;
            RepNone = repNone;
            RepQueueLeft = repQueueLeft;
            AmbiencePermille = ambiencePermille;
            AmbienceMax = ambienceMax;
            TwoWantsChancePercent = twoWantsChancePercent;
            CheckoutTicks = checkoutTicks;
            QueuePatienceTicks = queuePatienceTicks;
            WalkTicksPerTile = walkTicksPerTile;
            PickTicks = pickTicks;
            EmoteTicks = emoteTicks;
            MaxCustomerStayTicks = maxCustomerStayTicks;
            SpawnWindowEndTick = spawnWindowEndTick;
            MerchantArriveMinTick = merchantArriveMinTick;
            MerchantArriveMaxTick = merchantArriveMaxTick;
            MerchantStayTicks = merchantStayTicks;
            MerchantMaxBuy = merchantMaxBuy;
            RefundPercent = refundPercent;
            FreeShelfCount = freeShelfCount;
            Entrance = entrance;
            InitialLayout = new List<(FixtureDef Def, Tile Origin)>(initialLayout ?? throw new ArgumentNullException(nameof(initialLayout)));
            ReviewGoodRep = new List<int>(reviewGoodRep ?? throw new ArgumentNullException(nameof(reviewGoodRep)));
            ReviewExcellentRep = new List<int>(reviewExcellentRep ?? throw new ArgumentNullException(nameof(reviewExcellentRep)));
        }
    }

    /// <summary>
    /// 起動時に Data から作る読み取り専用の設定（kihonsekkei.md 8.1、shousaisekkei.md 3.2）。
    /// Core はここからだけ数値を読む。作るときに ConfigValidator で食い違いを確かめる。
    /// </summary>
    public sealed class GameConfig
    {
        public TimeConfig Time { get; }
        public BalanceDef Balance { get; }
        /// <summary>問屋の品 → 限定品の順。</summary>
        public IReadOnlyList<ItemDef> Items { get; }
        public IReadOnlyList<JobDef> Jobs { get; }
        /// <summary>[0] が ★1。</summary>
        public IReadOnlyList<StarDef> Stars { get; }
        public IReadOnlyList<FixtureDef> Fixtures { get; }
        /// <summary>[0] が段階 1。</summary>
        public IReadOnlyList<ShopSizeDef> ShopSizes { get; }

        public StarDef StarOf(int star) => Stars[star - 1];
        public ShopSizeDef SizeOf(int stage) => ShopSizes[stage - 1];
        public int MaxStar => Stars.Count;
        public int MaxStage => ShopSizes.Count;

        /// <summary>問題があれば InvalidOperationException（メッセージに全部の問題を並べる）。</summary>
        public GameConfig(TimeConfig time, BalanceDef balance, IEnumerable<ItemDef> items, IEnumerable<JobDef> jobs,
            IEnumerable<StarDef> stars, IEnumerable<FixtureDef> fixtures, IEnumerable<ShopSizeDef> shopSizes)
        {
            Time = time ?? throw new ArgumentNullException(nameof(time));
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            Items = new List<ItemDef>(items ?? throw new ArgumentNullException(nameof(items)));
            Jobs = new List<JobDef>(jobs ?? throw new ArgumentNullException(nameof(jobs)));
            Stars = new List<StarDef>(stars ?? throw new ArgumentNullException(nameof(stars)));
            Fixtures = new List<FixtureDef>(fixtures ?? throw new ArgumentNullException(nameof(fixtures)));
            ShopSizes = new List<ShopSizeDef>(shopSizes ?? throw new ArgumentNullException(nameof(shopSizes)));
            ConfigValidator.Validate(this);
        }

        /// <summary>ID から配置物を引く。なければ null。</summary>
        public FixtureDef FixtureById(string id)
        {
            foreach (var f in Fixtures) if (f.Id == id) return f;
            return null;
        }

        /// <summary>ID から商品を引く。なければ null。</summary>
        public ItemDef ItemById(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }
    }
}
