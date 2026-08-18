using System;
using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public class StatProvider
    {
        const int MIN_SCALING_BALL_COUNT = 1;
        const float DEFAULT_PERFECT_IMPULSE_SCALE = 1f;

        PinballStatData _statData = null;
        PlayerLoadout _loadout = null;
        FlipperData _flipper = null;
        Func<int> _remainingBallGetter = null;
        Func<int> _comboCountGetter = null;

        public void Init(PinballStatData statData, PlayerLoadout loadout, FlipperData flipper, Func<int> remainingBallGetter, Func<int> comboCountGetter)
        {
            _statData = statData;
            _loadout = loadout;
            _flipper = flipper;
            _remainingBallGetter = remainingBallGetter;
            _comboCountGetter = comboCountGetter;
        }

        public void Release()
        {
            _statData = null;
            _loadout = null;
            _flipper = null;
            _remainingBallGetter = null;
            _comboCountGetter = null;
        }

        public float GetPerfectImpulseScale()
        {
            if (null == _flipper)
                return DEFAULT_PERFECT_IMPULSE_SCALE;

            if (_flipper.AbilityType != FlipperData.EAbilityType.PerfectImpulseScale)
                return DEFAULT_PERFECT_IMPULSE_SCALE;

            return _flipper.AbilityValue;
        }

        public float GetFlipPower()
        {
            if (null == _statData)
                return 0f;

            return _statData.FlipPower + GetEffectSum(ShopItemData.EEffectType.FlipPowerAdd);
        }

        public int GetInitialBallCount()
        {
            if (null == _statData)
                return 0;

            return _statData.InitialBallCount;
        }

        public float GetBallMass()
        {
            if (null == _statData)
                return 0f;

            return _statData.BallMass;
        }

        public float GetBallAttackPower()
        {
            if (null == _statData)
                return 0f;

            float basePower = _statData.BallAttackPower + GetEffectSum(ShopItemData.EEffectType.BallAttackPowerAdd);
            return basePower * (1f + GetComboAttackPowerRatio());
        }

        float GetComboAttackPowerRatio()
        {
            float ratioPerHit = GetEffectSum(ShopItemData.EEffectType.ComboAttackPowerRatio);
            if (ratioPerHit <= 0f)
                return 0f;

            if (null == _comboCountGetter)
                return 0f;

            return ratioPerHit * _comboCountGetter();
        }

        public float GetLowBallScoreRatio()
        {
            float maxRatio = GetEffectSum(ShopItemData.EEffectType.LowBallScoreRatio);
            if (maxRatio <= 0f)
                return 0f;

            if (null == _remainingBallGetter)
                return 0f;

            int initialCount = GetInitialBallCount();
            if (initialCount <= MIN_SCALING_BALL_COUNT)
                return maxRatio;

            int remainingCount = Mathf.Clamp(_remainingBallGetter(), MIN_SCALING_BALL_COUNT, initialCount);
            return maxRatio * (initialCount - remainingCount) / (initialCount - MIN_SCALING_BALL_COUNT);
        }

        public float GetHitFlatDamage()
        {
            return GetEffectSum(ShopItemData.EEffectType.HitFlatDamageAdd);
        }

        public float GetCloneDamageRatio()
        {
            return GetEffectSum(ShopItemData.EEffectType.BallCloneSpawn);
        }

        public float GetPerfectTimingRelax()
        {
            return GetEffectSum(ShopItemData.EEffectType.PerfectTimingRelax);
        }

        public float GetComboMultiplierBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboMultiplierAdd);
        }

        public float GetComboTimeoutBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboTimeoutAdd);
        }

        public float GetGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetGimmickScoreRatio(gimmickType);
        }

        public float GetEffectSum(ShopItemData.EEffectType effectType)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetEffectSum(effectType);
        }

        public float GetEffectValue(ShopItemData.EEffectType effectType, int valueIndex)
        {
            if (null == _loadout)
                return 0f;

            return _loadout.GetEffectValue(effectType, valueIndex);
        }

        public bool HasEffect(ShopItemData.EEffectType effectType)
        {
            if (null == _loadout)
                return false;

            return _loadout.HasEffect(effectType);
        }
    }
}
