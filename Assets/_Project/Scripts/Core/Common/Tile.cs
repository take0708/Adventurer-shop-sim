using System;

namespace Shop.Core
{
    /// <summary>店内のマス（shousaisekkei.md 1.2）。原点は左下。</summary>
    public readonly struct Tile : IEquatable<Tile>
    {
        public int X { get; }
        public int Y { get; }

        public Tile(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>上 → 右 → 下 → 左（経路探索で隣を調べる順。shousaisekkei.md 8.6）。</summary>
        public static readonly Tile[] Directions = { new Tile(0, 1), new Tile(1, 0), new Tile(0, -1), new Tile(-1, 0) };

        public Tile Offset(int dx, int dy) => new Tile(X + dx, Y + dy);
        public Tile Offset(Tile d) => new Tile(X + d.X, Y + d.Y);

        public bool Equals(Tile other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public static bool operator ==(Tile a, Tile b) => a.Equals(b);
        public static bool operator !=(Tile a, Tile b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }
}
