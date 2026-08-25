using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Shop;
using UnityEngine;

namespace Pinball.Ingame.Stat
{
    public static class StatProvider
    {
        const int MIN_SCALING_BALL_COUNT = 1;
        const int MIN_LAMP_REQUIRED_COUNT = 1;
        const float DEFAULT_SCALE = 1f;
        const float MIN_FLIP_POWER_SCALE = 0f;
        const string NOT_INITIALIZED_MESSAGE = "[StatProvider] 珥덇린?붾릺吏 ?딆? ?곹깭?먯꽌 ?ㅽ꺈??議고쉶?덉뒿?덈떎.";

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

        public static float GetHitGradeFlipPowerScale(Flipper.EHitGrade grade)
        {
            if (!HasEffect(ShopItemData.EEffectType.HitGradeFlipPowerRatio))
                return DEFAULT_SCALE;

            if (grade == Flipper.EHitGrade.Perfect)
                return DEFAULT_SCALE + GetEffectSum(ShopItemData.EEffectType.HitGradeFlipPowerRatio);

            if (grade == Flipper.EHitGrade.Normal)
                return Mathf.Max(MIN_FLIP_POWER_SCALE, DEFAULT_SCALE - GetEffectValue(ShopItemData.EEffectType.HitGradeFlipPowerRatio, ShopItemData.SUB_VALUE_INDEX));

            return DEFAULT_SCALE;
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

        public static float GetNudgeImpulsePower()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            return _statData.NudgeImpulsePower;
        }

        public static float GetNudgeCooldownSeconds()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            return _statData.NudgeCooldownSeconds;
        }

        public static float GetNudgeAimSeconds()
        {
            if (!EnsureReady() || null == _statData)
                return 0f;

            return _statData.NudgeAimSeconds;
        }

        public static float GetNudgeVelocityKeepRatio()
        {
            if (!EnsureReady() || null == _statData)
                return DEFAULT_SCALE;

            return Mathf.Clamp01(_statData.NudgeVelocityKeepRatio);
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

        public static float GetFinalDamageScale()
        {
            return DEFAULT_SCALE + GetEffectSum(ShopItemData.EEffectType.BallDamageRatio) + GetComboDigitDamageRatio();
        }

        static float GetComboDigitDamageRatio()
        {
            if (!HasEffect(ShopItemData.EEffectType.ComboDigitDamage))
                return 0f;

            if (null == _comboCountGetter)
                return 0f;

            int comboPerStep = Mathf.RoundToInt(GetEffectValue(ShopItemData.EEffectType.ComboDigitDamage, ShopItemData.SUB_VALUE_INDEX));
            int maxComboCount = Mathf.RoundToInt(GetEffectValue(ShopItemData.EEffectType.ComboDigitDamage, ShopItemData.EXTRA_VALUE_INDEX));
            if (comboPerStep <= 0 || maxComboCount <= 0)
                return 0f;

            int stepCount = Mathf.Min(_comboCountGetter(), maxComboCount) / comboPerStep;
            return GetEffectValue(ShopItemData.EEffectType.ComboDigitDamage, ShopItemData.MAIN_VALUE_INDEX) * stepCount;
        }

        public static float GetComboGainRate()
        {
            if (!HasEffect(ShopItemData.EEffectType.ComboGainRate))
                return DEFAULT_SCALE;

            return Mathf.Max(DEFAULT_SCALE, GetEffectValue(ShopItemData.EEffectType.ComboGainRate, ShopItemData.MAIN_VALUE_INDEX));
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

        public static float GetBumperHitScore()
        {
            return GetEffectSum(ShopItemData.EEffectType.BumperHitScoreAdd);
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

        public static float GetComboBonusScale()
        {
            if (!HasEffect(ShopItemData.EEffectType.ComboBonusScale))
                return DEFAULT_SCALE;

            return Mathf.Max(DEFAULT_SCALE, GetEffectValue(ShopItemData.EEffectType.ComboBonusScale, ShopItemData.MAIN_VALUE_INDEX));
        }

        public static float GetComboTimeoutBonus()
        {
            return GetEffectSum(ShopItemData.EEffectType.ComboTimeoutAdd);
        }

        public static float GetGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (!EnsureReady() || null == _loadout)
                return 0f;

            return _loadout.GetGimmickScoreRatio(gimmickType) + GetPassiveGimmickScoreRatio(gimmickType);
        }

        static float GetPassiveGimmickScoreRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (gimmickType == MapInteractable.EGimmickType.Bumper)
                return GetEffectSum(ShopItemData.EEffectType.BumperFortify);

            return GetEffectSum(ShopItemData.EEffectType.NonBumperScoreRatio);
        }

        public static float GetSpinnerScoreGrowth()
        {
            return GetEffectSum(ShopItemData.EEffectType.SpinnerScoreGrowth);
        }

        public static float GetSlingshotScoreGrowth()
        {
            return GetEffectSum(ShopItemData.EEffectType.SlingshotScoreGrowth);
        }

        public static float GetSaucerTargetBonus(bool isStrongTarget)
        {
            if (!HasEffect(ShopItemData.EEffectType.SaucerTargetBonus))
                return DEFAULT_SCALE;

            int valueIndex = isStrongTarget ? ShopItemData.MAIN_VALUE_INDEX : ShopItemData.SUB_VALUE_INDEX;
            return Mathf.Max(DEFAULT_SCALE, GetEffectValue(ShopItemData.EEffectType.SaucerTargetBonus, valueIndex));
        }

        public static bool HasSaucerTargetBonus()
        {
            return HasEffect(ShopItemData.EEffectType.SaucerTargetBonus);
        }

        public static int GetLampRequiredCount(int baseCount)
        {
            float delta = GetEffectSum(ShopItemData.EEffectType.LampRequirementUp) + GetEffectSum(ShopItemData.EEffectType.LampRequirementDown);
            return Mathf.Max(MIN_LAMP_REQUIRED_COUNT, baseCount + Mathf.RoundToInt(delta));
        }

        public static float GetLampScoreMultiplier()
        {
            float multiplier = DEFAULT_SCALE;

            if (HasEffect(ShopItemData.EEffectType.LampRequirementUp))
                multiplier *= GetEffectValue(ShopItemData.EEffectType.LampRequirementUp, ShopItemData.SUB_VALUE_INDEX);

            if (HasEffect(ShopItemData.EEffectType.LampRequirementDown))
                multiplier *= GetEffectValue(ShopItemData.EEffectType.LampRequirementDown, ShopItemData.SUB_VALUE_INDEX);

            return multiplier;
        }

        public static float GetStandupDamageScoreMultiplier(float damage)
        {
            if (!HasEffect(ShopItemData.EEffectType.StandupDamageScore))
                return DEFAULT_SCALE;

            float maxDamage = GetEffectValue(ShopItemData.EEffectType.StandupDamageScore, ShopItemData.SUB_VALUE_INDEX);
            if (maxDamage <= 0f)
                return DEFAULT_SCALE;

            float maxMultiplier = Mathf.Max(DEFAULT_SCALE, GetEffectValue(ShopItemData.EEffectType.StandupDamageScore, ShopItemData.MAIN_VALUE_INDEX));
            return Mathf.Lerp(DEFAULT_SCALE, maxMultiplier, Mathf.Clamp01(damage / maxDamage));
        }

        public static float GetDropTargetOrderMultiplier()
        {
            if (!HasEffect(ShopItemData.EEffectType.DropTargetOrderBonus))
                return DEFAULT_SCALE;

            return Mathf.Max(DEFAULT_SCALE, GetEffectValue(ShopItemData.EEffectType.DropTargetOrderBonus, ShopItemData.MAIN_VALUE_INDEX));
        }

        public static bool HasDropTargetOrder()
        {
            return HasEffect(ShopItemData.EEffectType.DropTargetOrderBonus);
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
