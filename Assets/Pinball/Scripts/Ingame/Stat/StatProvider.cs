using System;
using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public static class StatProvider
    {
        const int MIN_SCALING_BALL_COUNT = 1;
        const float DEFAULT_SCALE = 1f;
        const string NOT_INITIALIZED_MESSAGE = "[StatProvider] 초기화되지 않은 상태에서 스탯을 조회했습니다.";

        static PinballStatData _statData = null;
        static PlayerLoadout _loadout = null;
        static PlayerVouchers _vouchers = null;
        static FlipperData.Tuning _tuning = null;
        static FlipperData.RunSetup _runSetup = null;
        static Func<int> _remainingBallGetter = null;
        static Func<int> _comboCountGetter = null;
        static bool _isInitialized = false;
        static bool _isMissingReported = false;

        public static bool IsInitialized => _isInitialized;

        public static void Init(PinballStatData statData, PlayerLoadout loadout, PlayerVouchers vouchers, FlipperData flipper, Func<int> remainingBallGetter, Func<int> comboCountGetter)
        {
            _statData = statData;
            _loadout = loadout;
            _vouchers = vouchers;
            _tuning = null == flipper ? FlipperData.DEFAULT_TUNING : flipper.GetTuning();
            _runSetup = null == flipper ? FlipperData.DEFAULT_RUN_SETUP : flipper.GetRunSetup();
            _remainingBallGetter = remainingBallGetter;
            _comboCountGetter = comboCountGetter;
            _isInitialized = true;
            _isMissingReported = false;
        }

        public static void Release()
        {
            _isInitialized = false;
            _statData = null;
            _loadout = null;
            _vouchers = null;
            _tuning = null;
            _runSetup = null;
            _remainingBallGetter = null;
            _comboCountGetter = null;
        }

        public static float GetPerfectImpulseScale()
        {
            return EnsureReady() ? _tuning.PerfectImpulseScale : DEFAULT_SCALE;
        }

        public static float GetFlipperLengthScale()
        {
            return EnsureReady() ? _tuning.LengthScale : DEFAULT_SCALE;
        }

        public static float GetFlipperRotateSpeedScale()
        {
            return EnsureReady() ? _tuning.RotateSpeedScale : DEFAULT_SCALE;
        }

        public static float GetFlipPower()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            return (_statData.FlipPower + GetEffectSum(ShopItemData.EEffectType.FlipPowerAdd)) * _tuning.FlipPowerScale;
        }

        public static int GetInitialBallCount()
        {
            if (!EnsureReady() || null == _statData)
                return 0;

            int voucherBallAdd = null == _vouchers ? 0 : Mathf.RoundToInt(_vouchers.GetEffectSum(VoucherData.EEffect.BallAdd));
            return Mathf.Max(MIN_SCALING_BALL_COUNT, _statData.InitialBallCount + _runSetup.StartBallAdd + voucherBallAdd);
        }

        public static float GetBallMass()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            return _statData.BallMass;
        }

        public static float GetBallAttackPower()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            float basePower = _statData.BallAttackPower + GetEffectSum(ShopItemData.EEffectType.BallAttackPowerAdd);
            return basePower * (1f + GetComboAttackPowerRatio());
        }

        static float GetComboAttackPowerRatio()
        {
            float ratioPerHit = GetEffectSum(ShopItemData.EEffectType.ComboAttackPowerRatio);
            if (ratioPerHit <= 0f)
                return 0f;

            if (null == _comboCountGetter)
                return 0f;

            return ratioPerHit * _comboCountGetter();
        }

        public static float GetLowBallScoreRatio()
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

        public static float GetHitFlatDamage()
        {
            return GetEffectSum(ShopItemData.EEffectType.HitFlatDamageAdd);
        }

        public static float GetCloneDamageRatio()
        {
            return GetEffectSum(ShopItemData.EEffectType.BallCloneSpawn);
        }

        public static float GetPerfectTimingRelax()
        {
            if (!EnsureReady())
                return 0f;

            return GetEffectSum(ShopItemData.EEffectType.PerfectTimingRelax) + _tuning.PerfectTimingWindowAdd;
        }

        public static float GetBallResupplyMoney()
        {
            if (!EnsureReady())
                return 0f;

            float voucherBonus = null == _vouchers ? 0f : _vouchers.GetEffectSum(VoucherData.EEffect.BallResupplyMoney);
            return GetEffectSum(ShopItemData.EEffectType.BallResupplyMoney) + _runSetup.BallResupplyMoneyAdd + voucherBonus;
        }

        public static float GetComboMultiplierBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboMultiplierAdd);
        }

        public static float GetComboTimeoutBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboTimeoutAdd);
        }

        public static float GetGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (!EnsureReady() || null == _loadout)
                return 0f;

            return _loadout.GetGimmickScoreRatio(gimmickType);
        }

        public static float GetEffectSum(ShopItemData.EEffectType effectType)
        {
            if (!EnsureReady() || null == _loadout)
                return 0f;

            return _loadout.GetEffectSum(effectType);
        }

        public static float GetEffectValue(ShopItemData.EEffectType effectType, int valueIndex)
        {
            if (!EnsureReady() || null == _loadout)
                return 0f;

            return _loadout.GetEffectValue(effectType, valueIndex);
        }

        public static bool HasEffect(ShopItemData.EEffectType effectType)
        {
            if (!EnsureReady() || null == _loadout)
                return false;

            return _loadout.HasEffect(effectType);
        }

        static bool EnsureReady()
        {
            if (_isInitialized)
                return true;

            if (!_isMissingReported)
            {
                _isMissingReported = true;
                Debug.LogError(NOT_INITIALIZED_MESSAGE);
            }

            return false;
        }
    }
}
