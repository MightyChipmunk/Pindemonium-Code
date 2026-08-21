using Pinball.Ingame.Shop;
using Pinball.UI;

namespace Pinball.Ingame.Reward
{
    public static class ClearRewardUnlockPresenter
    {
        public static bool TryShow(ClearRewardData reward)
        {
            if (null == reward)
                return false;

            int entryCount = reward.FlipperCount + reward.ItemCount;
            if (entryCount <= 0)
                return false;

            RewardUnlockView.Entry[] entries = new RewardUnlockView.Entry[entryCount];
            for (int i = 0; i < reward.FlipperCount; ++i)
                entries[i] = BuildFlipperEntry(reward.GetFlipper(i));

            for (int i = 0; i < reward.ItemCount; ++i)
                entries[reward.FlipperCount + i] = BuildItemEntry(reward.GetItem(i));

            UIManager.Show(new RewardUnlockView.Args(entries));
            return true;
        }

        static RewardUnlockView.Entry BuildFlipperEntry(FlipperData flipper)
        {
            if (null == flipper)
                return null;

            return new RewardUnlockView.Entry(
                Kind: RewardUnlockView.ERewardKind.Flipper,
                Icon: flipper.GetIcon(),
                NameKey: flipper.GetNameKey(),
                DescriptionKey: flipper.GetAbilityKey());
        }

        static RewardUnlockView.Entry BuildItemEntry(ShopItemData item)
        {
            if (null == item)
                return null;

            bool isPassive = item.Kind == ShopItemData.EItemKind.Passive;
            return new RewardUnlockView.Entry(
                Kind: isPassive ? RewardUnlockView.ERewardKind.Passive : RewardUnlockView.ERewardKind.Upgrade,
                Icon: item.GetIcon(),
                NameKey: item.GetNameKey(),
                DescriptionKey: item.GetDescriptionKey());
        }
    }
}
