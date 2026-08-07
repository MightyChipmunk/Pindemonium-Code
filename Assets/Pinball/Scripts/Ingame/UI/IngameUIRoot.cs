using System;
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

        public void Init()
        {
            _scorePopupSpawner.Init();

            if (null != _pauseButton)
                _pauseButton.onClick.AddListener(HandlePauseClicked);
        }

        public void Release()
        {
            if (null != _pauseButton)
                _pauseButton.onClick.RemoveListener(HandlePauseClicked);

            OnPauseClicked = null;
            _scorePopupSpawner.Release();
        }

        public void SetScore(int score)
        {
            _scoreView.SetValue(score);
        }

        public void SetBallCount(int count)
        {
            _ballCountView.SetValue(count);
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
