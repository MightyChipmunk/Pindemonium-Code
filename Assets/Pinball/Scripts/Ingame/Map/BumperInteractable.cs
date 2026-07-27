using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class BumperInteractable : CollisionInteractable
    {
        const float PUNCH_SCALE = 0.2f;
        const float PUNCH_DURATION = 0.2f;

        [SerializeField] Transform _visual = null;

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
            return (collision.rigidbody.position - (Vector2)transform.position).normalized;
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            if (null == _visual)
                return;

            if (null != _hitTween)
                _hitTween.Kill(true);

            _hitTween = _visual.DOPunchScale(Vector3.one * PUNCH_SCALE, PUNCH_DURATION);
        }
    }
}
