using System;
using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SlingshotInteractable : CollisionInteractable, IDamageable
    {
        public enum ESide
        {
            Left = 0,
            Right = 1,
        }

        const float KICK_PUNCH_DISTANCE = 0.12f;
        const float KICK_PUNCH_DURATION = 0.15f;
        const int KICK_PUNCH_VIBRATO = 6;
        const float DEFAULT_HIT_SCORE_MULTIPLIER = 1f;

        [SerializeField] Transform _visual = null;
        [SerializeField] Transform _directionTransform = null;
        [SerializeField] ESide _side = ESide.Left;

        Tween _kickTween = null;
        Func<ESide, float> _hitScoreMultiplierConsumer = null;

        public override EGimmickType GimmickType => EGimmickType.Slingshot;

        public event Action<Collider2D> OnBallBounced;

        public void SetHitScoreMultiplierConsumer(Func<ESide, float> consumer)
        {
            _hitScoreMultiplierConsumer = consumer;
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            if (IsDeactivated)
                return;

            GainScore(Mathf.RoundToInt(damage * GetScoreMultiplier()), worldPoint, EScoreSource.PassiveEffect);
        }

        protected override void OnRelease()
        {
            OnBallBounced = null;
            _hitScoreMultiplierConsumer = null;

            if (null != _kickTween)
            {
                _kickTween.Kill();
                _kickTween = null;
            }
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            if (null == _directionTransform)
            {
                Debug.LogError($"{nameof(_directionTransform)} is unsigned");
                return transform.up;
            }

            return _directionTransform.up;
        }

        protected override void GainHitScore(float damage, Vector2 worldPoint)
        {
            float multiplier = null == _hitScoreMultiplierConsumer ? DEFAULT_HIT_SCORE_MULTIPLIER : _hitScoreMultiplierConsumer(_side);
            base.GainHitScore(damage * multiplier, worldPoint);
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            OnBallBounced?.Invoke(collision.collider);

            if (null == _visual)
                return;

            if (null != _kickTween)
                _kickTween.Kill(true);

            _kickTween = _visual.DOPunchPosition(transform.up * KICK_PUNCH_DISTANCE, KICK_PUNCH_DURATION, KICK_PUNCH_VIBRATO);
        }
    }
}
