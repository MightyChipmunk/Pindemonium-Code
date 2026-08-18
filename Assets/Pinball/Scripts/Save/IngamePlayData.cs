using System;

namespace Pinball.Save
{
    [Serializable]
    public class IngamePlayData
    {
        public int StageNumber = 1;
        public int Money = 0;
        public int TotalScore = 0;
        public int[] GimmickIndices = null;
        public string FlipperId = string.Empty;
    }
}
