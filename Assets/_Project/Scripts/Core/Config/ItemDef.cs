using System;

namespace Shop.Core
{
    public enum ItemCategory { Tool, Weapon, Armor, Rare }

    /// <summary>商品（問屋の品と行商人限定品。shousaisekkei.md 3.1）。</summary>
    public sealed class ItemDef
    {
        public string Id { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        /// <summary>問屋の品: 解禁の星 / 限定品: 出始める星。</summary>
        public int UnlockStar { get; }
        public bool IsRare => Category == ItemCategory.Rare;
        /// <summary>限定品を買う職業。null = 全職業。問屋の品では null。</summary>
        public JobDef RareJob { get; }
        /// <summary>限定品が売れたときの評判。問屋の品では 0。</summary>
        public int RareRepBonus { get; }

        public ItemDef(string id, string name, ItemCategory category, int buyPrice, int sellPrice, int unlockStar,
            JobDef rareJob = null, int rareRepBonus = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            Category = category;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            UnlockStar = unlockStar;
            RareJob = rareJob;
            RareRepBonus = rareRepBonus;
        }

        public override string ToString() => Id;
    }
}
