using Pinball.Ingame.Shop;
using UnityEngine;

namespace Pinball.Ingame.Reward
{
    [CreateAssetMenu(fileName = "ClearRewardData", menuName = "Pinball/Ingame/ClearRewardData")]
    public class ClearRewardData : ScriptableObject
    {
        [SerializeField] FlipperData[] _flippers = null;
        [SerializeField] ShopItemData[] _items = null;

        public int FlipperCount => null == _flippers ? 0 : _flippers.Length;
        public int ItemCount => null == _items ? 0 : _items.Length;

        public FlipperData GetFlipper(int index)
        {
            if (index < 0 || index >= FlipperCount)
                return null;

            return _flippers[index];
        }

        public ShopItemData GetItem(int index)
        {
            if (index < 0 || index >= ItemCount)
                return null;

            return _items[index];
        }

        public bool ContainsFlipper(FlipperData flipper)
        {
            if (null == flipper)
                return false;

            for (int i = 0; i < FlipperCount; ++i)
                if (_flippers[i] == flipper)
                    return true;

            return false;
        }

        public bool ContainsItem(ShopItemData item)
        {
            if (null == item)
                return false;

            for (int i = 0; i < ItemCount; ++i)
                if (_items[i] == item)
                    return true;

            return false;
        }
    }
}
