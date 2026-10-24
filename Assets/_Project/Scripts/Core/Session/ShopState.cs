using System;

namespace Shop.Core
{
    /// <summary>
    /// 店の状態（shousaisekkei.md 5.1）。
    /// M2 ではお金と無料枠だけ。評判・星・売上は M3〜M5 で足す。
    /// </summary>
    public sealed class ShopState
    {
        public int Money { get; private set; }
        public int FreeShelvesLeft { get; private set; }

        internal ShopState(int money, int freeShelves)
        {
            Money = money;
            FreeShelvesLeft = freeShelves;
        }

        /// <summary>結果が負になるなら InvalidOperationException（呼ぶ前に確かめる前提）。</summary>
        internal void AddMoney(int delta)
        {
            long next = (long)Money + delta;
            if (next < 0) throw new InvalidOperationException($"所持金が負になる（{Money} + {delta}）");
            if (next > int.MaxValue) throw new OverflowException("所持金があふれる");
            Money = (int)next;
        }

        internal void UseFreeShelf()
        {
            if (FreeShelvesLeft <= 0) throw new InvalidOperationException("無料枠が残っていない");
            FreeShelvesLeft--;
        }
    }
}
