using System;

namespace Shop.Core
{
    /// <summary>Core が使う乱数（kihonsekkei.md 1.2 P3）。</summary>
    public interface IRandom
    {
        /// <summary>[minInclusive, maxExclusive) の整数。</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>[0, 1) の実数。</summary>
        double NextDouble();
    }

    /// <summary>
    /// シード指定できる乱数（xorshift64*）。
    /// System.Random は実行環境で中身が変わりうるので使わない。Unity（Mono / IL2CPP）でも同じ並びになる。
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        ulong _state;

        public SeededRandom(ulong seed)
        {
            // SplitMix64 で 0 以外の初期状態にする
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            _state = z == 0 ? 0x9E3779B97F4A7C15UL : z;
        }

        ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)((long)minInclusive + (long)(NextULong() % span));
        }
    }
}
