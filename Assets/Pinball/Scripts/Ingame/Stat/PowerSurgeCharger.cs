using System;

namespace Pinball.Ingame.Stat
{
    public class PowerSurgeCharger
    {
        const float NO_SURGE_MULTIPLIER = 1f;

        StatProvider _stats = null;
        Func<bool> _isPlayingGetter = null;
        float _elapsed = 0f;
        bool _isCharged = false;

        public event Action<bool> OnChargedChanged;

        public void Init(StatProvider stats, Func<bool> isPlayingGetter)
        {
            _stats = stats;
            _isPlayingGetter = isPlayingGetter;
            _elapsed = 0f;
            _isCharged = false;
        }

        public void Release()
        {
            OnChargedChanged = null;
            _stats = null;
            _isPlayingGetter = null;
            _elapsed = 0f;
            _isCharged = false;
        }

        public void Tick(float deltaTime)
        {
            if (null == _stats)
                return;

            if (_isCharged)
                return;

            if (null != _isPlayingGetter && !_isPlayingGetter())
                return;

            float chargeInterval = _stats.GetEffectValue(ShopItemData.EEffectType.TimedAttackPowerSurge, ShopItemData.SUB_VALUE_INDEX);
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
            return _stats.GetEffectSum(ShopItemData.EEffectType.TimedAttackPowerSurge);
        }

        void SetCharged(bool isCharged)
        {
            _isCharged = isCharged;
            OnChargedChanged?.Invoke(isCharged);
        }
    }
}
