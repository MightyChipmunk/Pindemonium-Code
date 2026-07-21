using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class MapInteractable : MonoBehaviour
    {
        [SerializeField] LayerMask _ballLayerMask = default;
        [SerializeField] float _scoreMultiplier = 1f;
        [SerializeField] float _bounceForce = 5f;

        Action<int> _onScoreGained = null;
        Func<float, float> _damageEvaluator = null;

        public void Init(Action<int> onScoreGained, Func<float, float> damageEvaluator)
        {
            _onScoreGained = onScoreGained;
            _damageEvaluator = damageEvaluator;
        }

        public virtual void Release()
        {
            _onScoreGained = null;
            _damageEvaluator = null;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (null == collision.rigidbody)
                return;

            if (!_ballLayerMask.HasLayer(collision.gameObject.layer))
                return;

            float collisionImpulse = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
            float damage = EvaluateDamage(collisionImpulse);
            int score = Mathf.RoundToInt(damage * _scoreMultiplier);
            _onScoreGained?.Invoke(score);

            Vector2 bounceDirection = (collision.rigidbody.position - (Vector2)transform.position).normalized;
            collision.rigidbody.AddForce(bounceDirection * _bounceForce, ForceMode2D.Impulse);
            OnBallHit(collision, damage);
        }

        float EvaluateDamage(float collisionImpulse)
        {
            if (null == _damageEvaluator)
                return 0f;

            return _damageEvaluator(collisionImpulse);
        }

        protected virtual void OnBallHit(Collision2D collision, float damage) { }
    }
}
