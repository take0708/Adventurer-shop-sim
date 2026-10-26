using System;

namespace Shop.Core
{
    /// <summary>
    /// 店の状態（shousaisekkei.md 5.1）。
    /// M3 まではお金・売上・無料枠。星は ★1 のまま（評判と星は M5 で足す）。
    /// </summary>
    public sealed class ShopState
    {
        public int Money { get; private set; }
        /// <summary>総売上（スコア）。払い戻しは含まない。</summary>
        public int TotalSales { get; private set; }
        /// <summary>到達した最高の星（下がらない）。</summary>
        public int Star { get; private set; } = 1;
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

        /// <summary>Money と TotalSales の両方に足す。</summary>
        internal void AddSales(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            AddMoney(amount);
            TotalSales += amount;
        }

        internal void UseFreeShelf()
        {
            if (FreeShelvesLeft <= 0) throw new InvalidOperationException("無料枠が残っていない");
            FreeShelvesLeft--;
        }
    }
}
