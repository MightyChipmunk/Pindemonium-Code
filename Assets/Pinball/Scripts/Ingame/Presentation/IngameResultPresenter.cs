using System;
using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stage;
using Pinball.Ingame.UI;
using Pinball.UI;

namespace Pinball.Ingame.Presentation
{
    public class IngameResultPresenter
    {
        public record RunSummary(int StageNumber, int Score, int Money, int UsedBallCount, int[] GimmickScores);

        const int PASSIVE_OWNED_COUNT = 1;

        PlayerLoadout _loadout = null;
        Action<OwnedItemsView.Args> _ownedItemsPresenter = null;
        List<OwnedItemsView.Entry> _passiveEntries = null;
        List<OwnedItemsView.Entry> _upgradeEntries = null;

        public IngameResultPresenter(PlayerLoadout loadout, Action<OwnedItemsView.Args> ownedItemsPresenter)
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
                _passiveEntries.Add(BuildOwnedEntry(_loadout.GetPassive(i), PASSIVE_OWNED_COUNT));

            _upgradeEntries.Clear();
            for (int i = 0; i < _loadout.UpgradeKindCount; ++i)
                _upgradeEntries.Add(BuildOwnedEntry(_loadout.GetUpgrade(i), _loadout.GetUpgradeCount(i)));

            _ownedItemsPresenter?.Invoke(new OwnedItemsView.Args(
                Passives: _passiveEntries,
                Upgrades: _upgradeEntries,
                BallSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Ball),
                MapSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Map),
                CommonSlotCount: _loadout.GetPassiveSlotCount(ShopItemData.EPassiveCategory.Common)));
        }

        public void ShowGameResult(GameResultView.EResult result, RunSummary summary)
        {
            UIManager.Show(new GameResultView.Args(result, BuildResultStats(summary)));
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
                Upgrades: upgrades,
                GimmickScores: BuildGimmickScores(summary.GimmickScores));
        }

        static GimmickScoreView.Args BuildGimmickScores(int[] scores)
        {
            Array gimmickTypes = Enum.GetValues(typeof(MapInteractable.EGimmickType));
            GimmickScoreView.Entry[] entries = new GimmickScoreView.Entry[gimmickTypes.Length];
            for (int i = 0; i < entries.Length; ++i)
            {
                MapInteractable.EGimmickType gimmickType = (MapInteractable.EGimmickType)gimmickTypes.GetValue(i);
                entries[i] = new GimmickScoreView.Entry(StageGimmickLabelFormatter.GetGimmickNameRef(gimmickType), GetGimmickScore(scores, gimmickType));
            }

            SortByScoreDescending(entries);
            return new GimmickScoreView.Args(entries);
        }

        static int GetGimmickScore(int[] scores, MapInteractable.EGimmickType gimmickType)
        {
            int index = (int)gimmickType;
            if (null == scores || index < 0 || index >= scores.Length)
                return 0;

            return scores[index];
        }

        static void SortByScoreDescending(GimmickScoreView.Entry[] entries)
        {
            for (int i = 1; i < entries.Length; ++i)
            {
                GimmickScoreView.Entry current = entries[i];
                int slot = i - 1;
                while (slot >= 0 && entries[slot].Score < current.Score)
                {
                    entries[slot + 1] = entries[slot];
                    --slot;
                }

                entries[slot + 1] = current;
            }
        }

        static OwnedItemsView.Entry BuildOwnedEntry(ShopItemData item, int count)
        {
            if (null == item)
                return null;

            return new OwnedItemsView.Entry(item.GetIcon(), count, item.GetNameRef(), item.GetDescriptionRef(), item.PassiveCategory);
        }

        static ResultStatsView.ItemEntry BuildResultItemEntry(ShopItemData item, int count)
        {
            if (null == item)
                return null;

            return new ResultStatsView.ItemEntry(item.GetIcon(), count, item.GetNameRef(), item.GetDescriptionRef(), item.PassiveCategory);
        }
    }
}
