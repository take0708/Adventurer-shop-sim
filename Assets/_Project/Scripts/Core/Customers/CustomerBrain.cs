using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// 客の状態を 1 Tick ずつ進める（kihonsekkei.md 5.3、shousaisekkei.md 8.4, 8.5）。
    /// 迷ったときは 番号が小さいほう → マスの (y, x) が小さいほう（shousaisekkei.md 1.3）。
    /// </summary>
    internal sealed class CustomerBrain
    {
        readonly GameSession _s;
        readonly BalanceDef _b;

        public CustomerBrain(GameSession session)
        {
            _s = session;
            _b = session.Config.Balance;
        }

        public void Tick(Customer c)
        {
            if (c.State == CustomerState.Gone) return;
            c.StayTicks++;

            if (c.State == CustomerState.ToQueue || c.State == CustomerState.Queuing)
            {
                c.QueueWaitTicks++;
                if (c.QueueWaitTicks > _b.QueuePatienceTicks)
                {
                    GiveUpQueue(c);
                    return;
                }
            }

            if (IsBrowsing(c.State) && c.StayTicks > _b.MaxCustomerStayTicks)
            {
                // 最長滞在を超えたら、残りをあきらめて会計へ（K-03）
                foreach (var w in c.Wants) if (w.Status == WantStatus.Pending) w.MarkFailed(FailKind.GaveUp);
                CancelStep(c);
                FinishBrowsing(c);
                return;
            }

            if (c.IsWalking)
            {
                StepWalk(c);
                return;
            }

            switch (c.State)
            {
                case CustomerState.Entering:
                    if (--c.StateTimer <= 0) NextWant(c);
                    break;
                case CustomerState.ToShelf:
                    ArriveAtShelf(c);
                    break;
                case CustomerState.Picking:
                case CustomerState.Disappointed:
                    if (--c.StateTimer <= 0) NextWant(c);
                    break;
                case CustomerState.Wandering:
                    c.CurrentWant.MarkFailed(FailKind.NotCarried);
                    Emote(c, EmoteKind.NotCarried);
                    break;
                case CustomerState.ToQueue:
                    c.State = CustomerState.Queuing;
                    break;
                case CustomerState.Queuing:
                {
                    var counter = _s.CounterById(c.CounterId);
                    var t = counter.QueueTileOf(c);
                    if (t != c.Tile) WalkTo(c, t);
                    break;
                }
                case CustomerState.Paying:
                    break;
                case CustomerState.Leaving:
                    c.State = CustomerState.Gone;
                    _s.Emit(new CustomerExitedEvent(_s.Now, c.Id));
                    break;
            }
        }

        static bool IsBrowsing(CustomerState s) =>
            s == CustomerState.ToShelf || s == CustomerState.Picking || s == CustomerState.Disappointed || s == CustomerState.Wandering;

        /// <summary>1 マスを WalkTicksPerTile かけて歩く。</summary>
        void StepWalk(Customer c)
        {
            if (c.StepTimer == 0)
            {
                c.NextTile = c.Path.Dequeue();
                c.StepTimer = _b.WalkTicksPerTile;
                _s.Emit(new CustomerMovedEvent(_s.Now, c.Id, c.Tile, c.NextTile, _b.WalkTicksPerTile));
            }
            c.StepTimer--;
            if (c.StepTimer == 0) c.Tile = c.NextTile;
        }

        /// <summary>歩いている途中のマスを取りやめる（配置替えなど）。出発したマスに戻ったことにする。</summary>
        void CancelStep(Customer c)
        {
            if (c.StepTimer > 0)
            {
                c.StepTimer = 0;
                _s.Emit(new CustomerMovedEvent(_s.Now, c.Id, c.NextTile, c.Tile, 0));
            }
            c.Path.Clear();
        }

        /// <summary>dest への経路を引く。届かなければ false（その場にとどまる）。</summary>
        bool WalkTo(Customer c, Tile dest)
        {
            c.Path.Clear();
            c.Dest = dest;
            var path = _s.Pathfinder.FindPath(c.Tile, dest);
            if (path == null) return false;
            foreach (var t in path) c.Path.Enqueue(t);
            return true;
        }

        void Emote(Customer c, EmoteKind kind)
        {
            _s.Emit(new CustomerEmoteEvent(_s.Now, c.Id, kind));
            c.State = CustomerState.Disappointed;
            c.StateTimer = _b.EmoteTicks;
        }

        // ---- 欲しいものを順に探す ----

        void NextWant(Customer c)
        {
            c.Retargeted = false;
            Want w = null;
            foreach (var x in c.Wants)
                if (x.Status == WantStatus.Pending) { w = x; break; }
            if (w == null)
            {
                FinishBrowsing(c);
                return;
            }
            c.CurrentWant = w;

            var shelf = ChooseShelf(c, w.Item, null);
            if (shelf != null)
            {
                GoToShelf(c, shelf);
                return;
            }
            if (w.IsRare)
            {
                // 掘り出し物は無ければ何もせず次へ（K-04）
                w.MarkFailed(FailKind.OutOfStock);
                NextWant(c);
                return;
            }

            // 置いていない: 同じ系統の棚があればそこへ行ってしょんぼり、なければうろうろ
            var sameCategory = Nearest(c, s => !s.IsBargainTable && s.Def.ShelfCategory == w.Item.Category);
            if (sameCategory != null)
            {
                GoToShelf(c, sameCategory);
                return;
            }
            Wander(c);
        }

        /// <summary>item を並べた棚のうち、在庫のある最も近い棚。なければ在庫なしでも最も近い棚。どちらもなければ null。</summary>
        Shelf ChooseShelf(Customer c, ItemDef item, Shelf exclude)
        {
            var inStock = Nearest(c, s => s != exclude && s.Item == item && s.Displayed > 0);
            if (inStock != null || exclude != null) return inStock;
            return Nearest(c, s => s.Item == item);
        }

        Shelf Nearest(Customer c, System.Func<Shelf, bool> match)
        {
            var field = _s.Pathfinder.DistanceField(c.Tile);
            Shelf best = null;
            int bestDist = int.MaxValue;
            foreach (var s in _s.Shelves)
            {
                if (!match(s)) continue;
                int d = UseTileOf(s, field, out _);
                if (d < bestDist) { best = s; bestDist = d; }
            }
            return best;
        }

        /// <summary>棚の使う位置（上下左右の通れるマス）のうち最も近いもの。距離を返す。</summary>
        int UseTileOf(Shelf s, int[,] field, out Tile use)
        {
            use = default;
            int best = int.MaxValue;
            var o = s.Fixture.Origin;
            foreach (var d in Tile.Directions)
            {
                var t = o.Offset(d);
                if (!_s.Layout.IsWalkable(t)) continue;
                int dist = field[t.X, t.Y];
                if (dist < best || (dist == best && dist != int.MaxValue && Before(t, use)))
                {
                    best = dist;
                    use = t;
                }
            }
            return best;
        }

        static bool Before(Tile a, Tile b) => a.Y < b.Y || (a.Y == b.Y && a.X < b.X);

        void GoToShelf(Customer c, Shelf shelf)
        {
            var field = _s.Pathfinder.DistanceField(c.Tile);
            if (UseTileOf(shelf, field, out var use) == int.MaxValue)
            {
                c.CurrentWant.MarkFailed(FailKind.NotCarried);
                Emote(c, EmoteKind.NotCarried);
                return;
            }
            c.TargetShelfId = shelf.FixtureId;
            c.State = CustomerState.ToShelf;
            WalkTo(c, use);
        }

        /// <summary>入口から 2〜4 マスの通れるマスを 1 つ選んで歩く（K-13）。</summary>
        void Wander(Customer c)
        {
            var field = _s.Pathfinder.DistanceField(_s.Layout.Entrance);
            var candidates = new List<Tile>();
            for (int y = 0; y < _s.Layout.Height; y++)
                for (int x = 0; x < _s.Layout.Width; x++)
                {
                    int d = field[x, y];
                    if (d >= 2 && d <= 4) candidates.Add(new Tile(x, y));
                }
            c.State = CustomerState.Wandering;
            if (candidates.Count == 0) return;   // 次の Tick にその場でしょんぼり
            WalkTo(c, candidates[_s.Random.Range(0, candidates.Count)]);
        }

        void ArriveAtShelf(Customer c)
        {
            var w = c.CurrentWant;
            var shelf = _s.ShelfById(c.TargetShelfId);
            if (shelf == null || shelf.Item != w.Item)
            {
                w.MarkFailed(FailKind.NotCarried);
                Emote(c, EmoteKind.NotCarried);
            }
            else if (shelf.Displayed == 0)
            {
                var other = c.Retargeted ? null : ChooseShelf(c, w.Item, shelf);
                if (other != null)
                {
                    // 在庫のある同じ品の棚へ 1 回だけ向かい直す（K-06）
                    c.Retargeted = true;
                    GoToShelf(c, other);
                    return;
                }
                w.MarkFailed(FailKind.OutOfStock);
                Emote(c, EmoteKind.OutOfStock);
            }
            else if (w.Item.SellPrice > c.Budget)
            {
                w.MarkFailed(FailKind.TooExpensive);
                Emote(c, EmoteKind.TooExpensive);
            }
            else
            {
                shelf.TryTake();
                c.Budget -= w.Item.SellPrice;
                c.BasketList.Add((w.Item, shelf.FixtureId));
                w.Status = WantStatus.InBasket;
                _s.Emit(new ItemTakenEvent(_s.Now, shelf.FixtureId, c.Id, w.Item));
                _s.Emit(new CustomerEmoteEvent(_s.Now, c.Id, w.IsRare ? EmoteKind.Treasure : EmoteKind.Picked));
                c.State = CustomerState.Picking;
                c.StateTimer = _b.PickTicks;
            }
            c.Retargeted = false;
        }

        // ---- 会計と退店 ----

        void FinishBrowsing(Customer c)
        {
            if (c.BasketList.Count == 0)
            {
                Decide(c, CustomerResult.None);
                return;
            }
            var counter = ChooseCounter(c, null);
            counter.Enqueue(c);
            c.State = CustomerState.ToQueue;
            WalkTo(c, counter.QueueTileOf(c));
        }

        /// <summary>待ち人数が少ない → 会計マスまでが近い → 番号が小さい（kihonsekkei.md 6.3）。</summary>
        Counter ChooseCounter(Customer c, Counter exclude)
        {
            var field = _s.Pathfinder.DistanceField(c.Tile);
            Counter best = null;
            int bestLoad = int.MaxValue, bestDist = int.MaxValue;
            foreach (var counter in _s.Counters)
            {
                if (counter == exclude) continue;
                int load = counter.Load;
                int dist = field[counter.PayTile.X, counter.PayTile.Y];
                if (load < bestLoad || (load == bestLoad && dist < bestDist))
                {
                    best = counter;
                    bestLoad = load;
                    bestDist = dist;
                }
            }
            return best;
        }

        /// <summary>Counter が会計を始めるときに呼ぶ。会計マスへの 1 歩は会計時間の中で歩く。</summary>
        public void StartPaying(Customer c, Counter counter)
        {
            c.State = CustomerState.Paying;
            if (c.Tile != counter.PayTile) WalkTo(c, counter.PayTile);
        }

        /// <summary>会計が終わった。かごの売値を足し、結果を決めて出口へ。</summary>
        public void OnCheckedOut(Customer c)
        {
            int got = 0, total = 0;
            foreach (var w in c.Wants)
            {
                if (w.IsRare) continue;
                total++;
                if (w.Status == WantStatus.InBasket) got++;
            }
            var result = total > 0 && got == total ? CustomerResult.Full : got > 0 ? CustomerResult.Partial : CustomerResult.None;
            Decide(c, result);
        }

        /// <summary>行列で待ちきれなかった。かごの品を取った棚へ戻す（だめなら倉庫へ）。</summary>
        void GiveUpQueue(Customer c)
        {
            CancelStep(c);
            _s.CounterById(c.CounterId)?.Leave(c);
            foreach (var (item, shelfId) in c.BasketList)
            {
                var shelf = _s.ShelfById(shelfId);
                if (shelf == null || !shelf.TryPutBack(item)) _s.Warehouse.Add(item, 1);
            }
            c.BasketList.Clear();
            Decide(c, CustomerResult.QueueLeft);
        }

        /// <summary>結果は店を出ることが決まった瞬間に 1 回だけ決める（shousaisekkei.md 8.5）。評判もここで足す。</summary>
        void Decide(Customer c, CustomerResult result)
        {
            c.Result = result;
            _s.RecordResult(c, result);
            var bought = new ItemDef[c.BasketList.Count];
            for (int i = 0; i < bought.Length; i++) bought[i] = c.BasketList[i].Item;
            _s.Emit(new CustomerLeftEvent(_s.Now, c.Id, result, bought));
            c.State = CustomerState.Leaving;
            c.CounterId = 0;
            WalkTo(c, _s.Layout.Entrance);
        }

        // ---- 配置が変わったとき（shousaisekkei.md 6.5） ----

        /// <summary>撤去されたカウンターに並んでいた客を、別のカウンターの最後に並び直させる（待った時間はそのまま）。</summary>
        public void Reassign(Customer c, Counter removed)
        {
            CancelStep(c);
            var counter = ChooseCounter(c, removed);
            counter.Enqueue(c);
            c.State = CustomerState.ToQueue;
            WalkTo(c, counter.QueueTileOf(c));
        }

        public void OnLayoutChanged(Customer c)
        {
            if (c.State == CustomerState.Gone) return;
            CancelStep(c);

            if (!_s.Layout.IsWalkable(c.Tile))
            {
                var to = NearestWalkable(c.Tile);
                _s.Emit(new CustomerMovedEvent(_s.Now, c.Id, c.Tile, to, 0));
                c.Tile = to;
            }

            switch (c.State)
            {
                case CustomerState.ToShelf:
                {
                    var shelf = _s.ShelfById(c.TargetShelfId);
                    if (shelf == null) NextWant(c);
                    else GoToShelf(c, shelf);
                    break;
                }
                case CustomerState.Wandering:
                    if (!_s.Layout.IsWalkable(c.Dest) || !WalkTo(c, c.Dest)) c.Path.Clear();
                    break;
                case CustomerState.ToQueue:
                case CustomerState.Queuing:
                    WalkTo(c, _s.CounterById(c.CounterId).QueueTileOf(c));
                    break;
                case CustomerState.Paying:
                {
                    var counter = _s.CounterById(c.CounterId);
                    if (c.Tile != counter.PayTile) WalkTo(c, counter.PayTile);
                    break;
                }
                case CustomerState.Leaving:
                    WalkTo(c, _s.Layout.Entrance);
                    break;
            }
        }

        /// <summary>入口からたどり着ける通れるマスのうち、from に最も近いもの（同じなら (y, x) が小さい）。</summary>
        Tile NearestWalkable(Tile from)
        {
            var field = _s.Pathfinder.DistanceField(_s.Layout.Entrance);
            Tile best = _s.Layout.Entrance;
            int bestDist = int.MaxValue;
            for (int y = 0; y < _s.Layout.Height; y++)
                for (int x = 0; x < _s.Layout.Width; x++)
                {
                    if (field[x, y] == int.MaxValue) continue;
                    int d = System.Math.Abs(x - from.X) + System.Math.Abs(y - from.Y);
                    if (d < bestDist) { bestDist = d; best = new Tile(x, y); }
                }
            return best;
        }
    }
}
