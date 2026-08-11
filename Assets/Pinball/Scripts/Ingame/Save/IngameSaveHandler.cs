using Pinball.Core;
using Pinball.Save;

namespace Pinball.Ingame
{
    public class IngameSaveHandler
    {
        public bool TryLoad(out int stageNumber, out int money, out int totalScore)
        {
            stageNumber = 0;
            money = 0;
            totalScore = 0;

            IngamePlayData savedData = SceneFlow.IsContinueRequested ? SaveManager.GetIngameData() : null;
            if (null == savedData)
                return false;

            stageNumber = savedData.StageNumber;
            money = savedData.Money;
            totalScore = savedData.TotalScore;
            return true;
        }

        public void Save(int stageNumber, int money, int totalScore)
        {
            IngamePlayData data = new IngamePlayData();
            data.StageNumber = stageNumber;
            data.Money = money;
            data.TotalScore = totalScore;
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

        public void ClearOnGameOver()
        {
            SaveManager.ClearIngame();
        }
    }
}
