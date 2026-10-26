using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>店の状態（shousaisekkei.md 5.1）。</summary>
    public sealed class ShopState
    {
        readonly int[] _starReachedMonth;

        public int Money { get; private set; }
        /// <summary>評判ポイント。0 以上。</summary>
        public int Reputation { get; private set; }
        /// <summary>到達した最高の星（下がらない）。</summary>
        public int Star { get; private set; } = 1;
        /// <summary>総売上（スコア）。払い戻しは含まない。</summary>
        public int TotalSales { get; private set; }
        public int FreeShelvesLeft { get; private set; }
        /// <summary>[0]=★1 は 1。その星の客が来始める月。未到達は 0。</summary>
        public IReadOnlyList<int> StarReachedMonth => _starReachedMonth;

        internal ShopState(int money, int freeShelves, int maxStar)
        {
            Money = money;
            FreeShelvesLeft = freeShelves;
            _starReachedMonth = new int[maxStar];
            _starReachedMonth[0] = 1;
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

        /// <summary>0 で止める（K-12）。実際に変わった量を返す。</summary>
        internal int AddReputation(int delta)
        {
            int before = Reputation;
            Reputation = Math.Max(0, Reputation + delta);
            return Reputation - before;
        }

        /// <summary>星を上げる。間の星も month に到達したことにする。</summary>
        internal void RaiseStar(int star, int month)
        {
            if (star <= Star) return;
            for (int s = Star + 1; s <= star; s++) _starReachedMonth[s - 1] = month;
            Star = star;
        }

        internal void UseFreeShelf()
        {
            if (FreeShelvesLeft <= 0) throw new InvalidOperationException("無料枠が残っていない");
            FreeShelvesLeft--;
        }
    }
}
