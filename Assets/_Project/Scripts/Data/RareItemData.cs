using UnityEngine;

namespace Shop.Data
{
    /// <summary>行商人限定品（youken.md 5.7、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Rare Item")]
    public sealed class RareItemData : ScriptableObject
    {
        [SerializeField] string _id;
        [SerializeField] string _displayName;
        [SerializeField] int _buyPrice;
        [SerializeField] int _sellPrice;
        [SerializeField] int _appearStar = 1;
        [Tooltip("空なら全職業")]
        [SerializeField] JobData _job;
        [SerializeField] int _repBonus;
        [SerializeField] Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
        public int AppearStar => _appearStar;
        public JobData Job => _job;
        public int RepBonus => _repBonus;
        public Sprite Icon => _icon;

#if UNITY_EDITOR
        public void EditorSet(string id, string displayName, int buyPrice, int sellPrice, int appearStar, JobData job, int repBonus)
        {
            _id = id; _displayName = displayName; _buyPrice = buyPrice; _sellPrice = sellPrice;
            _appearStar = appearStar; _job = job; _repBonus = repBonus;
        }
#endif
    }
}
