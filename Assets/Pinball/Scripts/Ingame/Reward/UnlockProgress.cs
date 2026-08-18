using System.Collections.Generic;
using Pinball.Save;

namespace Pinball.Ingame.Reward
{
    public class UnlockProgress
    {
        ClearRewardCatalogData _catalog = null;

        public void Init(ClearRewardCatalogData catalog)
        {
            _catalog = catalog;
        }

        public void Release()
        {
            _catalog = null;
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

        public ClearRewardData GrantClearReward()
        {
            ClearRewardData tier = null == _catalog ? null : _catalog.GetTier(SaveManager.ClearCount);
            SaveManager.RegisterRunClear(CollectFlipperIds(tier), CollectItemIds(tier));
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
