using GameCore.UI;
using UnityEngine;

namespace Pinball.UI
{
    [CreateAssetMenu(fileName = "UIViewCatalogData", menuName = "Pinball/UI/UIViewCatalogData")]
    public class UIViewCatalogData : UIViewCatalogBase<UIViewCatalogData.EViewType>
    {
        public enum EViewType
        {
            Settings = 0,
            StageClear = 2,
            Shop = 3,
            PausePopup = 4,
            GameResult = 6,
            ResultStats = 7,
            StageSelect = 8,
            FlipperSelect = 9,
            ContinueRun = 10,
            RewardUnlock = 11,
            CardPackPopup = 12,
            RoundClear = 13,
            GimmickScore = 14,
            VoucherListPopup = 15,
        }
    }
}
