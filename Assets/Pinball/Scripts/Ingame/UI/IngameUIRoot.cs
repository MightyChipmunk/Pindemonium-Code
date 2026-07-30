using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class IngameUIRoot : MonoBehaviour
    {
        [SerializeField] ScoreView _scoreView = null;
        [SerializeField] BallCountView _ballCountView = null;
        [SerializeField] ScorePopupSpawner _scorePopupSpawner = null;

        public void Init()
        {
            _scorePopupSpawner.Init();
        }

        public void Release()
        {
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
    }
}
