namespace Shop.Core
{
    /// <summary>店の広さの段階（youken.md 5.6。shousaisekkei.md 3.1）。</summary>
    public sealed class ShopSizeDef
    {
        /// <summary>1〜3。</summary>
        public int Stage { get; }
        public int Width { get; }
        public int Height { get; }
        public int ShelfMax { get; }
        public int CounterMax { get; }
        /// <summary>この段階に広げる費用。段階 1 は 0。</summary>
        public int Cost { get; }

        public ShopSizeDef(int stage, int width, int height, int shelfMax, int counterMax, int cost)
        {
            Stage = stage;
            Width = width;
            Height = height;
            ShelfMax = shelfMax;
            CounterMax = counterMax;
            Cost = cost;
        }
    }
}
