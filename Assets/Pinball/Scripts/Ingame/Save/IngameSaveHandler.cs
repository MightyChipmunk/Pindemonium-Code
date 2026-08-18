using Pinball.Core;
using Pinball.Save;

namespace Pinball.Ingame
{
    public class IngameSaveHandler
    {
        public record SaveParam(
            int StageNumber,
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
            IngamePlayData data = new IngamePlayData();
            data.StageNumber = param.StageNumber;
            data.Money = param.Money;
            data.TotalScore = param.TotalScore;
            data.GimmickIndices = param.GimmickIndices;
            data.FlipperId = param.FlipperId;
            data.PassiveIds = param.PassiveIds;
            data.Upgrades = param.Upgrades;
            SaveManager.SaveIngame(data);
        }

        public void UpdateBestStage(int stageNumber)
        {
            UserData userData = SaveManager.GetUserData();
            if (null == userData)
                return;

            if (stageNumber <= userData.BestStageNumber)
                return;

            userData.BestStageNumber = stageNumber;
            SaveManager.SaveUser();
        }

        public void ClearPlayData()
        {
            SaveManager.ClearIngame();
        }
    }
}
