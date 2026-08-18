using Pinball.Core;
using Pinball.Save;

namespace Pinball.Ingame
{
    public class IngameSaveHandler
    {
        public record SaveParam(
            int StageNumber,
            bool IsShopPending,
            int Money,
            int TotalScore,
            int[] GimmickIndices,
            string FlipperId,
            string[] PassiveIds,
            IngamePlayData.UpgradeEntry[] Upgrades);

        public bool TryLoad(out IngamePlayData savedData)
        {
            savedData = SceneFlow.IsContinueRequested ? SaveManager.GetIngameData() : null;
            return null != savedData;
        }

        public void Save(SaveParam param)
        {
            SaveManager.SaveIngame(new IngamePlayData(
                param.StageNumber,
                param.IsShopPending,
                param.Money,
                param.TotalScore,
                param.GimmickIndices,
                param.FlipperId,
                param.PassiveIds,
                param.Upgrades));
        }

        public void UpdateBestStage(int stageNumber)
        {
            SaveManager.UpdateBestStage(stageNumber);
        }

        public void ClearPlayData()
        {
            SaveManager.ClearIngame();
        }
    }
}
