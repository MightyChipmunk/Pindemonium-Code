using UnityEngine;

namespace Pinball.Ingame.Reward
{
    [CreateAssetMenu(fileName = "ClearRewardCatalogData", menuName = "Pinball/Ingame/ClearRewardCatalogData")]
    public class ClearRewardCatalogData : ScriptableObject
    {
        public const int INVALID_TIER = -1;

        [SerializeField] ClearRewardData[] _tiers = null;

        public int TierCount => null == _tiers ? 0 : _tiers.Length;

        public ClearRewardData GetTier(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= TierCount)
                return null;

            return _tiers[tierIndex];
        }

        public int FindFlipperTier(FlipperData flipper)
        {
            for (int i = 0; i < TierCount; ++i)
                if (null != _tiers[i] && _tiers[i].ContainsFlipper(flipper))
                    return i;

            return INVALID_TIER;
        }

        public int FindItemTier(ShopItemData item)
        {
            for (int i = 0; i < TierCount; ++i)
                if (null != _tiers[i] && _tiers[i].ContainsItem(item))
                    return i;

            return INVALID_TIER;
        }
    }
}
