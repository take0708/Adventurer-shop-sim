using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// Core の窓口（shousaisekkei.md 4 章）。Tick で進め、コマンドで操作し、DrainEvents でイベントを受け取る。
    /// M2 では時間と配置のコマンドだけ。客・棚・会計・仕入れ・月末は M3 以降で足す。
    /// </summary>
    public sealed class GameSession
    {
        readonly PlacementValidator _validator;
        List<GameEvent> _events = new List<GameEvent>();
        List<GameEvent> _drained = new List<GameEvent>();

        public GameConfig Config { get; }
        public GameClock Clock { get; }
        public ShopState State { get; }
        public ShopLayout Layout { get; }
        public bool IsEnded => Clock.IsFinished;

        /// <summary>乱数。M3 以降で客や行商人が使う（shousaisekkei.md 1.3）。</summary>
        internal IRandom Random { get; }

        public GameSession(GameConfig config, ulong seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Random = new SeededRandom(seed);
            Clock = new GameClock(config.Time);
            State = new ShopState(config.Balance.StartMoney, config.Balance.FreeShelfCount);
            Layout = new ShopLayout(config);
            _validator = new PlacementValidator(config, Layout, State);

            // 初期配置は無料で置く。無料枠は減らさない（shousaisekkei.md 4.2）
            foreach (var (def, origin) in config.Balance.InitialLayout) Layout.Add(def, origin, 0);

            Emit(new SessionStartedEvent(Clock.ElapsedTicks));
        }

        /// <summary>1 Tick 進める。終了後は何もしない（shousaisekkei.md 4.3）。</summary>
        public void Tick()
        {
            if (IsEnded) return;
            var clockEvents = Clock.Advance();
            if ((clockEvents & ClockEvents.DayChanged) != 0) Emit(new DayChangedEvent(Clock.ElapsedTicks, Clock.Day, Clock.Month));
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

        // ---- コマンド（shousaisekkei.md 6.4、10.2） ----

        public CommandResult PlaceFixture(FixtureDef def, Tile origin)
        {
            var r = CanPlace(def, origin);
            if (!r.Ok) return r;

            int cost = PlacementCost(def);
            if (cost == 0 && def.Kind == FixtureKind.Shelf && def.UsesFreeShelf && def.Cost > 0) State.UseFreeShelf();
            if (cost > 0) Pay(-cost, MoneyReason.Equipment);

            Layout.Add(def, origin, cost);
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

            int refund = RefundOf(fixtureId);
            if (refund > 0) Pay(refund, MoneyReason.Refund);
            Layout.Remove(Layout.FindById(fixtureId));
            OnLayoutChanged();
            return CommandResult.Success;
        }

        public CommandResult ExpandShop()
        {
            if (IsEnded) return CommandResult.Fail(FailReason.GameOver);
            if (Layout.Stage >= Config.MaxStage) return CommandResult.Fail(FailReason.MaxStage);
            int cost = NextExpansionCost;
            if (State.Money < cost) return CommandResult.Fail(FailReason.NotEnoughMoney);

            if (cost > 0) Pay(-cost, MoneyReason.Expansion);
            Layout.Expand();
            Emit(new ShopExpandedEvent(Clock.ElapsedTicks, Layout.Stage));
            OnLayoutChanged();
            return CommandResult.Success;
        }

        void Pay(int delta, MoneyReason reason)
        {
            State.AddMoney(delta);
            Emit(new MoneyChangedEvent(Clock.ElapsedTicks, delta, reason, State.Money));
        }

        /// <summary>配置が変わったとき（shousaisekkei.md 6.5）。客・行列の直しは M3 で足す。</summary>
        void OnLayoutChanged()
        {
            Emit(new LayoutChangedEvent(Clock.ElapsedTicks));
        }

        void Emit(GameEvent e) => _events.Add(e);
    }
}
