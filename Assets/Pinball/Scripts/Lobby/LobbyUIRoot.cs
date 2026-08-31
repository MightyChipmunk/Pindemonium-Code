using System;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Lobby
{
    public class LobbyUIRoot : MonoBehaviour
    {
        const string STAGE_CHIP_FORMAT = "STAGE {0}";
        const string STAGE_CHIP_KEY = "ui.common.stage_number";
        const string ENDLESS_CHIP_FORMAT = "ENDLESS {0}";
        const string ENDLESS_CHIP_KEY = "ui.common.endless_number";
        const float CONTINUE_FULL_ALPHA = 1f;
        const float CONTINUE_DIMMED_ALPHA = 0.45f;

        static readonly Vector2 CONTINUE_LABEL_POSITION_WITH_CHIP = new Vector2(-80f, 0f);
        static readonly Vector2 CONTINUE_LABEL_SIZE_WITH_CHIP = new Vector2(-264f, -16f);
        static readonly Vector2 CONTINUE_LABEL_POSITION_CENTERED = new Vector2(0f, 0f);
        static readonly Vector2 CONTINUE_LABEL_SIZE_CENTERED = new Vector2(-24f, -16f);

        [SerializeField] Button _continueButton = null;
        [SerializeField] CanvasGroup _continueGroup = null;
        [SerializeField] Button _newGameButton = null;
        [SerializeField] Button _settingsButton = null;
        [SerializeField] Button _quitButton = null;
        [SerializeField] GameObject _continueStageChip = null;
        [SerializeField] TMP_Text _continueStageLabel = null;
        [SerializeField] RectTransform _continueLabelRect = null;

        Action _onContinueClicked = null;
        Action _onNewGameClicked = null;
        Action _onSettingsClicked = null;
        Action _onQuitClicked = null;

        public void Init(Action onContinueClicked, Action onNewGameClicked, Action onSettingsClicked, Action onQuitClicked)
        {
            _onContinueClicked = onContinueClicked;
            _onNewGameClicked = onNewGameClicked;
            _onSettingsClicked = onSettingsClicked;
            _onQuitClicked = onQuitClicked;

            _continueButton.onClick.AddListener(OnContinueClicked);
            _newGameButton.onClick.AddListener(OnNewGameClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void SetContinueInteractable(bool isInteractable)
        {
            _continueButton.interactable = isInteractable;
            _continueGroup.alpha = isInteractable ? CONTINUE_FULL_ALPHA : CONTINUE_DIMMED_ALPHA;
        }

        public void SetContinueStage(int stageNumber, bool isEndless)
        {
            bool hasStage = stageNumber > 0;
            _continueStageChip.SetActive(hasStage);
            if (hasStage)
                _continueStageLabel.text = isEndless
                    ? LocalizationTable.Format(ENDLESS_CHIP_KEY, ENDLESS_CHIP_FORMAT, stageNumber)
                    : LocalizationTable.Format(STAGE_CHIP_KEY, STAGE_CHIP_FORMAT, stageNumber);

            ApplyContinueLabelLayout(hasStage);
        }

        void ApplyContinueLabelLayout(bool hasStage)
        {
            if (null == _continueLabelRect)
                return;

            _continueLabelRect.anchoredPosition = hasStage ? CONTINUE_LABEL_POSITION_WITH_CHIP : CONTINUE_LABEL_POSITION_CENTERED;
            _continueLabelRect.sizeDelta = hasStage ? CONTINUE_LABEL_SIZE_WITH_CHIP : CONTINUE_LABEL_SIZE_CENTERED;
        }

        public void Release()
        {
            _continueButton.onClick.RemoveListener(OnContinueClicked);
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _quitButton.onClick.RemoveListener(OnQuitClicked);

            _onContinueClicked = null;
            _onNewGameClicked = null;
            _onSettingsClicked = null;
            _onQuitClicked = null;
        }

        void OnContinueClicked()
        {
            _onContinueClicked?.Invoke();
        }

        void OnNewGameClicked()
        {
            _onNewGameClicked?.Invoke();
        }

        void OnSettingsClicked()
        {
            _onSettingsClicked?.Invoke();
        }

        void OnQuitClicked()
        {
            _onQuitClicked?.Invoke();
        }
    }
}
