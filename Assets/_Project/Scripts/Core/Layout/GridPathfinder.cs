using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// マス目の経路探索（shousaisekkei.md 8.6）。4 方向の幅優先探索で、隣は上 → 右 → 下 → 左の順に調べる。
    /// from が通れないマスでも、そこから出ることはできる。
    /// </summary>
    internal sealed class GridPathfinder
    {
        readonly ShopLayout _layout;

        public GridPathfinder(ShopLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        /// <summary>from を含まず to を含む経路。from == to なら空。届かなければ null。</summary>
        public IReadOnlyList<Tile> FindPath(Tile from, Tile to)
        {
            if (from == to) return Array.Empty<Tile>();
            if (!_layout.IsInside(from) || !_layout.IsWalkable(to)) return null;

            int w = _layout.Width, h = _layout.Height;
            var visited = new bool[w, h];
            var parent = new Tile[w, h];
            var queue = new Queue<Tile>();
            queue.Enqueue(from);
            visited[from.X, from.Y] = true;

            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Tile.Directions)
                {
                    var n = c.Offset(d);
                    if (!_layout.IsWalkable(n) || visited[n.X, n.Y]) continue;
                    visited[n.X, n.Y] = true;
                    parent[n.X, n.Y] = c;
                    if (n == to) return Trace(from, to, parent);
                    queue.Enqueue(n);
                }
            }
            return null;
        }

        /// <summary>歩く歩数。届かなければ int.MaxValue。</summary>
        public int Distance(Tile from, Tile to)
        {
            if (from == to) return 0;
            if (!_layout.IsInside(from) || !_layout.IsInside(to)) return int.MaxValue;
            return DistanceField(from)[to.X, to.Y];
        }

        /// <summary>1 回の幅優先探索で全マスの距離。届かないマスは int.MaxValue。</summary>
        public int[,] DistanceField(Tile from)
        {
            int w = _layout.Width, h = _layout.Height;
            var dist = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    dist[x, y] = int.MaxValue;
            if (!_layout.IsInside(from)) return dist;

            var queue = new Queue<Tile>();
            queue.Enqueue(from);
            dist[from.X, from.Y] = 0;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Tile.Directions)
                {
                    var n = c.Offset(d);
                    if (!_layout.IsWalkable(n) || dist[n.X, n.Y] != int.MaxValue) continue;
                    dist[n.X, n.Y] = dist[c.X, c.Y] + 1;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        static IReadOnlyList<Tile> Trace(Tile from, Tile to, Tile[,] parent)
        {
            var path = new List<Tile>();
            for (var t = to; t != from; t = parent[t.X, t.Y]) path.Add(t);
            path.Reverse();
            return path;
        }
    }
}
