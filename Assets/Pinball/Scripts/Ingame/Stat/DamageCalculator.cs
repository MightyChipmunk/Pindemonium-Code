using System;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public class DamageCalculator
    {
        const float NO_SURGE_MULTIPLIER = 1f;
        const float DEFAULT_DAMAGE_STACK = 1f;
        const float EFFECT_IMPACT_SCALE = 12f;

        public record InitParam(
            Func<float> SurgeMultiplierConsumer,
            Func<Collider2D, bool> IsCloneBallGetter,
            Func<Collider2D, float> DamageStackMultiplierGetter);

        Func<float> _surgeMultiplierConsumer = null;
        Func<Collider2D, bool> _isCloneBallGetter = null;
        Func<Collider2D, float> _damageStackMultiplierGetter = null;

        public DamageCalculator(InitParam param)
        {
            _surgeMultiplierConsumer = param.SurgeMultiplierConsumer;
            _isCloneBallGetter = param.IsCloneBallGetter;
            _damageStackMultiplierGetter = param.DamageStackMultiplierGetter;
        }

        public float Calculate(Collider2D ballCollider, float collisionImpulse)
        {
            float attackPower = EvaluateAttackPower(ballCollider);
            if (IsCloneBall(ballCollider))
                return collisionImpulse * attackPower;

            return collisionImpulse * attackPower * ConsumeSurgeMultiplier();
        }

        public float EvaluateAttackPower(Collider2D ballCollider)
        {
            float attackPower = StatProvider.GetBallAttackPower() * StatProvider.GetFinalDamageScale() * GetDamageStackMultiplier(ballCollider);
            if (IsCloneBall(ballCollider))
                return attackPower * StatProvider.GetCloneDamageRatio();

            return attackPower;
        }

        public float EvaluateEffectDamage(float damageRatio)
        {
            return StatProvider.GetBallAttackPower() * StatProvider.GetFinalDamageScale() * damageRatio * EFFECT_IMPACT_SCALE;
        }

        float GetDamageStackMultiplier(Collider2D ballCollider)
        {
            if (null == _damageStackMultiplierGetter)
                return DEFAULT_DAMAGE_STACK;

            return _damageStackMultiplierGetter(ballCollider);
        }

        bool IsCloneBall(Collider2D ballCollider)
        {
            if (null == _isCloneBallGetter)
                return false;

            return _isCloneBallGetter(ballCollider);
        }

        float ConsumeSurgeMultiplier()
        {
            if (null == _surgeMultiplierConsumer)
                return NO_SURGE_MULTIPLIER;

            return _surgeMultiplierConsumer();
        }
    }
}
