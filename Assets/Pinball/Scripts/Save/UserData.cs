using System;
using System.Collections.Generic;

namespace Pinball.Save
{
    [Serializable]
    public class UserData : SaveDataBase
    {
        public int BestStageNumber = 0;
        public int ClearCount = 0;
        public List<string> UnlockedFlipperIds = new List<string>();
        public List<string> UnlockedItemIds = new List<string>();
    }
}
