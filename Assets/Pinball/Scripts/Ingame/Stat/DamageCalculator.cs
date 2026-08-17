using System;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public class DamageCalculator
    {
        const float NO_SURGE_MULTIPLIER = 1f;

        public record InitParam(
            Func<float> AttackPowerGetter,
            Func<float> FlatDamageGetter,
            Func<float> SurgeMultiplierConsumer,
            Func<Collider2D, bool> IsCloneBallGetter,
            Func<float> CloneDamageRatioGetter);

        Func<float> _attackPowerGetter = null;
        Func<float> _flatDamageGetter = null;
        Func<float> _surgeMultiplierConsumer = null;
        Func<Collider2D, bool> _isCloneBallGetter = null;
        Func<float> _cloneDamageRatioGetter = null;

        public void Init(InitParam param)
        {
            _attackPowerGetter = param.AttackPowerGetter;
            _flatDamageGetter = param.FlatDamageGetter;
            _surgeMultiplierConsumer = param.SurgeMultiplierConsumer;
            _isCloneBallGetter = param.IsCloneBallGetter;
            _cloneDamageRatioGetter = param.CloneDamageRatioGetter;
        }

        public void Release()
        {
            _attackPowerGetter = null;
            _flatDamageGetter = null;
            _surgeMultiplierConsumer = null;
            _isCloneBallGetter = null;
            _cloneDamageRatioGetter = null;
        }

        public float Calculate(Collider2D ballCollider, float collisionImpulse)
        {
            if (null == _attackPowerGetter)
                return 0f;

            if (IsCloneBall(ballCollider))
                return (collisionImpulse * _attackPowerGetter() + GetFlatDamage()) * GetCloneDamageRatio();

            return collisionImpulse * _attackPowerGetter() * ConsumeSurgeMultiplier() + GetFlatDamage();
        }

        bool IsCloneBall(Collider2D ballCollider)
        {
            if (null == _isCloneBallGetter)
                return false;

            return _isCloneBallGetter(ballCollider);
        }

        float GetCloneDamageRatio()
        {
            if (null == _cloneDamageRatioGetter)
                return 0f;

            return _cloneDamageRatioGetter();
        }

        float GetFlatDamage()
        {
            if (null == _flatDamageGetter)
                return 0f;

            return _flatDamageGetter();
        }

        float ConsumeSurgeMultiplier()
        {
            if (null == _surgeMultiplierConsumer)
                return NO_SURGE_MULTIPLIER;

            return _surgeMultiplierConsumer();
        }
    }
}
