using Pinball.Localization;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageInfoView : UIViewBase
    {
        public record Args(int StageNumber, int TargetScore, int StageScore, int RemainingSeconds) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.StageInfo;
        }

        public const int NO_TIME_LIMIT = -1;

        const string STAGE_FORMAT = "STAGE {0}";
        const string STAGE_KEY = "ui.stage_info.stage";
        const string SCORE_GOAL_FORMAT = "SCORE {0} / {1}";
        const string SCORE_GOAL_KEY = "ui.stage_info.score_goal";
        const string TIME_FORMAT = "TIME {0:00}:{1:00}";
        const string TIME_KEY = "ui.stage_info.time";
        const int SECONDS_PER_MINUTE = 60;

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _goalLabel = null;
        [SerializeField] GameObject _timeRoot = null;
        [SerializeField] TMP_Text _timeLabel = null;

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber);
            _goalLabel.text = LocalizationTable.Format(SCORE_GOAL_KEY, SCORE_GOAL_FORMAT, viewArgs.StageScore, viewArgs.TargetScore);
            BindTime(viewArgs.RemainingSeconds);
        }

        void BindTime(int remainingSeconds)
        {
            if (null == _timeLabel)
                return;

            bool hasTimeLimit = remainingSeconds != NO_TIME_LIMIT;
            _timeRoot.SetActive(hasTimeLimit);
            if (!hasTimeLimit)
                return;

            int clampedSeconds = Mathf.Max(0, remainingSeconds);
            _timeLabel.text = LocalizationTable.Format(TIME_KEY, TIME_FORMAT, clampedSeconds / SECONDS_PER_MINUTE, clampedSeconds % SECONDS_PER_MINUTE);
        }
    }
}
