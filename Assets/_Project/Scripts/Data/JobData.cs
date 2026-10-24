using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>客の職業と欲しいもの（youken.md 5.3、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Job")]
    public sealed class JobData : ScriptableObject
    {
        [Serializable]
        public struct Want
        {
            public ItemData item;
            public int weight;
        }

        [SerializeField] string _id;
        [SerializeField] string _displayName;
        [SerializeField] List<Want> _wants = new List<Want>();
        [SerializeField] Sprite _sprite;

        public string Id => _id;
        public string DisplayName => _displayName;
        public IReadOnlyList<Want> Wants => _wants;
        public Sprite Sprite => _sprite;

#if UNITY_EDITOR
        public void EditorSet(string id, string displayName, List<Want> wants)
        {
            _id = id; _displayName = displayName; _wants = wants;
        }
#endif
    }
}
