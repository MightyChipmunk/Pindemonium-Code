using UnityEngine;

namespace Pinball.Ingame.UI
{
    public class IngameUIRoot : MonoBehaviour
    {
        [SerializeField] ScoreView _scoreView = null;
        [SerializeField] BallCountView _ballCountView = null;

        public void Init() { }

        public void Release() { }

        public void SetScore(int score)
        {
            _scoreView.SetValue(score);
        }

        public void SetBallCount(int count)
        {
            _ballCountView.SetValue(count);
        }
    }
}
