using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>星の段階ごとの値。[0] が ★1（youken.md 5.2, 5.4, 5.7、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Star Table")]
    public sealed class StarTable : ScriptableObject
    {
        [Serializable]
        public struct Row
        {
            public int requiredRep;
            public int baseVisits;
            public int budgetMin;
            public int budgetMax;
            public int merchantChancePercent;
        }

        [SerializeField] List<Row> _rows = new List<Row>();

        public IReadOnlyList<Row> Rows => _rows;

#if UNITY_EDITOR
        public void EditorSet(List<Row> rows) => _rows = rows;
#endif
    }
}
