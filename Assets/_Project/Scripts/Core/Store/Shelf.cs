namespace Shop.Core
{
    /// <summary>
    /// 棚（特売台も同じクラス。kihonsekkei.md 6.2、shousaisekkei.md 7.1）。
    /// 月の販売上限は持たず、並ぶ数と補充の速さで表す。
    /// </summary>
    public sealed class Shelf
    {
        readonly GameSession _session;
        int _timer;

        public PlacedFixture Fixture { get; }
        public int FixtureId => Fixture.Id;
        public FixtureDef Def => Fixture.Def;
        /// <summary>null = 何も置かない。</summary>
        public ItemDef Item { get; private set; }
        public int Displayed { get; private set; }
        public bool IsBargainTable => Def.IsBargainTable;
        public bool IsFull => Displayed >= Def.DisplayMax;

        internal Shelf(GameSession session, PlacedFixture fixture)
        {
            _session = session;
            Fixture = fixture;
        }

        /// <summary>並べる商品を決める。成功したら倉庫から DisplayMax までまとめて並べる。</summary>
        internal CommandResult Assign(ItemDef itemOrNull)
        {
            if (itemOrNull != null)
            {
                var item = itemOrNull;
                bool categoryOk = IsBargainTable ? item.IsRare : (!item.IsRare && item.Category == Def.ShelfCategory);
                if (!categoryOk) return CommandResult.Fail(FailReason.WrongCategory);
                if (!item.IsRare && item.UnlockStar > _session.State.Star) return CommandResult.Fail(FailReason.Locked);
                if (item.IsRare && _session.Warehouse.Count(item) <= 0 && Item != item) return CommandResult.Fail(FailReason.NotInWarehouse);
                if (item == Item) return CommandResult.Success;
            }
            else if (Item == null)
            {
                return CommandResult.Success;
            }

            ReturnAllToWarehouse();
            Item = itemOrNull;
            _timer = 0;
            if (Item != null)
                while (Displayed < Def.DisplayMax && _session.Warehouse.TryTake(Item)) Displayed++;
            EmitChanged();
            return CommandResult.Success;
        }

        /// <summary>客が 1 個手に取る。</summary>
        internal bool TryTake()
        {
            if (Displayed <= 0) return false;
            Displayed--;
            EmitChanged();
            if (Displayed == 0) _session.Emit(new ShelfEmptiedEvent(_session.Now, FixtureId));
            return true;
        }

        /// <summary>行列で帰った客が戻す。品が違う・満杯なら false（呼び出し側が倉庫へ）。</summary>
        internal bool TryPutBack(ItemDef item)
        {
            if (item != Item || IsFull) return false;
            Displayed++;
            EmitChanged();
            return true;
        }

        internal void ReturnAllToWarehouse()
        {
            if (Item == null || Displayed == 0) return;
            _session.Warehouse.Add(Item, Displayed);
            Displayed = 0;
        }

        /// <summary>補充（kihonsekkei.md 6.2）。</summary>
        internal void Tick()
        {
            if (Item == null) return;
            if (Displayed < Def.DisplayMax && _session.Warehouse.Count(Item) > 0)
            {
                _timer++;
                if (_timer >= Def.RestockTicks)
                {
                    _session.Warehouse.TryTake(Item);
                    Displayed++;
                    _timer = 0;
                    EmitChanged();
                }
            }
            else
            {
                _timer = 0;
            }
        }

        void EmitChanged() => _session.Emit(new ShelfChangedEvent(_session.Now, FixtureId, Item, Displayed));
    }
}
