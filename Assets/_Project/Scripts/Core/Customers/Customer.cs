using System.Collections.Generic;

namespace Shop.Core
{
    public enum CustomerState
    {
        /// <summary>街の道から入口へ（WalkTicksPerTile の間）。</summary>
        Entering,
        /// <summary>次の欲しいものの棚へ歩く。</summary>
        ToShelf,
        /// <summary>棚の前で手に取る（PickTicks）。</summary>
        Picking,
        /// <summary>しょんぼり / 「高い…」（EmoteTicks）。</summary>
        Disappointed,
        /// <summary>置いていない品を探して、うろうろ。</summary>
        Wandering,
        /// <summary>カウンターの行列へ歩く。</summary>
        ToQueue,
        /// <summary>行列で待つ。</summary>
        Queuing,
        /// <summary>会計中（Counter が進める）。</summary>
        Paying,
        /// <summary>出口（入口マス）へ歩く。</summary>
        Leaving,
        /// <summary>出口に着いた。この Tick の終わりに消す。</summary>
        Gone,
    }

    public enum CustomerResult { Full, Partial, None, QueueLeft }

    public enum WantStatus { Pending, InBasket, Failed }

    public enum FailKind { None, OutOfStock, TooExpensive, NotCarried, GaveUp }

    /// <summary>客の欲しいもの 1 つ（shousaisekkei.md 8.2）。</summary>
    public sealed class Want
    {
        public ItemDef Item { get; }
        /// <summary>掘り出し物（特売台の限定品）。結果には数えない（K-04）。</summary>
        public bool IsRare { get; }
        public WantStatus Status { get; internal set; }
        public FailKind Fail { get; internal set; }

        internal Want(ItemDef item, bool isRare)
        {
            Item = item;
            IsRare = isRare;
        }

        internal void MarkFailed(FailKind kind)
        {
            Status = WantStatus.Failed;
            Fail = kind;
        }
    }

    /// <summary>店内の客 1 人（kihonsekkei.md 5.3、shousaisekkei.md 8.3）。状態は CustomerBrain が進める。</summary>
    public sealed class Customer
    {
        readonly List<Want> _wants;
        internal readonly List<(ItemDef Item, int ShelfId)> BasketList = new List<(ItemDef Item, int ShelfId)>();
        internal readonly Queue<Tile> Path = new Queue<Tile>();

        public int Id { get; }
        public JobDef Job { get; }
        public IReadOnlyList<Want> Wants => _wants;
        /// <summary>残りの予算。手に取った時点で引く。</summary>
        public int Budget { get; internal set; }
        public IReadOnlyList<(ItemDef Item, int ShelfId)> Basket => BasketList;
        public CustomerState State { get; internal set; }
        /// <summary>今いるマス（歩いている途中は出発したマス）。</summary>
        public Tile Tile { get; internal set; }
        /// <summary>結果が決まったら入る。</summary>
        public CustomerResult? Result { get; internal set; }
        /// <summary>来店からの経過 Tick。</summary>
        public int StayTicks { get; internal set; }

        // CustomerBrain が使う内部の状態
        internal int StepTimer;
        internal Tile NextTile;
        internal int StateTimer;
        internal Want CurrentWant;
        internal int TargetShelfId;
        internal Tile Dest;
        internal int CounterId;
        internal int QueueWaitTicks;
        internal bool Retargeted;

        /// <summary>1 マスの移動の途中か、まだ歩く経路が残っている。</summary>
        public bool IsWalking => StepTimer > 0 || Path.Count > 0;

        internal Customer(int id, JobDef job, List<Want> wants, int budget, Tile tile)
        {
            Id = id;
            Job = job;
            _wants = wants;
            Budget = budget;
            Tile = tile;
            State = CustomerState.Entering;
        }

        public override string ToString() => $"客#{Id} {Job.Id} {State} {Tile}";
    }
}
