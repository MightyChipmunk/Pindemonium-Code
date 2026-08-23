using System;
using System.Collections.Generic;
using Pinball.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.UI
{
    public class IngameUIRoot : MonoBehaviour
    {
        const int NO_STAGE_NUMBER = 0;

        [SerializeField] ScoreView _scoreView = null;
        [SerializeField] BallCountView _ballCountView = null;
        [SerializeField] OwnedItemsView _ownedItemsView = null;
        [SerializeField] Pinball.UI.StageInfoView _stageInfoView = null;
        [SerializeField] Pinball.UI.ComboView _comboView = null;
        [SerializeField] ScorePopupSpawner _scorePopupSpawner = null;
        [SerializeField] Button _pauseButton = null;

        public event Action OnPauseClicked;

        int _score = 0;
        int _ballCount = 0;
        int _stageNumber = NO_STAGE_NUMBER;
        int _stageTargetScore = 0;
        int _stageRemainingSeconds = Pinball.UI.StageInfoView.NO_TIME_LIMIT;

        public void Init()
        {
            _scorePopupSpawner.Init();
            _ownedItemsView.Init();
            LocalizationManager.OnLanguageChanged += RefreshLabels;

            if (null != _pauseButton)
                _pauseButton.onClick.AddListener(HandlePauseClicked);
        }

        public void Release()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLabels;

            if (null != _pauseButton)
                _pauseButton.onClick.RemoveListener(HandlePauseClicked);

            OnPauseClicked = null;
            _ownedItemsView.Release();
            _scorePopupSpawner.Release();
        }

        public void SetStageScore(int score)
        {
            _score = score;
            _scoreView.SetValue(score);
            _stageInfoView.SetScore(score);
        }

        public void SetBallCount(int count)
        {
            _ballCount = count;
            _ballCountView.SetValue(count);
        }

        public void SetOwnedItems(List<OwnedItemsView.Entry> passives, List<OwnedItemsView.Entry> upgrades)
        {
            _ownedItemsView.SetItems(passives, upgrades);
        }

        public void SetStageInfo(int stageNumber, int targetScore, int remainingSeconds)
        {
            _stageNumber = stageNumber;
            _stageTargetScore = targetScore;
            _stageRemainingSeconds = remainingSeconds;
            _stageInfoView.SetInfo(stageNumber, targetScore, remainingSeconds);
        }

        public void SetCombo(int comboCount, float multiplier, int pendingBonus, float stepProgress)
        {
            _comboView.SetCombo(comboCount, multiplier, pendingBonus, stepProgress);
        }

        public void ShowComboBonus(int comboCount, float multiplier, int bonusScore)
        {
            _comboView.ShowBonus(comboCount, multiplier, bonusScore);
        }

        public void HideCombo()
        {
            _comboView.Hide();
        }

        void RefreshLabels()
        {
            _scoreView.SetValue(_score);
            _ballCountView.SetValue(_ballCount);

            if (_stageNumber == NO_STAGE_NUMBER)
                return;

            _stageInfoView.SetInfo(_stageNumber, _stageTargetScore, _stageRemainingSeconds);
        }

        public void PlayScorePopup(int score, Vector2 worldPoint)
        {
            _scorePopupSpawner.Play(score, worldPoint);
        }

        void HandlePauseClicked()
        {
            OnPauseClicked?.Invoke();
        }
    }
}
