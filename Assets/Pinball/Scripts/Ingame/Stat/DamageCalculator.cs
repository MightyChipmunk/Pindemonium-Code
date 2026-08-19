using System;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public class DamageCalculator
    {
        const float NO_SURGE_MULTIPLIER = 1f;

        public record InitParam(
            Func<float> SurgeMultiplierConsumer,
            Func<Collider2D, bool> IsCloneBallGetter);

        Func<float> _surgeMultiplierConsumer = null;
        Func<Collider2D, bool> _isCloneBallGetter = null;

        public DamageCalculator(InitParam param)
        {
            _surgeMultiplierConsumer = param.SurgeMultiplierConsumer;
            _isCloneBallGetter = param.IsCloneBallGetter;
        }

        public float Calculate(Collider2D ballCollider, float collisionImpulse)
        {
            float attackPower = StatProvider.GetBallAttackPower();
            float flatDamage = StatProvider.GetHitFlatDamage();
            if (IsCloneBall(ballCollider))
                return (collisionImpulse * attackPower + flatDamage) * StatProvider.GetCloneDamageRatio();

            return collisionImpulse * attackPower * ConsumeSurgeMultiplier() + flatDamage;
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
