using System;
using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>客の職業（shousaisekkei.md 3.1）。</summary>
    public sealed class JobDef
    {
        public string Id { get; }
        public string Name { get; }
        /// <summary>欲しがる問屋の品と重み。重み 0 の品は入れない。</summary>
        public IReadOnlyList<(ItemDef Item, int Weight)> Wants { get; }

        public JobDef(string id, string name, IEnumerable<(ItemDef Item, int Weight)> wants)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            if (wants == null) throw new ArgumentNullException(nameof(wants));
            var list = new List<(ItemDef Item, int Weight)>();
            foreach (var w in wants)
            {
                if (w.Item == null) throw new ArgumentNullException(nameof(wants));
                if (w.Weight <= 0) throw new ArgumentOutOfRangeException(nameof(wants), $"{id}: {w.Item.Id} の重みが 0 以下");
                list.Add(w);
            }
            Wants = list;
        }

        public override string ToString() => Id;
    }
}
