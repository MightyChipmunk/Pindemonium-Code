using System;
using Pinball.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.UI
{
    public class IngameUIRoot : MonoBehaviour
    {
        [SerializeField] ScoreView _scoreView = null;
        [SerializeField] BallCountView _ballCountView = null;
        [SerializeField] ScorePopupSpawner _scorePopupSpawner = null;
        [SerializeField] Button _pauseButton = null;

        public event Action OnPauseClicked;

        int _score = 0;
        int _ballCount = 0;

        public void Init()
        {
            _scorePopupSpawner.Init();
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
            _scorePopupSpawner.Release();
        }

        public void SetScore(int score)
        {
            _score = score;
            _scoreView.SetValue(score);
        }

        public void SetBallCount(int count)
        {
            _ballCount = count;
            _ballCountView.SetValue(count);
        }

        void RefreshLabels()
        {
            _scoreView.SetValue(_score);
            _ballCountView.SetValue(_ballCount);
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
