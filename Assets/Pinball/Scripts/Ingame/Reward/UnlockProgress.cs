using System.Collections.Generic;
using Pinball.Save;

namespace Pinball.Ingame.Reward
{
    public class UnlockProgress
    {
        ClearRewardCatalogData _catalog = null;

        public UnlockProgress(ClearRewardCatalogData catalog)
        {
            _catalog = catalog;
        }

        public int GetFlipperUnlockTier(FlipperData flipper)
        {
            if (null == _catalog)
                return ClearRewardCatalogData.INVALID_TIER;

            return _catalog.FindFlipperTier(flipper);
        }

        public bool IsFlipperUnlocked(FlipperData flipper)
        {
            if (null == flipper)
                return false;

            if (GetFlipperUnlockTier(flipper) == ClearRewardCatalogData.INVALID_TIER)
                return true;

            return SaveManager.IsFlipperUnlocked(flipper.GetId());
        }

        public bool IsItemUnlocked(ShopItemData item)
        {
            if (null == item)
                return false;

            if (null == _catalog || _catalog.FindItemTier(item) == ClearRewardCatalogData.INVALID_TIER)
                return true;

            return SaveManager.IsItemUnlocked(item.GetId());
        }

        public void GrantClearReward()
        {
            int tierIndex = SaveManager.ClearCount;
            ClearRewardData tier = null == _catalog ? null : _catalog.GetTier(tierIndex);
            SaveManager.RegisterRunClear(
                CollectFlipperIds(tier),
                CollectItemIds(tier),
                null == tier ? UserData.NO_PENDING_REWARD_TIER : tierIndex);
        }

        public ClearRewardData ConsumePendingReward()
        {
            ClearRewardData tier = null == _catalog ? null : _catalog.GetTier(SaveManager.PendingRewardTier);
            SaveManager.ConsumePendingReward();
            return tier;
        }

        static List<string> CollectFlipperIds(ClearRewardData tier)
        {
            List<string> ids = new List<string>();
            if (null == tier)
                return ids;

            for (int i = 0; i < tier.FlipperCount; ++i)
                AddId(ids, tier.GetFlipper(i));

            return ids;
        }

        static List<string> CollectItemIds(ClearRewardData tier)
        {
            List<string> ids = new List<string>();
            if (null == tier)
                return ids;

            for (int i = 0; i < tier.ItemCount; ++i)
                AddId(ids, tier.GetItem(i));

            return ids;
        }

        static void AddId(List<string> ids, FlipperData flipper)
        {
            if (null != flipper)
                ids.Add(flipper.GetId());
        }

        static void AddId(List<string> ids, ShopItemData item)
        {
            if (null != item)
                ids.Add(item.GetId());
        }
    }
}
