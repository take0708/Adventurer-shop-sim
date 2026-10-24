using Shop.Core;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>配置物（youken.md 5.5、kihonsekkei.md 4.2, 6.2、shousaisekkei.md 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Fixture")]
    public sealed class FixtureData : ScriptableObject
    {
        [SerializeField] string _id;
        [SerializeField] string _displayName;
        [SerializeField] FixtureKind _kind;
        [Tooltip("棚のとき。特売台は Rare")]
        [SerializeField] ItemCategory _shelfCategory;
        [SerializeField] int _cost;
        [SerializeField] int _width = 1;
        [SerializeField] int _height = 1;
        [SerializeField] bool _walkable;
        [SerializeField] int _displayMax;
        [SerializeField] int _restockTicks;
        [SerializeField] int _ambience;
        [Tooltip("0 = 広さの段階の上限に従う")]
        [SerializeField] int _maxCount;
        [SerializeField] bool _countsTowardShelfLimit;
        [SerializeField] bool _usesFreeShelf;
        [SerializeField] Sprite _sprite;
        [Tooltip("素材が決まるまでの仮の色")]
        [SerializeField] Color _placeholderColor = Color.gray;

        public string Id => _id;
        public string DisplayName => _displayName;
        public FixtureKind Kind => _kind;
        public ItemCategory ShelfCategory => _shelfCategory;
        public int Cost => _cost;
        public int Width => _width;
        public int Height => _height;
        public bool Walkable => _walkable;
        public int DisplayMax => _displayMax;
        public int RestockTicks => _restockTicks;
        public int Ambience => _ambience;
        public int MaxCount => _maxCount;
        public bool CountsTowardShelfLimit => _countsTowardShelfLimit;
        public bool UsesFreeShelf => _usesFreeShelf;
        public Sprite Sprite => _sprite;
        public Color PlaceholderColor => _placeholderColor;

#if UNITY_EDITOR
        public void EditorSet(string id, string displayName, FixtureKind kind, ItemCategory shelfCategory, int cost,
            int width, int height, bool walkable, int displayMax, int restockTicks, int ambience,
            int maxCount, bool countsTowardShelfLimit, bool usesFreeShelf, Color placeholderColor)
        {
            _id = id; _displayName = displayName; _kind = kind; _shelfCategory = shelfCategory; _cost = cost;
            _width = width; _height = height; _walkable = walkable; _displayMax = displayMax; _restockTicks = restockTicks;
            _ambience = ambience; _maxCount = maxCount; _countsTowardShelfLimit = countsTowardShelfLimit;
            _usesFreeShelf = usesFreeShelf; _placeholderColor = placeholderColor;
        }
#endif
    }
}
