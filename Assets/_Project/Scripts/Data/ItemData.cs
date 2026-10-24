using Shop.Core;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>問屋の品（youken.md 5.1、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Item")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] string _id;
        [SerializeField] string _displayName;
        [SerializeField] ItemCategory _category;
        [SerializeField] int _buyPrice;
        [SerializeField] int _sellPrice;
        [SerializeField] int _unlockStar = 1;
        [SerializeField] Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ItemCategory Category => _category;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
        public int UnlockStar => _unlockStar;
        public Sprite Icon => _icon;

#if UNITY_EDITOR
        public void EditorSet(string id, string displayName, ItemCategory category, int buyPrice, int sellPrice, int unlockStar)
        {
            _id = id; _displayName = displayName; _category = category;
            _buyPrice = buyPrice; _sellPrice = sellPrice; _unlockStar = unlockStar;
        }
#endif
    }
}
