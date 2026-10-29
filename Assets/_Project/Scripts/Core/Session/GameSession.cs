using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// Core の窓口（shousaisekkei.md 4 章）。Tick で進め、コマンドで操作し、DrainEvents でイベントを受け取る。
    /// M6 まで: 時間、配置と拡張、棚、客と会計、問屋と行商人、月末の集計、評判と星。年末・終了・結果は M7。
    /// </summary>
    public sealed class GameSession
    {
        readonly PlacementValidator _validator;
        readonly CustomerSpawner _spawner;
        readonly CustomerFactory _factory;
        readonly Wholesaler _wholesaler;
        readonly ReputationSystem _reputation;
        readonly List<Shelf> _shelves = new List<Shelf>();
        readonly List<Counter> _counters = new List<Counter>();
        readonly List<Customer> _customers = new List<Customer>();
        List<GameEvent> _events = new List<GameEvent>();
        List<GameEvent> _drained = new List<GameEvent>();
        int _nextCustomerId = 1;
        bool _debugStarUp;
        readonly List<MonthlyReport> _reports = new List<MonthlyReport>();
        readonly List<YearRecord> _years = new List<YearRecord>();
        int _yearStartRep;

        public GameConfig Config { get; }
        public GameClock Clock { get; }
        public ShopState State { get; }
        public Warehouse Warehouse { get; }
        public ShopLayout Layout { get; }
        /// <summary>FixtureId の小さい順。</summary>
        public IReadOnlyList<Shelf> Shelves => _shelves;
        public IReadOnlyList<Counter> Counters => _counters;
        /// <summary>店内にいる客。CustomerId の小さい順。</summary>
        public IReadOnlyList<Customer> Customers => _customers;
        public Merchant Merchant { get; }
        public bool IsEnded => Clock.IsFinished;
        /// <summary>今月の来店予定の人数。</summary>
        public int PlannedVisits => _spawner.PlannedCount;
        /// <summary>締めた月の集計（古い順）。</summary>
        public IReadOnlyList<MonthlyReport> Reports => _reports;
        /// <summary>締めた年の記録（古い順）。</summary>
        public IReadOnlyList<YearRecord> Years => _years;
        /// <summary>2 年が終わったときの結果。GameEnded の前は null。</summary>
        public GameResult Result { get; private set; }
        /// <summary>今月の途中集計。</summary>
        public MonthlyReport CurrentReport { get; private set; }
        /// <summary>
        /// 直前の Tick で月が終わった（MonthEnded を積んだ）。
        /// GameRunner は 1 フレームで何 Tick も進めるので、これを見て月末で止まる（shousaisekkei.md 11.2）。
        /// </summary>
        public bool MonthEndedThisTick { get; private set; }

        /// <summary>次の星までの残りの評判。最大の星なら 0。</summary>
        public int RepToNextStar => _reputation.RepToNextStar(State.Reputation, State.Star);

        /// <summary>今の星で問屋が売る品。</summary>
        public IReadOnlyList<ItemDef> UnlockedItems => _wholesaler.Unlocked(State.Star);

        internal IRandom Random { get; }
        internal GridPathfinder Pathfinder { get; }
        internal CustomerBrain Brain { get; }
        /// <summary>今の ElapsedTicks。イベントの時刻に使う。</summary>
        internal int Now => Clock.ElapsedTicks;

        public GameSession(GameConfig config, ulong seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Random = new SeededRandom(seed);
            Clock = new GameClock(config.Time);
            State = new ShopState(config.Balance.StartMoney, config.Balance.FreeShelfCount, config.MaxStar);
            Warehouse = new Warehouse(config, this);
            Layout = new ShopLayout(config);
            Pathfinder = new GridPathfinder(Layout);
            _validator = new PlacementValidator(config, Layout, State);
            Brain = new CustomerBrain(this);
            _spawner = new CustomerSpawner(config, Random);
            _factory = new CustomerFactory(config, Random);
            _wholesaler = new Wholesaler(this);
            _reputation = new ReputationSystem(this);
            Merchant = new Merchant(this);
            CurrentReport = new MonthlyReport(1, 0);

            // 初期配置は無料で置く。無料枠は減らさない（shousaisekkei.md 4.2）
            foreach (var (def, origin) in config.Balance.InitialLayout) AddFixtureObjects(Layout.Add(def, origin, 0));

            // 1 か月目の準備
            _spawner.PlanMonth(State.Star, Layout.Ambience);
            Merchant.PlanMonth(State.Star);

            Emit(new SessionStartedEvent(Now));
        }

        /// <summary>1 Tick 進める。終了後は何もしない（shousaisekkei.md 4.3、K-01）。</summary>
        public void Tick()
        {
            MonthEndedThisTick = false;
            if (IsEnded) return;
            int t = Clock.TickInMonth;

            int due = _spawner.Due(t);
            for (int i = 0; i < due; i++)
            {
                var (job, wants, budget) = _factory.Create(State.Star, _shelves);
                SpawnCustomer(job, wants, budget);
            }

            for (int i = 0; i < _customers.Count; i++) Brain.Tick(_customers[i]);
            for (int i = 0; i < _counters.Count; i++) _counters[i].Tick();
            for (int i = 0; i < _shelves.Count; i++) _shelves[i].Tick();
            Merchant.Tick(t);
            _customers.RemoveAll(c => c.State == CustomerState.Gone);

            var clockEvents = Clock.Advance();
            if ((clockEvents & ClockEvents.DayChanged) != 0) Emit(new DayChangedEvent(Now, Clock.Day, Clock.Month));
            if ((clockEvents & ClockEvents.MonthEnded) != 0) EndMonth(clockEvents);
        }

        /// <summary>
        /// 月末の処理（kihonsekkei.md 7.1、shousaisekkei.md 9.2）。店内に残っている客はそのまま翌月へ。
        /// 星は翌月から反映する（FR-REP-05）。イベントは MonthEnded → StarRaised → YearEnded → GameEnded の順。
        /// </summary>
        void EndMonth(ClockEvents clockEvents)
        {
            int month = Clock.ElapsedTicks / Config.Time.TicksPerMonth;
            if (_debugStarUp)
            {
                _debugStarUp = false;
                if (RepToNextStar > 0) DebugAddReputation(RepToNextStar);
            }
            bool raised = _reputation.TryRaiseStar(month + 1, out int newStar, out var unlocked);
            // 集計の星は月末の判定のあと
            CurrentReport.Close(State.Reputation, State.Star, RepToNextStar);
            Emit(new MonthEndedEvent(Now, CurrentReport));
            _reports.Add(CurrentReport);
            if (raised) Emit(new StarRaisedEvent(Now, newStar, unlocked.ToArray()));
            MonthEndedThisTick = true;
            if ((clockEvents & ClockEvents.YearEnded) != 0) EndYear(month);
            if ((clockEvents & ClockEvents.GameEnded) != 0)
            {
                Result = new GameResult(State.TotalSales, State.Star, State.Reputation, State.StarReachedMonth, _years);
                Emit(new GameEndedEvent(Now, Result));
            }
            if (IsEnded) return;

            // 翌月の準備
            CurrentReport = new MonthlyReport(month + 1, State.Reputation);
            _spawner.PlanMonth(State.Star, Layout.Ambience);
            Merchant.PlanMonth(State.Star);
        }

        /// <summary>年末の記録を締める（kihonsekkei.md 7.2, 7.4）。講評は増えた評判で 3 段階。</summary>
        void EndYear(int month)
        {
            int year = (month - 1) / Config.Time.MonthsPerYear + 1;
            int sales = 0;
            foreach (var r in _reports) if ((r.Month - 1) / Config.Time.MonthsPerYear + 1 == year) sales += r.Sales;
            int gained = State.Reputation - _yearStartRep;
            var b = Config.Balance;
            var grade = gained >= b.ReviewExcellentRep[year - 1] ? ReviewGrade.Excellent
                : gained >= b.ReviewGoodRep[year - 1] ? ReviewGrade.Good : ReviewGrade.Steady;
            var record = new YearRecord(year, gained, sales, State.Star, grade);
            _years.Add(record);
            _yearStartRep = State.Reputation;
            Emit(new YearEndedEvent(Now, record));
        }

        /// <summary>ためたイベントを返して空にする。返したリストは次の DrainEvents まで有効。</summary>
        public IReadOnlyList<GameEvent> DrainEvents()
        {
            var tmp = _drained;
            _drained = _events;
            _events = tmp;
            _events.Clear();
            return _drained;
        }

        public Shelf ShelfById(int fixtureId)
        {
            foreach (var s in _shelves) if (s.FixtureId == fixtureId) return s;
            return null;
        }

        public Counter CounterById(int fixtureId)
        {
            foreach (var c in _counters) if (c.FixtureId == fixtureId) return c;
            return null;
        }

        // ---- 確かめるだけ（配置モードの赤・緑表示用。状態は変えない） ----

        public CommandResult CanPlace(FixtureDef def, Tile origin)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            return _validator.CanPlace(def, origin);
        }

        public CommandResult CanMove(int fixtureId, Tile origin)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            var f = Layout.FindById(fixtureId);
            if (f == null) return CommandResult.Fail(FailReason.NotFound);
            return _validator.CanMove(f, origin);
        }

        public CommandResult CanRemove(int fixtureId)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            var f = Layout.FindById(fixtureId);
            if (f == null) return CommandResult.Fail(FailReason.NotFound);
            return _validator.CanRemove(f);
        }

        /// <summary>置く費用。無料枠が残っていれば 0。</summary>
        public int PlacementCost(FixtureDef def) => _validator.PlacementCost(def);

        /// <summary>撤去したときに戻る額。</summary>
        public int RefundOf(int fixtureId)
        {
            var f = Layout.FindById(fixtureId);
            return f == null ? 0 : f.PaidCost * Config.Balance.RefundPercent / 100;
        }

        /// <summary>次の段階に広げる費用。最大の段階なら 0。</summary>
        public int NextExpansionCost => Layout.Stage < Config.MaxStage ? Config.SizeOf(Layout.Stage + 1).Cost : 0;

        // ---- コマンド（shousaisekkei.md 6.4、7.1、10.2） ----

        public CommandResult PlaceFixture(FixtureDef def, Tile origin)
        {
            var r = CanPlace(def, origin);
            if (!r.Ok) return r;

            int cost = PlacementCost(def);
            if (cost == 0 && def.Kind == FixtureKind.Shelf && def.UsesFreeShelf && def.Cost > 0) State.UseFreeShelf();
            if (cost > 0)
            {
                Pay(-cost, MoneyReason.Equipment);
                CurrentReport.AddEquipment(cost);
            }

            var f = Layout.Add(def, origin, cost);
            AddFixtureObjects(f);
            OnLayoutChanged();
            return CommandResult.Success;
        }

        public CommandResult MoveFixture(int fixtureId, Tile origin)
        {
            var r = CanMove(fixtureId, origin);
            if (!r.Ok) return r;

            var f = Layout.FindById(fixtureId);
            if (f.Origin == origin) return CommandResult.Success;
            Layout.Move(f, origin);
            OnLayoutChanged();
            return CommandResult.Success;
        }

        public CommandResult RemoveFixture(int fixtureId)
        {
            var r = CanRemove(fixtureId);
            if (!r.Ok) return r;

            var f = Layout.FindById(fixtureId);
            var shelf = ShelfById(fixtureId);
            if (shelf != null)
            {
                shelf.ReturnAllToWarehouse();
                _shelves.Remove(shelf);
            }
            var counter = CounterById(fixtureId);
            if (counter != null)
            {
                var waiting = counter.TakeAll();
                _counters.Remove(counter);
                Layout.Remove(f);
                foreach (var c in _counters) c.RebuildQueueTiles();
                foreach (var c in waiting) Brain.Reassign(c, counter);
            }
            else
            {
                Layout.Remove(f);
            }

            int refund = f.PaidCost * Config.Balance.RefundPercent / 100;
            if (refund > 0)
            {
                Pay(refund, MoneyReason.Refund);
                CurrentReport.AddEquipment(-refund);
            }
            OnLayoutChanged();
            return CommandResult.Success;
        }

        public CommandResult ExpandShop()
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            if (Layout.Stage >= Config.MaxStage) return CommandResult.Fail(FailReason.MaxStage);
            int cost = NextExpansionCost;
            if (State.Money < cost) return CommandResult.Fail(FailReason.NotEnoughMoney);

            if (cost > 0)
            {
                Pay(-cost, MoneyReason.Expansion);
                CurrentReport.AddEquipment(cost);
            }
            Layout.Expand();
            Emit(new ShopExpandedEvent(Now, Layout.Stage));
            OnLayoutChanged();
            return CommandResult.Success;
        }

        /// <summary>棚に並べる商品を決める。null で何も置かない。</summary>
        public CommandResult AssignShelf(int fixtureId, ItemDef itemOrNull)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            var shelf = ShelfById(fixtureId);
            if (shelf == null) return CommandResult.Fail(FailReason.NotFound);
            return shelf.Assign(itemOrNull);
        }

        /// <summary>問屋から仕入れる（shousaisekkei.md 5.4）。</summary>
        public CommandResult BuyFromWholesaler(ItemDef item, int count)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            return _wholesaler.Buy(item, count);
        }

        /// <summary>行商人から限定品を買う（shousaisekkei.md 9.1）。特売台がなくても買えて倉庫に入る。</summary>
        public CommandResult BuyFromMerchant(int count)
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            return Merchant.Buy(count);
        }

        /// <summary>
        /// デバッグ用: 倉庫に品を直接入れる（問屋のない M3 で遊ぶため。shousaisekkei.md 14 章）。
        /// 問屋（M4）ができたら UI からは呼ばない。
        /// </summary>
        public void DebugAddStock(ItemDef item, int count)
        {
            if (count > 0) Warehouse.Add(item, count);
        }

        /// <summary>
        /// デバッグ用: 今月の月末の判定の直前に、次の星に足りない分の評判を足す（星アップの演出を早く確かめるため）。
        /// 押してから月末までに不満の客で評判が減っても、必ず次の星に上がる。
        /// </summary>
        public void DebugStarUpAtMonthEnd() => _debugStarUp = true;

        /// <summary>DebugStarUpAtMonthEnd を押してあり、今月の月末に次の星へ上がる予定か。</summary>
        public bool DebugStarUpPending => _debugStarUp;

        /// <summary>デバッグ用: 行商人を次の Tick で来させる（行商人パネルを早く確かめるため）。</summary>
        public void DebugCallMerchant()
        {
            if (!IsEnded) Merchant.DebugArriveNext(State.Star, Clock.TickInMonth);
        }

        /// <summary>デバッグ用: お金を足す（拡張を早く確かめるため）。月の集計には数えない。</summary>
        public void DebugAddMoney(int amount)
        {
            if (amount != 0) Pay(amount, MoneyReason.Refund);
        }

        /// <summary>デバッグ用: 評判を足す。</summary>
        public void DebugAddReputation(int amount)
        {
            if (amount == 0) return;
            int delta = State.AddReputation(amount);
            Emit(new ReputationChangedEvent(Now, delta, State.Reputation, CustomerResult.Full, 0));
        }

        // ---- 内部 ----

        void AddFixtureObjects(PlacedFixture f)
        {
            if (f.Def.Kind == FixtureKind.Shelf) InsertById(_shelves, new Shelf(this, f), s => s.FixtureId);
            else if (f.Def.Kind == FixtureKind.Counter) InsertById(_counters, new Counter(this, f), c => c.FixtureId);
        }

        static void InsertById<T>(List<T> list, T item, Func<T, int> id)
        {
            int i = list.Count;
            while (i > 0 && id(list[i - 1]) > id(item)) i--;
            list.Insert(i, item);
        }

        /// <summary>客を入口に出す。テストでは中身を決めて直接呼ぶ。</summary>
        internal Customer SpawnCustomer(JobDef job, List<Want> wants, int budget)
        {
            var c = new Customer(_nextCustomerId++, job, wants, budget, Layout.Entrance);
            c.StateTimer = Config.Balance.WalkTicksPerTile;
            _customers.Add(c);
            CurrentReport.AddVisitor();
            Emit(new CustomerSpawnedEvent(Now, c.Id, job, c.Tile));
            return c;
        }

        /// <summary>今月の残りの来店予定を消す（テスト用）。</summary>
        internal void ClearSpawnPlan() => _spawner.Clear();

        internal CustomerFactory Factory => _factory;
        internal CustomerSpawner Spawner => _spawner;

        /// <summary>Counter から呼ばれる。売上を足して、客の結果を決める（shousaisekkei.md 7.2）。</summary>
        internal void Checkout(Counter counter, Customer c)
        {
            int amount = 0, rare = 0;
            foreach (var (item, _) in c.Basket)
            {
                amount += item.SellPrice;
                if (item.IsRare) rare++;
            }
            CurrentReport.AddRareSold(rare);
            if (amount > 0)
            {
                State.AddSales(amount);
                CurrentReport.AddSales(amount);
                Emit(new MoneyChangedEvent(Now, amount, MoneyReason.Sale, State.Money));
            }
            Emit(new CheckoutDoneEvent(Now, counter.FixtureId, c.Id, amount));
            Brain.OnCheckedOut(c);
        }

        /// <summary>客の結果が決まった（shousaisekkei.md 8.5）。評判を足して、今月の集計に数える。</summary>
        internal void RecordResult(Customer c, CustomerResult result)
        {
            _reputation.Apply(c, result);
            CurrentReport.AddResult(result);
        }

        internal void Pay(int delta, MoneyReason reason)
        {
            State.AddMoney(delta);
            Emit(new MoneyChangedEvent(Now, delta, reason, State.Money));
        }

        /// <summary>配置が変わったとき（shousaisekkei.md 6.5）。行列のマスを作り直し、客をどかして経路を引き直す。</summary>
        void OnLayoutChanged()
        {
            foreach (var c in _counters) c.RebuildQueueTiles();
            foreach (var c in _customers) Brain.OnLayoutChanged(c);
            Emit(new LayoutChangedEvent(Now));
        }

        internal void Emit(GameEvent e) => _events.Add(e);
    }
}
