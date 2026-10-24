using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>店の広さの段階。[0] が段階 1（youken.md 5.6、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Shop Size Table")]
    public sealed class ShopSizeTable : ScriptableObject
    {
        [Serializable]
        public struct Row
        {
            public int width;
            public int height;
            public int shelfMax;
            public int counterMax;
            public int cost;
        }

        [SerializeField] List<Row> _rows = new List<Row>();

        public IReadOnlyList<Row> Rows => _rows;

#if UNITY_EDITOR
        public void EditorSet(List<Row> rows) => _rows = rows;
#endif
    }
}
