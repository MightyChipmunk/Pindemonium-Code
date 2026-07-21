using System;

namespace Pinball.Ingame.Stat
{
    public class DamageCalculator
    {
        Func<float> _attackPowerGetter = null;

        public void Init(Func<float> attackPowerGetter)
        {
            _attackPowerGetter = attackPowerGetter;
        }

        public void Release()
        {
            _attackPowerGetter = null;
        }

        public float Calculate(float collisionImpulse)
        {
            if (null == _attackPowerGetter)
                return 0f;

            return collisionImpulse * _attackPowerGetter();
        }
    }
}
