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

            UserData userData = SaveManager.GetUserData();
            if (null == userData || null == userData.UnlockedFlipperIds)
                return false;

            return userData.UnlockedFlipperIds.Contains(flipper.GetId());
        }

        public bool IsItemUnlocked(ShopItemData item)
        {
            if (null == item)
                return false;

            if (null == _catalog || _catalog.FindItemTier(item) == ClearRewardCatalogData.INVALID_TIER)
                return true;

            UserData userData = SaveManager.GetUserData();
            if (null == userData || null == userData.UnlockedItemIds)
                return false;

            return userData.UnlockedItemIds.Contains(item.GetId());
        }

        public ClearRewardData GrantClearReward()
        {
            UserData userData = SaveManager.GetUserData();
            if (null == userData)
                return null;

            int tierIndex = userData.ClearCount;
            ++userData.ClearCount;

            ClearRewardData tier = null == _catalog ? null : _catalog.GetTier(tierIndex);
            if (null != tier)
                Unlock(userData, tier);

            SaveManager.SaveUser();
            return tier;
        }

        static void Unlock(UserData userData, ClearRewardData tier)
        {
            if (null == userData.UnlockedFlipperIds)
                userData.UnlockedFlipperIds = new List<string>();

            if (null == userData.UnlockedItemIds)
                userData.UnlockedItemIds = new List<string>();

            for (int i = 0; i < tier.FlipperCount; ++i)
                AddId(userData.UnlockedFlipperIds, tier.GetFlipper(i)?.GetId());

            for (int i = 0; i < tier.ItemCount; ++i)
                AddId(userData.UnlockedItemIds, tier.GetItem(i)?.GetId());
        }

        static void AddId(List<string> ids, string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (ids.Contains(id))
                return;

            ids.Add(id);
        }
    }
}
