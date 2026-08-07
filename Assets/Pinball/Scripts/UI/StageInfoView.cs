using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageInfoView : UIViewBase
    {
        public record Args(int StageNumber, int TargetScore, int StageScore) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageInfo;
        }

        const string STAGE_FORMAT = "STAGE {0}";
        const string SCORE_GOAL_FORMAT = "SCORE {0} / {1}";

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _goalLabel = null;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _stageLabel.text = string.Format(STAGE_FORMAT, viewArgs.StageNumber);
            _goalLabel.text = string.Format(SCORE_GOAL_FORMAT, viewArgs.StageScore, viewArgs.TargetScore);
        }
    }
}
