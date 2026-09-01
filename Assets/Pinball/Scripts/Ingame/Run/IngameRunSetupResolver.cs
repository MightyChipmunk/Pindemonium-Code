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
            int BaseCommonPassiveSlotCount);

        public record RunStart(
            FlipperData Flipper,
            FlipperData.RunSetup Setup,
            IngamePlayData SavedData,
            bool IsNewRun,
            int StageNumber,
            bool IsEndless,
            int Money,
            long Score,
            int[] GimmickIndices,
            bool IsShopPending,
            bool IsRoundRewardPending,
            int BallPassiveSlotCount,
            int MapPassiveSlotCount,
            int CommonPassiveSlotCount,
            string[] ShopOfferIds,
            IngamePlayData.CardPackEntry[] ShopPacks,
            IngamePlayData.CardPackEntry ShopOpening,
            int ShopRerollCount,
            int SlingshotHitCount,
            long[] GimmickScores);

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
                IsEndless: !isNewRun && saved.IsEndless,
                Money: isNewRun ? Mathf.Max(0, setup.StartMoneyAdd) : saved.Money,
                Score: isNewRun ? 0 : saved.TotalScore,
                GimmickIndices: isNewRun ? null : saved.GetGimmickIndices(),
                IsShopPending: !isNewRun && saved.IsShopPending,
                IsRoundRewardPending: !isNewRun && saved.IsRoundRewardPending,
                BallPassiveSlotCount: param.BaseBallPassiveSlotCount,
                MapPassiveSlotCount: param.BaseMapPassiveSlotCount,
                CommonPassiveSlotCount: Mathf.Max(MIN_COMMON_PASSIVE_SLOT_COUNT, param.BaseCommonPassiveSlotCount + setup.PassiveSlotAdd),
                ShopOfferIds: isNewRun || !saved.IsShopPending ? null : saved.GetShopOfferIds(),
                ShopPacks: isNewRun || !saved.IsShopPending ? null : saved.GetShopPacks(),
                ShopOpening: isNewRun || !saved.IsShopPending ? default : saved.GetShopOpening(),
                ShopRerollCount: isNewRun || !saved.IsShopPending ? 0 : saved.ShopRerollCount,
                SlingshotHitCount: isNewRun ? 0 : saved.SlingshotHitCount,
                GimmickScores: isNewRun ? null : saved.GetGimmickScores());
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
