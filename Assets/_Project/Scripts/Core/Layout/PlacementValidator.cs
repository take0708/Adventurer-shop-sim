using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// 置く・動かす・撤去するの判定（kihonsekkei.md 4.3 L1〜L5、shousaisekkei.md 6.3）。
    /// 状態は変えない。変えたあとのレイアウトを仮に考えて確かめる。
    /// </summary>
    internal sealed class PlacementValidator
    {
        readonly GameConfig _config;
        readonly ShopLayout _layout;
        readonly ShopState _state;

        /// <param name="state">null ならお金と無料枠を見ない（ConfigValidator の初期配置の確認用）。</param>
        public PlacementValidator(GameConfig config, ShopLayout layout, ShopState state)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _state = state;
        }

        /// <summary>棚で無料枠が残っていれば 0、それ以外は def.Cost。</summary>
        public int PlacementCost(FixtureDef def)
        {
            if (_state != null && def.Kind == FixtureKind.Shelf && def.UsesFreeShelf && _state.FreeShelvesLeft > 0) return 0;
            return def.Cost;
        }

        public CommandResult CanPlace(FixtureDef def, Tile origin)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            var r = CheckTiles(def, origin, null);
            if (!r.Ok) return r;

            var size = _layout.Size;
            if (def.MaxCount >= 1 && _layout.CountOf(def) >= def.MaxCount) return CommandResult.Fail(FailReason.LimitReached);
            if (def.CountsTowardShelfLimit && _layout.ShelfCount >= size.ShelfMax) return CommandResult.Fail(FailReason.LimitReached);
            if (def.Kind == FixtureKind.Counter && _layout.CounterCount >= size.CounterMax) return CommandResult.Fail(FailReason.LimitReached);

            if (_state != null && _state.Money < PlacementCost(def)) return CommandResult.Fail(FailReason.NotEnoughMoney);

            if (!CheckReachability(null, def, origin)) return CommandResult.Fail(FailReason.BlocksPath);
            return CommandResult.Success;
        }

        /// <summary>自分を取り除いた仮のレイアウトで、置く判定の 1〜4 と 7 を確かめる。移動は無料。</summary>
        public CommandResult CanMove(PlacedFixture f, Tile origin)
        {
            if (f == null) throw new ArgumentNullException(nameof(f));
            if (f.Origin == origin) return CommandResult.Success;

            var r = CheckTiles(f.Def, origin, f);
            if (!r.Ok) return r;
            if (!CheckReachability(f, f.Def, origin)) return CommandResult.Fail(FailReason.BlocksPath);
            return CommandResult.Success;
        }

        public CommandResult CanRemove(PlacedFixture f)
        {
            if (f == null) throw new ArgumentNullException(nameof(f));
            if (f.Def.Kind == FixtureKind.Counter && _layout.CounterCount <= 1) return CommandResult.Fail(FailReason.LastCounter);
            if (!CheckReachability(f, null, default)) return CommandResult.Fail(FailReason.BlocksPath);
            return CommandResult.Success;
        }

        /// <summary>置く判定の 1〜4（外・入口・重なり・会計マス）。ignore はないものとして扱う。</summary>
        CommandResult CheckTiles(FixtureDef def, Tile origin, PlacedFixture ignore)
        {
            foreach (var t in ShopLayout.Footprint(def, origin))
                if (!_layout.IsInside(t)) return CommandResult.Fail(FailReason.OutOfBounds);

            foreach (var t in ShopLayout.Footprint(def, origin))
                if (t == _layout.Entrance) return CommandResult.Fail(FailReason.BlocksEntrance);

            // 絨毯の上にも置けない。絨毯は何もないマスにだけ置ける
            foreach (var t in ShopLayout.Footprint(def, origin))
            {
                var other = _layout.FixtureAt(t);
                if (other != null && other != ignore) return CommandResult.Fail(FailReason.Occupied);
            }

            var pay = ShopLayout.PayTileOf(def, origin);
            if (pay.HasValue)
            {
                var p = pay.Value;
                if (!_layout.IsInside(p) || p == _layout.Entrance) return CommandResult.Fail(FailReason.NoPayTile);
                var other = _layout.FixtureAt(p);
                if (other != null && other != ignore && !other.Def.Walkable) return CommandResult.Fail(FailReason.NoPayTile);
            }
            return CommandResult.Success;
        }

        /// <summary>
        /// 入口から、すべての棚の使う位置のどれかと、すべてのカウンターの会計マスへ歩いて行けるか。
        /// ignore はないものとし、extraDef があれば extraOrigin に置いたものとして考える。
        /// </summary>
        bool CheckReachability(PlacedFixture ignore, FixtureDef extraDef, Tile extraOrigin)
        {
            int w = _layout.Width, h = _layout.Height;
            var blocked = new bool[w, h];
            var targets = new List<(FixtureDef Def, Tile Origin)>();

            foreach (var f in _layout.Fixtures)
            {
                if (f == ignore) continue;
                targets.Add((f.Def, f.Origin));
            }
            if (extraDef != null) targets.Add((extraDef, extraOrigin));

            foreach (var (def, origin) in targets)
            {
                if (def.Walkable) continue;
                foreach (var t in ShopLayout.Footprint(def, origin)) blocked[t.X, t.Y] = true;
            }

            var reached = Flood(_layout.Entrance, blocked, w, h);

            foreach (var (def, origin) in targets)
            {
                if (def.Kind == FixtureKind.Shelf)
                {
                    bool ok = false;
                    foreach (var d in Tile.Directions)
                    {
                        var t = origin.Offset(d);
                        if (InBounds(t, w, h) && reached[t.X, t.Y]) { ok = true; break; }
                    }
                    if (!ok) return false;
                }
                else if (def.Kind == FixtureKind.Counter)
                {
                    var p = ShopLayout.PayTileOf(def, origin).Value;
                    if (!InBounds(p, w, h) || !reached[p.X, p.Y]) return false;
                }
            }
            return true;
        }

        static bool InBounds(Tile t, int w, int h) => t.X >= 0 && t.Y >= 0 && t.X < w && t.Y < h;

        static bool[,] Flood(Tile start, bool[,] blocked, int w, int h)
        {
            var reached = new bool[w, h];
            if (!InBounds(start, w, h) || blocked[start.X, start.Y]) return reached;
            var queue = new Queue<Tile>();
            queue.Enqueue(start);
            reached[start.X, start.Y] = true;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Tile.Directions)
                {
                    var n = c.Offset(d);
                    if (!InBounds(n, w, h) || blocked[n.X, n.Y] || reached[n.X, n.Y]) continue;
                    reached[n.X, n.Y] = true;
                    queue.Enqueue(n);
                }
            }
            return reached;
        }
    }
}
