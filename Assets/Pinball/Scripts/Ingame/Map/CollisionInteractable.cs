using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class CollisionInteractable : MapInteractable
    {
        [SerializeField] float _scoreMultiplier = 1f;
        [SerializeField] float _bounceForce = 5f;

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (null == collision.rigidbody)
                return;

            if (!IsBallLayer(collision.gameObject.layer))
                return;

            float collisionImpulse = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
            float damage = EvaluateDamage(collisionImpulse);
            GainScore(Mathf.RoundToInt(damage * _scoreMultiplier));

            collision.rigidbody.AddForce(GetBounceDirection(collision) * _bounceForce, ForceMode2D.Impulse);
            OnBallHit(collision, damage);
        }

        protected abstract Vector2 GetBounceDirection(Collision2D collision);

        protected virtual void OnBallHit(Collision2D collision, float damage) { }
    }
}
