using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>置いた配置物 1 つ（shousaisekkei.md 6.1）。</summary>
    public sealed class PlacedFixture
    {
        public int Id { get; }
        public FixtureDef Def { get; }
        /// <summary>左下のマス。</summary>
        public Tile Origin { get; internal set; }
        /// <summary>払い戻しの基準。無料枠・初期配置は 0。</summary>
        public int PaidCost { get; }

        /// <summary>占めるマス。</summary>
        public IEnumerable<Tile> Tiles => ShopLayout.Footprint(Def, Origin);

        /// <summary>カウンターだけ: 会計マス（Origin の 1 つ下）。</summary>
        public Tile? PayTile => ShopLayout.PayTileOf(Def, Origin);

        internal PlacedFixture(int id, FixtureDef def, Tile origin, int paidCost)
        {
            Id = id;
            Def = def;
            Origin = origin;
            PaidCost = paidCost;
        }

        public override string ToString() => $"#{Id} {Def.Id} {Origin}";
    }

    /// <summary>店内のグリッドと配置物（kihonsekkei.md 4 章、shousaisekkei.md 6.1）。</summary>
    public sealed class ShopLayout
    {
        readonly GameConfig _config;
        readonly List<PlacedFixture> _fixtures = new List<PlacedFixture>();
        PlacedFixture[,] _blocking;
        PlacedFixture[,] _floor;
        int _nextId = 1;

        public int Stage { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Tile Entrance { get; }
        /// <summary>Id の小さい順。</summary>
        public IReadOnlyList<PlacedFixture> Fixtures => _fixtures;
        /// <summary>装飾の雰囲気値の合計。AmbienceMax で頭打ち。</summary>
        public int Ambience { get; private set; }
        /// <summary>広さの段階の「棚の上限」に数える配置物の数。</summary>
        public int ShelfCount { get; private set; }
        public int CounterCount { get; private set; }
        /// <summary>変わるたびに +1。経路のキャッシュの無効化に使う。</summary>
        public int Version { get; private set; }

        public ShopSizeDef Size => _config.SizeOf(Stage);

        public ShopLayout(GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Entrance = config.Balance.Entrance;
            SetStage(1);
        }

        /// <summary>def を origin に置いたときに占めるマス。</summary>
        public static IEnumerable<Tile> Footprint(FixtureDef def, Tile origin)
        {
            for (int y = 0; y < def.Height; y++)
                for (int x = 0; x < def.Width; x++)
                    yield return origin.Offset(x, y);
        }

        /// <summary>def を origin に置いたときに tile を占めるか。</summary>
        public static bool Covers(FixtureDef def, Tile origin, Tile tile) =>
            tile.X >= origin.X && tile.X < origin.X + def.Width &&
            tile.Y >= origin.Y && tile.Y < origin.Y + def.Height;

        /// <summary>カウンターの会計マス（左のマスの 1 つ下）。カウンター以外は null。</summary>
        public static Tile? PayTileOf(FixtureDef def, Tile origin) =>
            def.Kind == FixtureKind.Counter ? origin.Offset(0, -1) : (Tile?)null;

        public int CountOf(FixtureDef def)
        {
            int n = 0;
            foreach (var f in _fixtures) if (f.Def == def) n++;
            return n;
        }

        public bool IsInside(Tile t) => t.X >= 0 && t.Y >= 0 && t.X < Width && t.Y < Height;

        /// <summary>内側で、通れない配置物がない。</summary>
        public bool IsWalkable(Tile t) => IsInside(t) && _blocking[t.X, t.Y] == null;

        /// <summary>通れない物を優先。なければ絨毯。なければ null。</summary>
        public PlacedFixture FixtureAt(Tile t)
        {
            if (!IsInside(t)) return null;
            return _blocking[t.X, t.Y] ?? _floor[t.X, t.Y];
        }

        public PlacedFixture FindById(int id)
        {
            foreach (var f in _fixtures) if (f.Id == id) return f;
            return null;
        }

        internal PlacedFixture Add(FixtureDef def, Tile origin, int paidCost)
        {
            var f = new PlacedFixture(_nextId++, def, origin, paidCost);
            _fixtures.Add(f);
            Mark(f, f);
            Recount();
            return f;
        }

        internal void Move(PlacedFixture f, Tile origin)
        {
            Mark(f, null);
            f.Origin = origin;
            Mark(f, f);
            Recount();
        }

        internal void Remove(PlacedFixture f)
        {
            Mark(f, null);
            _fixtures.Remove(f);
            Recount();
        }

        /// <summary>次の段階に広げる。右と上に広がるので、既存の座標は変わらない（kihonsekkei.md 4.1）。</summary>
        internal void Expand()
        {
            if (Stage >= _config.MaxStage) throw new InvalidOperationException("最大の段階");
            SetStage(Stage + 1);
            foreach (var f in _fixtures) Mark(f, f);
            Recount();
        }

        void SetStage(int stage)
        {
            Stage = stage;
            var size = _config.SizeOf(stage);
            Width = size.Width;
            Height = size.Height;
            _blocking = new PlacedFixture[Width, Height];
            _floor = new PlacedFixture[Width, Height];
        }

        void Mark(PlacedFixture f, PlacedFixture value)
        {
            var grid = f.Def.Walkable ? _floor : _blocking;
            foreach (var t in f.Tiles) grid[t.X, t.Y] = value;
        }

        void Recount()
        {
            int ambience = 0, shelves = 0, counters = 0;
            foreach (var f in _fixtures)
            {
                ambience += f.Def.Ambience;
                if (f.Def.CountsTowardShelfLimit) shelves++;
                if (f.Def.Kind == FixtureKind.Counter) counters++;
            }
            Ambience = Math.Min(ambience, _config.Balance.AmbienceMax);
            ShelfCount = shelves;
            CounterCount = counters;
            Version++;
        }
    }
}
