using System;
using System.Collections.Generic;
using Pinball.Ingame.Reward;
using Pinball.Ingame.UI;
using Pinball.UI;

namespace Pinball.Ingame.Presentation
{
    public class IngameResultPresenter
    {
        public record RunSummary(int StageNumber, int Score, int Money, int UsedBallCount);

        const int PASSIVE_OWNED_COUNT = 1;

        PlayerLoadout _loadout = null;
        Action<List<OwnedItemsView.Entry>, List<OwnedItemsView.Entry>> _ownedItemsPresenter = null;
        List<OwnedItemsView.Entry> _passiveEntries = null;
        List<OwnedItemsView.Entry> _upgradeEntries = null;

        public IngameResultPresenter(PlayerLoadout loadout, Action<List<OwnedItemsView.Entry>, List<OwnedItemsView.Entry>> ownedItemsPresenter)
        {
            _loadout = loadout;
            _ownedItemsPresenter = ownedItemsPresenter;
            _passiveEntries = new List<OwnedItemsView.Entry>();
            _upgradeEntries = new List<OwnedItemsView.Entry>();
        }

        public void RefreshOwnedItems()
        {
            if (null == _passiveEntries || null == _upgradeEntries || null == _loadout)
                return;

            _passiveEntries.Clear();
            for (int i = 0; i < _loadout.PassiveCount; ++i)
                _passiveEntries.Add(new OwnedItemsView.Entry(_loadout.GetPassive(i).GetIcon(), PASSIVE_OWNED_COUNT));

            _upgradeEntries.Clear();
            for (int i = 0; i < _loadout.UpgradeKindCount; ++i)
                _upgradeEntries.Add(new OwnedItemsView.Entry(_loadout.GetUpgrade(i).GetIcon(), _loadout.GetUpgradeCount(i)));

            _ownedItemsPresenter?.Invoke(_passiveEntries, _upgradeEntries);
        }

        public void ShowGameResult(GameResultView.EResult result, RunSummary summary)
        {
            UIManager.Show(new GameResultView.Args(result, BuildResultStats(summary)));
        }

        public bool TryShowRewardUnlock(ClearRewardData reward)
        {
            if (null == reward)
                return false;

            int entryCount = reward.FlipperCount + reward.ItemCount;
            if (entryCount <= 0)
                return false;

            RewardUnlockView.Entry[] entries = new RewardUnlockView.Entry[entryCount];
            for (int i = 0; i < reward.FlipperCount; ++i)
                entries[i] = BuildFlipperRewardEntry(reward.GetFlipper(i));

            for (int i = 0; i < reward.ItemCount; ++i)
                entries[reward.FlipperCount + i] = BuildItemRewardEntry(reward.GetItem(i));

            UIManager.Show(new RewardUnlockView.Args(entries));
            return true;
        }

        ResultStatsView.Args BuildResultStats(RunSummary summary)
        {
            ResultStatsView.ItemEntry[] passives = new ResultStatsView.ItemEntry[_loadout.PassiveCount];
            for (int i = 0; i < passives.Length; ++i)
                passives[i] = BuildResultItemEntry(_loadout.GetPassive(i), PASSIVE_OWNED_COUNT);

            ResultStatsView.ItemEntry[] upgrades = new ResultStatsView.ItemEntry[_loadout.UpgradeKindCount];
            for (int i = 0; i < upgrades.Length; ++i)
                upgrades[i] = BuildResultItemEntry(_loadout.GetUpgrade(i), _loadout.GetUpgradeCount(i));

            return new ResultStatsView.Args(
                StageNumber: summary.StageNumber,
                Score: summary.Score,
                Money: summary.Money,
                UsedBallCount: summary.UsedBallCount,
                Passives: passives,
                Upgrades: upgrades);
        }

        static ResultStatsView.ItemEntry BuildResultItemEntry(ShopItemData item, int count)
        {
            if (null == item)
                return null;

            return new ResultStatsView.ItemEntry(item.GetIcon(), count, item.GetDisplayName(), item.GetDescription());
        }

        static RewardUnlockView.Entry BuildFlipperRewardEntry(FlipperData flipper)
        {
            if (null == flipper)
                return null;

            return new RewardUnlockView.Entry(
                Kind: RewardUnlockView.ERewardKind.Flipper,
                Icon: flipper.GetIcon(),
                DisplayName: flipper.GetDisplayName(),
                Description: flipper.GetAbilityDescription());
        }

        static RewardUnlockView.Entry BuildItemRewardEntry(ShopItemData item)
        {
            if (null == item)
                return null;

            bool isPassive = item.Kind == ShopItemData.EItemKind.Passive;
            return new RewardUnlockView.Entry(
                Kind: isPassive ? RewardUnlockView.ERewardKind.Passive : RewardUnlockView.ERewardKind.Upgrade,
                Icon: item.GetIcon(),
                DisplayName: item.GetDisplayName(),
                Description: item.GetDescription());
        }
    }
}
