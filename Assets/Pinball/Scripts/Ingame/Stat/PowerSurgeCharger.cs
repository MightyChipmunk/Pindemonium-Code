using System;

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
            if (_isCharged)
                return;

            if (null != _isPlayingGetter && !_isPlayingGetter())
                return;

            float chargeInterval = StatProvider.GetEffectValue(ShopItemData.EEffectType.TimedAttackPowerSurge, ShopItemData.SUB_VALUE_INDEX);
            if (chargeInterval <= 0f)
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
            return StatProvider.GetEffectSum(ShopItemData.EEffectType.TimedAttackPowerSurge);
        }

        void SetCharged(bool isCharged)
        {
            _isCharged = isCharged;
            OnChargedChanged?.Invoke(isCharged);
        }
    }
}
