using System;

namespace Pinball.Save
{
    [Serializable]
    public class IngamePlayData : SaveDataBase
    {
        [Serializable]
        public class UpgradeEntry
        {
            public string Id = string.Empty;
            public int Count = 0;
        }

        public int StageNumber = 1;
        public bool IsShopPending = false;
        public int Money = 0;
        public int TotalScore = 0;
        public int[] GimmickIndices = null;
        public string FlipperId = string.Empty;
        public string[] PassiveIds = null;
        public UpgradeEntry[] Upgrades = null;
    }
}
