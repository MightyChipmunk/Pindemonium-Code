using Pinball.Save;

namespace Pinball.Ingame.Save
{
    public class IngameSaveHandler
    {
        public record SaveParam(
            int StageNumber,
            bool IsEndless,
            bool IsShopPending,
            bool IsRoundRewardPending,
            int Money,
            int TotalScore,
            int[] GimmickIndices,
            string FlipperId,
            string[] PassiveIds,
            IngamePlayData.UpgradeEntry[] Upgrades,
            string[] VoucherIds,
            string[] VoucherOfferedIds,
            string VoucherOfferId,
            int VoucherOfferRound,
            int VoucherPurchasedRound,
            int SlingshotHitCount,
            int[] GimmickScores);

        public bool TryLoad(bool isContinueRequested, out IngamePlayData savedData)
        {
            savedData = isContinueRequested ? SaveManager.GetIngameData() : null;
            return null != savedData;
        }

        public void Save(SaveParam param)
        {
            SaveManager.SaveIngame(new IngamePlayData(
                param.StageNumber,
                param.IsEndless,
                param.IsShopPending,
                param.IsRoundRewardPending,
                param.Money,
                param.TotalScore,
                param.GimmickIndices,
                param.FlipperId,
                param.PassiveIds,
                param.Upgrades,
                param.VoucherIds,
                param.VoucherOfferedIds,
                param.VoucherOfferId,
                param.VoucherOfferRound,
                param.VoucherPurchasedRound,
                param.SlingshotHitCount,
                param.GimmickScores));
        }

        public void UpdateBestStage(string flipperId, int stageNumber)
        {
            SaveManager.UpdateBestStage(flipperId, stageNumber);
        }

        public void UpdateBestClear(string flipperId)
        {
            SaveManager.UpdateBestClear(flipperId);
        }

        public void UpdateBestEndlessStage(string flipperId, int endlessStageNumber)
        {
            SaveManager.UpdateBestEndlessStage(flipperId, endlessStageNumber);
        }

        public void ClearPlayData()
        {
            SaveManager.ClearIngame();
        }
    }
}
