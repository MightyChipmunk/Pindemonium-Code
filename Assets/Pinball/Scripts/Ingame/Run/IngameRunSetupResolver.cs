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
            int BaseBallPassiveSlotCount,
            int BaseMapPassiveSlotCount,
            int BaseCommonPassiveSlotCount,
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
            bool IsRoundRewardPending,
            int BallPassiveSlotCount,
            int MapPassiveSlotCount,
            int CommonPassiveSlotCount,
            int StageClearMoney,
            int SlingshotHitCount);

        const int FIRST_STAGE_NUMBER = 1;
        const int MIN_COMMON_PASSIVE_SLOT_COUNT = 0;

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
                IsRoundRewardPending: !isNewRun && saved.IsRoundRewardPending,
                BallPassiveSlotCount: param.BaseBallPassiveSlotCount,
                MapPassiveSlotCount: param.BaseMapPassiveSlotCount,
                CommonPassiveSlotCount: Mathf.Max(MIN_COMMON_PASSIVE_SLOT_COUNT, param.BaseCommonPassiveSlotCount + setup.PassiveSlotAdd),
                StageClearMoney: Mathf.Max(0, param.BaseStageClearMoney + setup.StageClearMoneyAdd),
                SlingshotHitCount: isNewRun ? 0 : saved.SlingshotHitCount);
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
