using System;
using Pinball.Ingame.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class StageCardView : MonoBehaviour
    {
        public record BindParam(
            int StageNumber,
            bool IsCurrent,
            bool IsFinal,
            int TargetScore,
            float TargetScoreMultiplier,
            int RewardMoney,
            string[] GimmickLabels,
            int RerollPrice,
            bool CanReroll);

        const string STAGE_FORMAT = "STAGE {0}";
        const string TARGET_SCORE_FORMAT = "N0";
        const string MULTIPLIER_FORMAT = "x{0:0.#}";
        const string REWARD_FORMAT = "${0}";
        const string REROLL_FORMAT = "REROLL ${0}";
        const string TAG_NOW = "NOW";
        const string TAG_UP_NEXT = "UP NEXT";
        const string TAG_FINAL = "FINAL";
        const float UPCOMING_ALPHA = 0.7f;
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
            gameObject.SetActive(true);
            _titleLabel.text = string.Format(STAGE_FORMAT, param.StageNumber);
            _tagLabel.text = GetTagText(param.IsCurrent, param.IsFinal);
            _bandImage.color = param.IsFinal ? _finalBandColor : _bandBaseColor;
            _content.anchoredPosition = _contentBasePosition + (param.IsCurrent ? Vector2.up * CURRENT_RAISE_OFFSET : Vector2.zero);
            _canvasGroup.alpha = param.IsCurrent ? 1f : UPCOMING_ALPHA;
            _targetScoreLabel.text = param.TargetScore.ToString(TARGET_SCORE_FORMAT);
            BindMultiplier(param.TargetScoreMultiplier);
            _rewardLabel.text = string.Format(REWARD_FORMAT, param.RewardMoney);
            BindGimmicks(param.GimmickLabels);
            BindActions(param);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void BindMultiplier(float multiplier)
        {
            bool hasMultiplier = !Mathf.Approximately(multiplier, StageGimmickResolver.DEFAULT_TARGET_SCORE_MULTIPLIER);
            _multiplierLabel.gameObject.SetActive(hasMultiplier);
            if (!hasMultiplier)
                return;

            _multiplierLabel.text = string.Format(MULTIPLIER_FORMAT, multiplier);
        }

        void BindGimmicks(string[] labels)
        {
            int labelCount = null == labels ? 0 : labels.Length;
            _gimmickPanel.SetActive(labelCount > 0);
            _emptyGimmickRoot.SetActive(labelCount <= 0);

            for (int i = 0; i < _gimmickLabels.Length; ++i)
            {
                bool isUsed = i < labelCount;
                _gimmickLabels[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    _gimmickLabels[i].text = labels[i];
            }
        }

        void BindActions(BindParam param)
        {
            bool hasGimmick = null != param.GimmickLabels && param.GimmickLabels.Length > 0;
            _startButton.gameObject.SetActive(param.IsCurrent);
            _rerollButton.gameObject.SetActive(param.IsCurrent && hasGimmick);
            _rerollButton.interactable = param.CanReroll;
            _rerollLabel.text = string.Format(REROLL_FORMAT, param.RerollPrice);
            _upNextPlate.SetActive(!param.IsCurrent);
        }

        string GetTagText(bool isCurrent, bool isFinal)
        {
            if (isCurrent)
                return TAG_NOW;

            return isFinal ? TAG_FINAL : TAG_UP_NEXT;
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
