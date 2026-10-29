using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>設定の食い違いを起動時に見つける（shousaisekkei.md 3.3 V1〜V9、V10 は M7 で足した）。</summary>
    public static class ConfigValidator
    {
        /// <summary>問題があれば InvalidOperationException。メッセージに全部の問題を並べる。</summary>
        public static void Validate(GameConfig config)
        {
            var errors = Check(config);
            if (errors.Count > 0)
                throw new InvalidOperationException("GameConfig に問題があります:\n- " + string.Join("\n- ", errors));
        }

        /// <summary>問題の一覧。空なら問題なし。</summary>
        public static List<string> Check(GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var errors = new List<string>();
            var b = config.Balance;

            // V1 ID の重複
            CheckUnique(config.Items, i => i.Id, "商品", errors);
            CheckUnique(config.Jobs, j => j.Id, "職業", errors);
            CheckUnique(config.Fixtures, f => f.Id, "配置物", errors);

            // V2 星
            if (config.Stars.Count == 0) errors.Add("V2: 星が 1 つもない");
            for (int i = 0; i < config.Stars.Count; i++)
            {
                var s = config.Stars[i];
                if (s.Star != i + 1) errors.Add($"V2: 星の番号が連番でない（{i + 1} 番目が ★{s.Star}）");
                if (i == 0 && s.RequiredRep != 0) errors.Add("V2: ★1 の必要評判が 0 でない");
                if (i > 0 && s.RequiredRep <= config.Stars[i - 1].RequiredRep) errors.Add($"V2: ★{s.Star} の必要評判が前の星以下");
                if (s.BudgetMin > s.BudgetMax) errors.Add($"V2: ★{s.Star} の予算が Min > Max");
            }

            // V3 広さ
            if (config.ShopSizes.Count == 0) errors.Add("V3: 広さの段階が 1 つもない");
            for (int i = 0; i < config.ShopSizes.Count; i++)
            {
                var s = config.ShopSizes[i];
                if (s.Stage != i + 1) errors.Add($"V3: 広さの段階が連番でない（{i + 1} 番目が段階 {s.Stage}）");
                if (i > 0 && (s.Width <= config.ShopSizes[i - 1].Width || s.Height <= config.ShopSizes[i - 1].Height))
                    errors.Add($"V3: 段階 {s.Stage} が前の段階より縦横とも広がっていない");
                var e = b.Entrance;
                if (e.X < 0 || e.Y < 0 || e.X >= s.Width || e.Y >= s.Height) errors.Add($"V3: 入口 {e} が段階 {s.Stage} の外");
            }

            // V4 ★1 の客が欲しいものを持てる
            foreach (var job in config.Jobs)
            {
                int sum = 0;
                foreach (var (item, weight) in job.Wants) if (!item.IsRare && item.UnlockStar <= 1) sum += weight;
                if (sum < 1) errors.Add($"V4: {job.Id} が ★1 で欲しがる品がない");
                foreach (var (item, _) in job.Wants)
                    if (item.IsRare) errors.Add($"V4: {job.Id} の欲しいものに限定品 {item.Id} がある");
            }

            // V5 棚の種類と商品の系統
            foreach (ItemCategory cat in Enum.GetValues(typeof(ItemCategory)))
            {
                int n = 0;
                foreach (var f in config.Fixtures) if (f.Kind == FixtureKind.Shelf && f.ShelfCategory == cat) n++;
                if (n != 1) errors.Add($"V5: {cat} の棚が {n} 種類（1 種類のはず）");
            }
            bool seenRare = false;
            foreach (var item in config.Items)
            {
                if (item.IsRare) seenRare = true;
                else if (seenRare) errors.Add($"V5: 問屋の品 {item.Id} が限定品より後ろにある");
                if (!item.IsRare && (item.RareJob != null || item.RareRepBonus != 0)) errors.Add($"V5: 問屋の品 {item.Id} に限定品の値がある");
            }

            // V6 限定品の出始める星
            if (config.Stars.Count > 0)
            {
                var perStar = new int[config.MaxStar + 1];
                foreach (var item in config.Items)
                {
                    if (!item.IsRare) continue;
                    if (item.UnlockStar < 1 || item.UnlockStar > config.MaxStar) errors.Add($"V6: 限定品 {item.Id} の星 {item.UnlockStar} が範囲外");
                    else perStar[item.UnlockStar]++;
                }
                for (int s = 1; s <= config.MaxStar; s++)
                    if (perStar[s] == 0) errors.Add($"V6: ★{s} で出始める限定品がない");
            }

            // V8 棚の並ぶ数と補充間隔
            foreach (var f in config.Fixtures)
                if (f.Kind == FixtureKind.Shelf && (f.DisplayMax < 1 || f.RestockTicks < 1))
                    errors.Add($"V8: 棚 {f.Id} の DisplayMax か RestockTicks が 0 以下");

            // V9 行商人が月をまたがない
            if (b.MerchantArriveMaxTick + b.MerchantStayTicks > config.Time.TicksPerMonth)
                errors.Add("V9: 行商人が月をまたぐ（MerchantArriveMaxTick + MerchantStayTicks > TicksPerMonth）");

            // V10 年末講評の境目が年の数だけあり、Good < Excellent
            int years = config.Time.TotalMonths / config.Time.MonthsPerYear;
            if (b.ReviewGoodRep.Count != years || b.ReviewExcellentRep.Count != years)
                errors.Add($"V10: 年末講評の境目が {years} 年分ない");
            else
                for (int y = 0; y < years; y++)
                    if (b.ReviewGoodRep[y] >= b.ReviewExcellentRep[y]) errors.Add($"V10: {y + 1} 年目の講評の境目が Good >= Excellent");

            // V7 初期配置が段階 1 に置ける。広さと入口が正しいときだけ確かめる
            if (errors.Count == 0) CheckInitialLayout(config, errors);

            return errors;
        }

        static void CheckInitialLayout(GameConfig config, List<string> errors)
        {
            var layout = new ShopLayout(config);
            var validator = new PlacementValidator(config, layout, null);
            foreach (var (def, origin) in config.Balance.InitialLayout)
            {
                var r = validator.CanPlace(def, origin);
                if (!r.Ok)
                {
                    errors.Add($"V7: 初期配置 {def.Id} {origin} が置けない（{r.Reason}）");
                    return;
                }
                layout.Add(def, origin, 0);
            }
            if (layout.CounterCount == 0) errors.Add("V7: 初期配置にカウンターがない");
        }

        static void CheckUnique<T>(IReadOnlyList<T> list, Func<T, string> id, string label, List<string> errors)
        {
            var seen = new HashSet<string>();
            foreach (var x in list)
                if (!seen.Add(id(x))) errors.Add($"V1: {label}の ID {id(x)} が重複");
        }
    }
}
