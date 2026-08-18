using System;
using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public class StatProvider
    {
        const int MIN_SCALING_BALL_COUNT = 1;

        PinballStatData _statData = null;
        PlayerLoadout _loadout = null;
        FlipperData.Tuning _tuning = null;
        FlipperData.RunSetup _runSetup = null;
        Func<int> _remainingBallGetter = null;
        Func<int> _comboCountGetter = null;

        public void Init(PinballStatData statData, PlayerLoadout loadout, FlipperData flipper, Func<int> remainingBallGetter, Func<int> comboCountGetter)
        {
            _statData = statData;
            _loadout = loadout;
            _tuning = null == flipper ? FlipperData.DEFAULT_TUNING : flipper.GetTuning();
            _runSetup = null == flipper ? FlipperData.DEFAULT_RUN_SETUP : flipper.GetRunSetup();
            _remainingBallGetter = remainingBallGetter;
            _comboCountGetter = comboCountGetter;
        }

        public void Release()
        {
            _statData = null;
            _loadout = null;
            _tuning = null;
            _runSetup = null;
            _remainingBallGetter = null;
            _comboCountGetter = null;
        }

        public float GetPerfectImpulseScale()
        {
            return _tuning.PerfectImpulseScale;
        }

        public float GetFlipperLengthScale()
        {
            return _tuning.LengthScale;
        }

        public float GetFlipperRotateSpeedScale()
        {
            return _tuning.RotateSpeedScale;
        }

        public float GetFlipPower()
        {
            if (null == _statData)
                return 0f;

            return (_statData.FlipPower + GetEffectSum(ShopItemData.EEffectType.FlipPowerAdd)) * _tuning.FlipPowerScale;
        }

        public int GetInitialBallCount()
        {
            if (null == _statData)
                return 0;

            return Mathf.Max(MIN_SCALING_BALL_COUNT, _statData.InitialBallCount + _runSetup.StartBallAdd);
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
            return GetEffectSum(ShopItemData.EEffectType.PerfectTimingRelax) + _tuning.PerfectTimingWindowAdd;
        }

        public float GetBallResupplyMoney()
        {
            return GetEffectSum(ShopItemData.EEffectType.BallResupplyMoney) + _runSetup.BallResupplyMoneyAdd;
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
