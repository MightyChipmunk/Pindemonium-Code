using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SlingshotInteractable : CollisionInteractable
    {
        const float KICK_PUNCH_DISTANCE = 0.12f;
        const float KICK_PUNCH_DURATION = 0.15f;
        const int KICK_PUNCH_VIBRATO = 6;

        [SerializeField] Transform _visual = null;

        Tween _kickTween = null;

        protected override void OnRelease()
        {
            if (null != _kickTween)
            {
                _kickTween.Kill();
                _kickTween = null;
            }
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return transform.up;
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            if (null == _visual)
                return;

            if (null != _kickTween)
                _kickTween.Kill(true);

            _kickTween = _visual.DOPunchPosition(transform.up * KICK_PUNCH_DISTANCE, KICK_PUNCH_DURATION, KICK_PUNCH_VIBRATO);
        }
    }
}
