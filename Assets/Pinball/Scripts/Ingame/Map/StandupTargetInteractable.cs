using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class StandupTargetInteractable : CollisionInteractable
    {
        const float HIT_PUNCH_DISTANCE = 0.08f;
        const float HIT_PUNCH_DURATION = 0.18f;
        const int HIT_PUNCH_VIBRATO = 8;

        [SerializeField] Transform _visual = null;
        [SerializeField] int _scoreReward = 0;

        Tween _hitTween = null;

        protected override void OnRelease()
        {
            if (null != _hitTween)
            {
                _hitTween.Kill();
                _hitTween = null;
            }
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return transform.up;
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            GainScore(_scoreReward);

            if (null == _visual)
                return;

            if (null != _hitTween)
                _hitTween.Kill(true);

            _hitTween = _visual.DOPunchPosition(-transform.up * HIT_PUNCH_DISTANCE, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);
        }
    }
}
