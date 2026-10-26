using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>
    /// 客の中身（職業・欲しいもの・予算）を決める（shousaisekkei.md 8.2）。
    /// 乱数を引く順番はシミュレーションの make_customer と同じ（職業 → 1 品か 2 品か → 品 → 予算）。
    /// </summary>
    internal sealed class CustomerFactory
    {
        readonly GameConfig _config;
        readonly IRandom _random;

        public CustomerFactory(GameConfig config, IRandom random)
        {
            _config = config;
            _random = random;
        }

        /// <summary>客の中身。shelves は掘り出し物の判定に使う（乱数は引かない）。</summary>
        public (JobDef Job, List<Want> Wants, int Budget) Create(int star, IReadOnlyList<Shelf> shelves)
        {
            var job = _config.Jobs[_random.Range(0, _config.Jobs.Count)];

            var candidates = new List<(ItemDef Item, int Weight)>();
            foreach (var w in job.Wants)
                if (!w.Item.IsRare && w.Item.UnlockStar <= star) candidates.Add(w);

            int n = _random.NextDouble() < _config.Balance.TwoWantsChancePercent / 100.0 ? 2 : 1;
            var wants = new List<Want>();
            for (int i = 0; i < n; i++)
            {
                var item = WeightedPicker.Pick(candidates, _random);
                if (item == null) continue;
                bool dup = false;
                foreach (var w in wants) if (w.Item == item) dup = true;
                if (!dup) wants.Add(new Want(item, false));
            }

            var starDef = _config.StarOf(star);
            int budget = _random.Range(starDef.BudgetMin, starDef.BudgetMax + 1);

            var bargain = FindBargain(job, budget, shelves);
            if (bargain != null) wants.Insert(0, new Want(bargain, true));
            return (job, wants, budget);
        }

        /// <summary>特売台（Id 順）に並んでいて、職業が合い、予算内の最初の限定品。</summary>
        static ItemDef FindBargain(JobDef job, int budget, IReadOnlyList<Shelf> shelves)
        {
            if (shelves == null) return null;
            foreach (var s in shelves)
            {
                if (!s.IsBargainTable || s.Item == null || s.Displayed <= 0) continue;
                var item = s.Item;
                if ((item.RareJob == null || item.RareJob == job) && item.SellPrice <= budget) return item;
            }
            return null;
        }
    }
}
