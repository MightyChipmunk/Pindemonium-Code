using System;
using Pinball.Save;
using UnityEngine;

namespace Pinball.Ingame.Run
{
    public static class IngameRunSetupResolver
    {
        public record ResolveParam(
            IngamePlayData SavedData,
            int RequestedFlipperIndex,
            FlipperCatalogData FlipperCatalog,
            Func<FlipperData, bool> IsFlipperUnlockedGetter,
            int BasePassiveSlotCount,
            int BaseStageClearMoney);

        public record RunStart(
            FlipperData Flipper,
            FlipperData.RunSetup Setup,
            IngamePlayData SavedData,
            bool IsNewRun,
            int StageNumber,
            int Money,
            int Score,
            int[] GimmickIndices,
            bool IsShopPending,
            int PassiveSlotCount,
            int StageClearMoney);

        const int FIRST_STAGE_NUMBER = 1;
        const int MIN_PASSIVE_SLOT_COUNT = 1;

        public static RunStart Resolve(ResolveParam param)
        {
            IngamePlayData saved = param.SavedData;
            bool isNewRun = null == saved;
            int flipperIndex = isNewRun ? param.RequestedFlipperIndex : ResolveSavedFlipperIndex(param.FlipperCatalog, saved.FlipperId);
            FlipperData flipper = ResolveUnlockedFlipper(param.FlipperCatalog, flipperIndex, param.IsFlipperUnlockedGetter);
            FlipperData.RunSetup setup = null == flipper ? FlipperData.DEFAULT_RUN_SETUP : flipper.GetRunSetup();

            return new RunStart(
                Flipper: flipper,
                Setup: setup,
                SavedData: saved,
                IsNewRun: isNewRun,
                StageNumber: isNewRun ? FIRST_STAGE_NUMBER : saved.StageNumber,
                Money: isNewRun ? Mathf.Max(0, setup.StartMoneyAdd) : saved.Money,
                Score: isNewRun ? 0 : saved.TotalScore,
                GimmickIndices: isNewRun ? null : saved.GetGimmickIndices(),
                IsShopPending: !isNewRun && saved.IsShopPending,
                PassiveSlotCount: Mathf.Max(MIN_PASSIVE_SLOT_COUNT, param.BasePassiveSlotCount + setup.PassiveSlotAdd),
                StageClearMoney: Mathf.Max(0, param.BaseStageClearMoney + setup.StageClearMoneyAdd));
        }

        static int ResolveSavedFlipperIndex(FlipperCatalogData catalog, string flipperId)
        {
            if (null == catalog)
                return FlipperCatalogData.DEFAULT_INDEX;

            return catalog.IndexOfIdOrDefault(flipperId);
        }

        static FlipperData ResolveUnlockedFlipper(FlipperCatalogData catalog, int flipperIndex, Func<FlipperData, bool> isUnlocked)
        {
            if (null == catalog)
                return null;

            FlipperData flipper = catalog.GetFlipper(flipperIndex);
            if (null != flipper && (null == isUnlocked || isUnlocked(flipper)))
                return flipper;

            return catalog.GetFlipper(FlipperCatalogData.DEFAULT_INDEX);
        }
    }
}
