using System;

namespace Shop.Core
{
    public enum FixtureKind { Shelf, Counter, Decor }

    /// <summary>配置物の種類（youken.md 5.5、kihonsekkei.md 4.2。shousaisekkei.md 3.1）。</summary>
    public sealed class FixtureDef
    {
        public string Id { get; }
        public string Name { get; }
        public FixtureKind Kind { get; }
        /// <summary>棚に並べる系統。Kind == Shelf のときだけ意味がある。特売台は Rare。</summary>
        public ItemCategory ShelfCategory { get; }
        public int Cost { get; }
        public int Width { get; }
        public int Height { get; }
        /// <summary>上を歩けるか。絨毯だけ true。</summary>
        public bool Walkable { get; }
        /// <summary>棚: 並ぶ数。</summary>
        public int DisplayMax { get; }
        /// <summary>棚: 補充の間隔（Tick）。</summary>
        public int RestockTicks { get; }
        /// <summary>装飾: 雰囲気値。</summary>
        public int Ambience { get; }
        /// <summary>0 = 広さの段階の上限に従う / 1 以上 = その数まで（特売台 1）。</summary>
        public int MaxCount { get; }
        /// <summary>広さの段階の「棚の上限」に数えるか。武器・防具・道具棚 true、特売台 false。</summary>
        public bool CountsTowardShelfLimit { get; }
        /// <summary>開始時の無料枠を使えるか。武器・防具・道具棚 true。</summary>
        public bool UsesFreeShelf { get; }

        public bool IsBargainTable => Kind == FixtureKind.Shelf && ShelfCategory == ItemCategory.Rare;

        public FixtureDef(string id, string name, FixtureKind kind, ItemCategory shelfCategory, int cost,
            int width, int height, bool walkable, int displayMax, int restockTicks, int ambience,
            int maxCount, bool countsTowardShelfLimit, bool usesFreeShelf)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Kind = kind;
            ShelfCategory = shelfCategory;
            Cost = cost;
            Width = width;
            Height = height;
            Walkable = walkable;
            DisplayMax = displayMax;
            RestockTicks = restockTicks;
            Ambience = ambience;
            MaxCount = maxCount;
            CountsTowardShelfLimit = countsTowardShelfLimit;
            UsesFreeShelf = usesFreeShelf;
        }

        public override string ToString() => Id;
    }
}
