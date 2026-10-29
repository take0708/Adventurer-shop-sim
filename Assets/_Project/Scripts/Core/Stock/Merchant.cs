using System.Collections.Generic;

namespace Shop.Core
{
    public enum MerchantState { None, Scheduled, Present, Left }

    /// <summary>
    /// 行商人（FR-STOCK-07、kihonsekkei.md 6.2.1、shousaisekkei.md 9.1）。月に一度抽選し、来たら限定品を 1 種類売る。
    /// 月をまたがない（V9）。
    /// </summary>
    public sealed class Merchant
    {
        readonly GameSession _s;
        int _tickInMonth;

        public MerchantState State { get; private set; }
        public ItemDef Item { get; private set; }
        /// <summary>月の中の Tick。</summary>
        public int ArrivalTick { get; private set; }
        public int LeaveTick { get; private set; }
        /// <summary>あと何個買えるか（最初は MerchantMaxBuy）。</summary>
        public int Remaining { get; private set; }
        /// <summary>帰るまでの Tick（HUD の「残り n 秒」用）。いないときは 0。</summary>
        public int TicksLeft => State == MerchantState.Present ? LeaveTick - _tickInMonth : 0;

        internal Merchant(GameSession session)
        {
            _s = session;
        }

        /// <summary>月の初めに 1 回。乱数は「来る → 品 → 時刻」の順に引く。</summary>
        internal void PlanMonth(int star)
        {
            State = MerchantState.None;
            Item = null;
            Remaining = 0;
            _tickInMonth = 0;
            var b = _s.Config.Balance;
            if (_s.Random.NextDouble() >= _s.Config.StarOf(star).MerchantChancePercent / 100.0) return;

            // ★1 は ★1 の品だけになる
            var candidates = new List<ItemDef>();
            foreach (var i in _s.Config.Items)
                if (i.IsRare && star - 1 <= i.UnlockStar && i.UnlockStar <= star) candidates.Add(i);
            if (candidates.Count == 0) return;
            Item = candidates[_s.Random.Range(0, candidates.Count)];
            ArrivalTick = _s.Random.Range(b.MerchantArriveMinTick, b.MerchantArriveMaxTick);
            LeaveTick = ArrivalTick + b.MerchantStayTicks;
            Remaining = b.MerchantMaxBuy;
            State = MerchantState.Scheduled;
        }

        /// <summary>デバッグ用: 次の Tick で来るようにする（品は抽選）。月をまたがないよう帰る時刻を月末で切る。</summary>
        internal void DebugArriveNext(int star, int tickInMonth)
        {
            if (State == MerchantState.Present) return;
            var candidates = new List<ItemDef>();
            foreach (var i in _s.Config.Items)
                if (i.IsRare && star - 1 <= i.UnlockStar && i.UnlockStar <= star) candidates.Add(i);
            if (candidates.Count == 0) return;
            Item = candidates[_s.Random.Range(0, candidates.Count)];
            ArrivalTick = tickInMonth;
            LeaveTick = System.Math.Min(tickInMonth + _s.Config.Balance.MerchantStayTicks, _s.Config.Time.TicksPerMonth - 1);
            Remaining = _s.Config.Balance.MerchantMaxBuy;
            State = MerchantState.Scheduled;
        }

        internal void Tick(int tickInMonth)
        {
            _tickInMonth = tickInMonth;
            if (State == MerchantState.Scheduled && tickInMonth == ArrivalTick)
            {
                State = MerchantState.Present;
                _s.Emit(new MerchantArrivedEvent(_s.Now, Item, LeaveTick));
            }
            else if (State == MerchantState.Present && tickInMonth == LeaveTick)
            {
                State = MerchantState.Left;
                _s.Emit(new MerchantLeftEvent(_s.Now));
            }
        }

        /// <summary>特売台がなくても買える。Remaining が 0 になっても LeaveTick まではいる（見た目のため）。</summary>
        internal CommandResult Buy(int count)
        {
            if (State != MerchantState.Present) return CommandResult.Fail(FailReason.MerchantGone);
            if (count < 1 || count > Remaining) return CommandResult.Fail(FailReason.InvalidCount);
            int total = Item.BuyPrice * count;
            if (_s.State.Money < total) return CommandResult.Fail(FailReason.NotEnoughMoney);

            _s.Pay(-total, MoneyReason.Purchase);
            _s.Warehouse.Add(Item, count);
            _s.CurrentReport.AddPurchase(total);
            Remaining -= count;
            return CommandResult.Success;
        }
    }
}
