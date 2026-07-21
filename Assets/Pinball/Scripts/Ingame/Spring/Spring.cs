using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Spring : MonoBehaviour
    {
        const float COMPRESS_SCALE_Y = 0.6f;
        const float COMPRESS_DURATION = 0.05f;
        const float RESTORE_DURATION = 0.15f;

        [SerializeField] Transform _ballAnchor = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] float _launchImpulse = 10f;

        Ball _loadedBall = null;
        Tween _launchTween = null;

        public void Init()
        {
            _loadedBall = null;
        }

        public void Release()
        {
            _loadedBall = null;

            if (null != _launchTween)
            {
                _launchTween.Kill();
                _launchTween = null;
            }
        }

        public void LoadBall(Ball ball)
        {
            if (null == ball)
                return;

            _loadedBall = ball;
            ball.SetKinematic(true);
            ball.MoveTo(_ballAnchor.position);
        }

        public bool TryLaunch()
        {
            if (null == _loadedBall)
                return false;

            Ball ball = _loadedBall;
            _loadedBall = null;
            ball.SetKinematic(false);
            ball.AddImpulse(Vector2.up * _launchImpulse);
            PlayLaunchMotion();
            return true;
        }

        void PlayLaunchMotion()
        {
            if (null == _visual)
                return;

            if (null != _launchTween)
                _launchTween.Kill();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_visual.DOScaleY(COMPRESS_SCALE_Y, COMPRESS_DURATION));
            sequence.Append(_visual.DOScaleY(1f, RESTORE_DURATION));
            _launchTween = sequence;
        }
    }
}
