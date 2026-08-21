using UnityEngine;

namespace Pinball.Ingame.Shop
{
    [CreateAssetMenu(fileName = "ShopCatalogData", menuName = "Pinball/Ingame/ShopCatalogData")]
    public class ShopCatalogData : ScriptableObject
    {
        public const int INVALID_INDEX = -1;

        [SerializeField] ShopItemData[] _items = null;

        public int Count => null == _items ? 0 : _items.Length;

        public ShopItemData GetItem(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _items[index];
        }

        public ShopItemData FindById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            for (int i = 0; i < Count; ++i)
                if (null != _items[i] && _items[i].GetId() == itemId)
                    return _items[i];

            return null;
        }

        public int IndexOf(ShopItemData item)
        {
            if (null == item)
                return INVALID_INDEX;

            for (int i = 0; i < Count; ++i)
                if (_items[i] == item)
                    return i;

            return INVALID_INDEX;
        }
    }
}
