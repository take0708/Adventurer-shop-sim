using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>問屋。星に応じた品揃え（FR-STOCK-06、shousaisekkei.md 5.4）。</summary>
    internal sealed class Wholesaler
    {
        readonly GameSession _s;

        public Wholesaler(GameSession session)
        {
            _s = session;
        }

        /// <summary>問屋の品で UnlockStar &lt;= star のもの（Config.Items の順）。</summary>
        public IReadOnlyList<ItemDef> Unlocked(int star)
        {
            var list = new List<ItemDef>();
            foreach (var i in _s.Config.Items) if (!i.IsRare && i.UnlockStar <= star) list.Add(i);
            return list;
        }

        public CommandResult Buy(ItemDef item, int count)
        {
            if (count < 1) return CommandResult.Fail(FailReason.InvalidCount);
            if (item == null || item.IsRare) return CommandResult.Fail(FailReason.NotSoldHere);
            if (item.UnlockStar > _s.State.Star) return CommandResult.Fail(FailReason.Locked);
            long total = (long)item.BuyPrice * count;
            if (_s.State.Money < total) return CommandResult.Fail(FailReason.NotEnoughMoney);

            // 並べる棚が空いていても、すぐには並べない（補充の速さで並ぶ）
            _s.Pay(-(int)total, MoneyReason.Purchase);
            _s.Warehouse.Add(item, count);
            _s.CurrentReport.AddPurchase((int)total);
            return CommandResult.Success;
        }
    }
}
