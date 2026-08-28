using System.Globalization;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class StageInfoView : MonoBehaviour
    {
        public const int NO_TIME_LIMIT = -1;

        const string STAGE_FORMAT = "STAGE {0}";
        const string STAGE_KEY = "ui.common.stage_number";
        const string ENDLESS_STAGE_FORMAT = "ENDLESS {0}";
        const string ENDLESS_STAGE_KEY = "ui.common.endless_number";
        const string SCORE_GOAL_FORMAT = "TARGET SCORE {0}";
        const string SCORE_GOAL_KEY = "ui.stage_info.score_goal";
        const string NUMBER_FORMAT = "N0";
        const string PROGRESS_FORMAT = "{0:N0} / {1:N0}";
        const string TIME_FORMAT = "{0:00}:{1:00}";
        const int SECONDS_PER_MINUTE = 60;
        const float MIN_FILL_WIDTH = 16f;

        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _goalLabel = null;
        [SerializeField] TMP_Text _progressLabel = null;
        [SerializeField] RectTransform _progressFill = null;
        [SerializeField] float _progressFillMaxWidth = 370f;
        [SerializeField] GameObject _timeRoot = null;
        [SerializeField] TMP_Text _timeLabel = null;
        [SerializeField] LayoutElement _layoutElement = null;
        [SerializeField] float _heightWithTime = 370f;
        [SerializeField] float _heightWithoutTime = 280f;

        int _targetScore = 0;
        int _score = 0;

        public void SetInfo(int stageNumber, bool isEndless, int targetScore, int remainingSeconds)
        {
            _targetScore = targetScore;
            _stageLabel.text = isEndless
                ? LocalizationTable.Format(ENDLESS_STAGE_KEY, ENDLESS_STAGE_FORMAT, stageNumber)
                : LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, stageNumber);
            _goalLabel.text = LocalizationTable.Format(SCORE_GOAL_KEY, SCORE_GOAL_FORMAT, targetScore.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture));
            BindProgress();
            BindTime(remainingSeconds);
        }

        public void SetScore(int score)
        {
            _score = score;
            BindProgress();
        }

        void BindProgress()
        {
            _progressLabel.text = string.Format(CultureInfo.InvariantCulture, PROGRESS_FORMAT, _score, _targetScore);

            float ratio = _targetScore <= 0 ? 0f : Mathf.Clamp01(_score / (float)_targetScore);
            bool hasFill = ratio > 0f;
            _progressFill.gameObject.SetActive(hasFill);
            if (!hasFill)
                return;

            float width = Mathf.Max(MIN_FILL_WIDTH, _progressFillMaxWidth * ratio);
            _progressFill.sizeDelta = new Vector2(width, _progressFill.sizeDelta.y);
        }

        void BindTime(int remainingSeconds)
        {
            bool hasTimeLimit = remainingSeconds != NO_TIME_LIMIT;
            _timeRoot.SetActive(hasTimeLimit);
            _layoutElement.preferredHeight = hasTimeLimit ? _heightWithTime : _heightWithoutTime;
            if (!hasTimeLimit)
                return;

            int clampedSeconds = Mathf.Max(0, remainingSeconds);
            _timeLabel.text = string.Format(CultureInfo.InvariantCulture, TIME_FORMAT, clampedSeconds / SECONDS_PER_MINUTE, clampedSeconds % SECONDS_PER_MINUTE);
        }
    }
}
