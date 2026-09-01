using System;
using Pinball.Ingame.Shop;

namespace Pinball.Ingame.Stat
{
    public class PowerSurgeCharger
    {
        const float NO_SURGE_MULTIPLIER = 1f;

        Func<bool> _isPlayingGetter = null;
        float _elapsed = 0f;
        bool _isCharged = false;

        public event Action<bool> OnChargedChanged;

        public PowerSurgeCharger(Func<bool> isPlayingGetter)
        {
            _isPlayingGetter = isPlayingGetter;
            _elapsed = 0f;
            _isCharged = false;
        }

        public void Tick(float deltaTime)
        {
            float chargeInterval = StatProvider.GetEffectValue(ShopItemData.EEffectType.TimedAttackPowerSurge, ShopItemData.SUB_VALUE_INDEX);
            if (chargeInterval <= 0f)
            {
                _elapsed = 0f;
                if (_isCharged)
                    SetCharged(false);

                return;
            }

            if (_isCharged)
                return;

            if (null != _isPlayingGetter && !_isPlayingGetter())
                return;

            _elapsed += deltaTime;
            if (_elapsed < chargeInterval)
                return;

            _elapsed = 0f;
            SetCharged(true);
        }

        public float ConsumeMultiplier()
        {
            if (!_isCharged)
                return NO_SURGE_MULTIPLIER;

            SetCharged(false);
            float multiplier = StatProvider.GetEffectSum(ShopItemData.EEffectType.TimedAttackPowerSurge);
            if (multiplier <= 0f)
                return NO_SURGE_MULTIPLIER;

            return multiplier;
        }

        void SetCharged(bool isCharged)
        {
            _isCharged = isCharged;
            OnChargedChanged?.Invoke(isCharged);
        }
    }
}
