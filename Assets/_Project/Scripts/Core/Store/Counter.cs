using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>カウンターと行列（kihonsekkei.md 6.3、shousaisekkei.md 7.2）。</summary>
    public sealed class Counter
    {
        readonly GameSession _session;
        readonly List<Customer> _queue = new List<Customer>();
        readonly List<Tile> _queueTiles = new List<Tile>();

        public PlacedFixture Fixture { get; }
        public int FixtureId => Fixture.Id;
        public Tile PayTile => Fixture.PayTile.Value;
        /// <summary>並んでいる順。会計中の客は含まない。</summary>
        public IReadOnlyList<Customer> Queue => _queue;
        public Customer Serving { get; private set; }
        /// <summary>会計の残り Tick。</summary>
        public int Remaining { get; private set; }
        public int Load => _queue.Count + (Serving != null ? 1 : 0);
        /// <summary>[0] が行列の先頭のマス。</summary>
        public IReadOnlyList<Tile> QueueTiles => _queueTiles;

        internal Counter(GameSession session, PlacedFixture fixture)
        {
            _session = session;
            Fixture = fixture;
            RebuildQueueTiles();
        }

        /// <summary>
        /// 会計マスから入口までの最短経路を、会計マスの次のマスから並べる。
        /// 足りなければ残りの客は最後のマスに重ねる。
        /// </summary>
        internal void RebuildQueueTiles()
        {
            _queueTiles.Clear();
            var path = _session.Pathfinder.FindPath(PayTile, _session.Layout.Entrance);
            if (path != null) _queueTiles.AddRange(path);
            if (_queueTiles.Count == 0) _queueTiles.Add(_session.Layout.Entrance);
        }

        /// <summary>行列で c が立つマス。</summary>
        public Tile QueueTileOf(Customer c)
        {
            int i = _queue.IndexOf(c);
            if (i < 0) return PayTile;
            return _queueTiles[i < _queueTiles.Count ? i : _queueTiles.Count - 1];
        }

        internal void Enqueue(Customer c)
        {
            _queue.Add(c);
            c.CounterId = FixtureId;
            EmitQueue();
        }

        /// <summary>行列から抜ける（待ちきれない、配置替え）。会計中の客なら会計をやめる。</summary>
        internal void Leave(Customer c)
        {
            if (Serving == c)
            {
                Serving = null;
                Remaining = 0;
            }
            else if (!_queue.Remove(c))
            {
                return;
            }
            EmitQueue();
        }

        /// <summary>会計中と行列の客を、会計中 → 並んでいる順で返して空にする（撤去のとき）。</summary>
        internal List<Customer> TakeAll()
        {
            var all = new List<Customer>();
            if (Serving != null) all.Add(Serving);
            all.AddRange(_queue);
            Serving = null;
            Remaining = 0;
            _queue.Clear();
            return all;
        }

        internal void Tick()
        {
            if (Serving != null)
            {
                Remaining--;
                if (Remaining == 0)
                {
                    var c = Serving;
                    Serving = null;
                    _session.Checkout(this, c);
                    EmitQueue();
                }
            }

            if (Serving == null && _queue.Count > 0)
            {
                var head = _queue[0];
                // 1 番目の行列マスに着いていれば始める。会計マスへの 1 歩は会計時間に含める（K-02）
                if (!head.IsWalking && (head.Tile == _queueTiles[0] || head.Tile == PayTile))
                {
                    _queue.RemoveAt(0);
                    Serving = head;
                    Remaining = _session.Config.Balance.CheckoutTicks;
                    _session.Brain.StartPaying(head, this);
                    EmitQueue();
                }
            }
        }

        void EmitQueue() => _session.Emit(new QueueChangedEvent(_session.Now, FixtureId, Load));
    }
}
