using Pinball.Localization;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageInfoView : MonoBehaviour
    {
        public const int NO_TIME_LIMIT = -1;

        const string STAGE_FORMAT = "STAGE {0}";
        const string STAGE_KEY = "ui.stage_info.stage";
        const string SCORE_GOAL_FORMAT = "TARGET SCORE {0}";
        const string SCORE_GOAL_KEY = "ui.stage_info.score_goal";
        const string TIME_FORMAT = "TIME {0:00}:{1:00}";
        const string TIME_KEY = "ui.stage_info.time";
        const int SECONDS_PER_MINUTE = 60;

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _goalLabel = null;
        [SerializeField] GameObject _timeRoot = null;
        [SerializeField] TMP_Text _timeLabel = null;

        public void SetInfo(int stageNumber, int targetScore, int remainingSeconds)
        {
            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, stageNumber);
            _goalLabel.text = LocalizationTable.Format(SCORE_GOAL_KEY, SCORE_GOAL_FORMAT, targetScore);
            BindTime(remainingSeconds);
        }

        void BindTime(int remainingSeconds)
        {
            bool hasTimeLimit = remainingSeconds != NO_TIME_LIMIT;
            _timeRoot.SetActive(hasTimeLimit);
            if (!hasTimeLimit)
                return;

            int clampedSeconds = Mathf.Max(0, remainingSeconds);
            _timeLabel.text = LocalizationTable.Format(TIME_KEY, TIME_FORMAT, clampedSeconds / SECONDS_PER_MINUTE, clampedSeconds % SECONDS_PER_MINUTE);
        }
    }
}
