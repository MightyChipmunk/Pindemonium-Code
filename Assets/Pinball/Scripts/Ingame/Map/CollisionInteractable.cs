using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class CollisionInteractable : MapInteractable
    {
        protected const float DEFAULT_DAMAGE_MULTIPLIER = 1f;

        [SerializeField] float _scoreMultiplier = 1f;
        [SerializeField] float _bounceForce = 5f;

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (null == collision.rigidbody)
                return;

            if (!IsBallLayer(collision.gameObject.layer))
                return;

            NotifyBallHit();

            float collisionImpulse = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
            float damage = EvaluateDamage(collisionImpulse) * GetDamageMultiplier();
            GainHitScore(damage, collision.GetContact(0).point);

            collision.rigidbody.AddForce(GetBounceDirection(collision) * GetBounceForce(), ForceMode2D.Impulse);
            OnBallHit(collision, damage);
        }

        protected abstract Vector2 GetBounceDirection(Collision2D collision);

        protected virtual void GainHitScore(float damage, Vector2 worldPoint)
        {
            GainScore(Mathf.RoundToInt(damage * GetScoreMultiplier()), worldPoint);
        }

        protected virtual float GetScoreMultiplier()
        {
            return _scoreMultiplier;
        }

        protected virtual float GetBounceForce()
        {
            return _bounceForce;
        }

        protected virtual float GetDamageMultiplier()
        {
            return DEFAULT_DAMAGE_MULTIPLIER;
        }

        protected virtual void OnBallHit(Collision2D collision, float damage) { }
    }
}
