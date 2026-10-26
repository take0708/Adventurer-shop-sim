using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>評判の増減と星の判定（kihonsekkei.md 5.5, 7.2、shousaisekkei.md 5.2）。</summary>
    internal sealed class ReputationSystem
    {
        readonly GameSession _s;
        readonly BalanceDef _b;

        public ReputationSystem(GameSession session)
        {
            _s = session;
            _b = session.Config.Balance;
        }

        /// <summary>結果ごとの増減（掘り出し物のボーナスは含まない）。</summary>
        public int RepFor(CustomerResult result)
        {
            switch (result)
            {
                case CustomerResult.Full: return _b.RepFull;
                case CustomerResult.Partial: return _b.RepPartial;
                case CustomerResult.None: return _b.RepNone;
                default: return _b.RepQueueLeft;
            }
        }

        /// <summary>結果＋会計した限定品のボーナスを 1 回にまとめて足し、イベントを積む。</summary>
        public void Apply(Customer c, CustomerResult result)
        {
            int bonus = 0;
            if (result != CustomerResult.QueueLeft)
                foreach (var (item, _) in c.Basket)
                    if (item.IsRare) bonus += item.RareRepBonus;

            int delta = _s.State.AddReputation(RepFor(result) + bonus);
            _s.Emit(new ReputationChangedEvent(_s.Now, delta, _s.State.Reputation, result, bonus));
        }

        /// <summary>必要評判を満たす最大の星。</summary>
        public int StarFor(int reputation)
        {
            int star = 1;
            foreach (var s in _s.Config.Stars)
                if (reputation >= s.RequiredRep) star = s.Star;
            return star;
        }

        /// <summary>次の星までの残り。最大の星なら 0。</summary>
        public int RepToNextStar(int reputation, int star)
        {
            if (star >= _s.Config.MaxStar) return 0;
            return System.Math.Max(0, _s.Config.StarOf(star + 1).RequiredRep - reputation);
        }

        /// <summary>
        /// 月末に呼ぶ。評判から求めた星が今より高ければ上げる（一度に 2 段階でもよい）。
        /// month はその星の客が来始める月（翌月）。解禁された問屋の品を返す。
        /// </summary>
        public bool TryRaiseStar(int month, out int newStar, out List<ItemDef> unlocked)
        {
            int before = _s.State.Star;
            newStar = StarFor(_s.State.Reputation);
            unlocked = new List<ItemDef>();
            if (newStar <= before)
            {
                newStar = before;
                return false;
            }
            foreach (var item in _s.Config.Items)
                if (!item.IsRare && item.UnlockStar > before && item.UnlockStar <= newStar) unlocked.Add(item);
            _s.State.RaiseStar(newStar, month);
            return true;
        }
    }
}
