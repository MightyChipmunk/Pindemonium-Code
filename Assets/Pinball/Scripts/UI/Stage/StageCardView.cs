using System;
using Pinball.Ingame.Stage;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class StageCardView : MonoBehaviour
    {
        public enum EState
        {
            Cleared,
            Current,
            Upcoming,
        }

        public record BindParam(
            int StageNumber,
            EState State,
            bool IsFinal,
            int TargetScore,
            float TargetScoreMultiplier,
            int RewardMoney,
            StageGimmickData[] Gimmicks,
            int RerollPrice,
            bool CanReroll);

        const string STAGE_FORMAT = "STAGE {0}";
        const string STAGE_KEY = "ui.stage_card.stage";
        const string TARGET_SCORE_FORMAT = "N0";
        const string MULTIPLIER_FORMAT = "x{0:0.#}";
        const string REWARD_FORMAT = "${0}";
        const string REROLL_FORMAT = "REROLL ${0}";
        const string REROLL_KEY = "ui.stage_card.reroll";
        const string TAG_NOW = "NOW";
        const string TAG_NOW_KEY = "ui.stage_card.tag_now";
        const string TAG_UP_NEXT = "UP NEXT";
        const string TAG_UP_NEXT_KEY = "ui.stage_card.tag_up_next";
        const string TAG_FINAL = "FINAL";
        const string TAG_FINAL_KEY = "ui.stage_card.tag_final";
        const string TAG_CLEARED = "CLEAR";
        const string TAG_CLEARED_KEY = "ui.stage_card.tag_cleared";
        const float CURRENT_ALPHA = 1f;
        const float UPCOMING_ALPHA = 0.7f;
        const float CLEARED_ALPHA = 0.3f;
        const float DEFAULT_TARGET_SCORE_MULTIPLIER = 1f;
        const float CURRENT_RAISE_OFFSET = 40f;

        [SerializeField] RectTransform _content = null;
        [SerializeField] CanvasGroup _canvasGroup = null;
        [SerializeField] Image _bandImage = null;
        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _tagLabel = null;
        [SerializeField] TMP_Text _targetScoreLabel = null;
        [SerializeField] TMP_Text _multiplierLabel = null;
        [SerializeField] TMP_Text _rewardLabel = null;
        [SerializeField] GameObject _gimmickPanel = null;
        [SerializeField] TMP_Text[] _gimmickLabels = null;
        [SerializeField] GameObject _emptyGimmickRoot = null;
        [SerializeField] Button _rerollButton = null;
        [SerializeField] TMP_Text _rerollLabel = null;
        [SerializeField] Button _startButton = null;
        [SerializeField] GameObject _upNextPlate = null;
        [SerializeField] Color _finalBandColor = Color.white;

        Action<int> _startCallback = null;
        Action<int> _rerollCallback = null;
        Vector2 _contentBasePosition = Vector2.zero;
        Color _bandBaseColor = Color.white;
        int _slotIndex = 0;

        public void Init(int slotIndex, Action<int> startCallback, Action<int> rerollCallback)
        {
            _slotIndex = slotIndex;
            _startCallback = startCallback;
            _rerollCallback = rerollCallback;
            _contentBasePosition = _content.anchoredPosition;
            _bandBaseColor = _bandImage.color;
            _startButton.onClick.AddListener(OnStartClicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
        }

        public void Release()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startCallback = null;
            _rerollCallback = null;
        }

        public void Bind(BindParam param)
        {
            bool isCurrent = param.State == EState.Current;
            gameObject.SetActive(true);
            _titleLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, param.StageNumber);
            _tagLabel.text = GetTagText(param.State, param.IsFinal);
            _bandImage.color = param.IsFinal ? _finalBandColor : _bandBaseColor;
            _content.anchoredPosition = _contentBasePosition + (isCurrent ? Vector2.up * CURRENT_RAISE_OFFSET : Vector2.zero);
            _canvasGroup.alpha = GetAlpha(param.State);
            _canvasGroup.interactable = isCurrent;
            _canvasGroup.blocksRaycasts = isCurrent;
            _targetScoreLabel.text = param.TargetScore.ToString(TARGET_SCORE_FORMAT);
            BindMultiplier(param.TargetScoreMultiplier);
            _rewardLabel.text = string.Format(REWARD_FORMAT, param.RewardMoney);
            BindGimmicks(param.Gimmicks);
            BindActions(param);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void BindMultiplier(float multiplier)
        {
            bool hasMultiplier = !Mathf.Approximately(multiplier, DEFAULT_TARGET_SCORE_MULTIPLIER);
            _multiplierLabel.gameObject.SetActive(hasMultiplier);
            if (!hasMultiplier)
                return;

            _multiplierLabel.text = string.Format(MULTIPLIER_FORMAT, multiplier);
        }

        void BindGimmicks(StageGimmickData[] gimmicks)
        {
            int gimmickCount = null == gimmicks ? 0 : gimmicks.Length;
            _gimmickPanel.SetActive(gimmickCount > 0);
            _emptyGimmickRoot.SetActive(gimmickCount <= 0);

            for (int i = 0; i < _gimmickLabels.Length; ++i)
            {
                bool isUsed = i < gimmickCount;
                _gimmickLabels[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    _gimmickLabels[i].text = StageGimmickLabelFormatter.Format(gimmicks[i]);
            }
        }

        void BindActions(BindParam param)
        {
            bool isCurrent = param.State == EState.Current;
            bool hasGimmick = null != param.Gimmicks && param.Gimmicks.Length > 0;
            _startButton.gameObject.SetActive(isCurrent);
            _rerollButton.gameObject.SetActive(isCurrent && hasGimmick);
            _rerollButton.interactable = param.CanReroll;
            _rerollLabel.text = LocalizationTable.Format(REROLL_KEY, REROLL_FORMAT, param.RerollPrice);
            _upNextPlate.SetActive(param.State == EState.Upcoming);
        }

        static float GetAlpha(EState state)
        {
            if (state == EState.Current)
                return CURRENT_ALPHA;

            return state == EState.Cleared ? CLEARED_ALPHA : UPCOMING_ALPHA;
        }

        static string GetTagText(EState state, bool isFinal)
        {
            if (state == EState.Current)
                return LocalizationTable.Get(TAG_NOW_KEY, TAG_NOW);

            if (state == EState.Cleared)
                return LocalizationTable.Get(TAG_CLEARED_KEY, TAG_CLEARED);

            return isFinal ? LocalizationTable.Get(TAG_FINAL_KEY, TAG_FINAL) : LocalizationTable.Get(TAG_UP_NEXT_KEY, TAG_UP_NEXT);
        }

        void OnStartClicked()
        {
            _startCallback?.Invoke(_slotIndex);
        }

        void OnRerollClicked()
        {
            _rerollCallback?.Invoke(_slotIndex);
        }
    }
}
