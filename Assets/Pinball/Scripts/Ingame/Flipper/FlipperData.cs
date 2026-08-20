using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "FlipperData", menuName = "Pinball/Ingame/FlipperData")]
    public class FlipperData : ScriptableObject
    {
        const int MAIN_VALUE_INDEX = 0;
        const int SUB_VALUE_INDEX = 1;

        const float DEFAULT_SCALE = 1f;

        public enum EEffectType
        {
            None = 0,
            PerfectImpulseScale = 1,
            PerfectTimingRelax = 2,
            FlipperReachExtend = 3,
            StartBallAdd = 4,
            BallResupplyMoneyAdd = 5,
            StartPassiveGrant = 6,
            PassiveSlotAdd = 7,
            StartMoneyNoInterest = 8,
            ShopOfferSlotAdd = 9,
        }

        public record Tuning(
            float PerfectImpulseScale,
            float PerfectTimingWindowAdd,
            float FlipPowerScale,
            float LengthScale,
            float RotateSpeedScale);

        public record RunSetup(
            int PassiveSlotAdd,
            int StartMoneyAdd,
            int StartBallAdd,
            int ShopOfferSlotAdd,
            int StageClearMoneyAdd,
            int StartPassiveCount,
            int BallResupplyMoneyAdd,
            bool IsInterestDisabled);

        public static readonly Tuning DEFAULT_TUNING = new Tuning(1f, 0f, 1f, 1f, 1f);
        public static readonly RunSetup DEFAULT_RUN_SETUP = new RunSetup(0, 0, 0, 0, 0, 0, 0, false);

        [SerializeField] string _nameKey = string.Empty;
        [SerializeField] string _abilityKey = string.Empty;
        [SerializeField] string _drawbackKey = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] EEffectType _effectType = EEffectType.None;
        [SerializeField] float[] _effectValueArr = null;

        float GetEffectValue(int index)
        {
            if (null == _effectValueArr)
                return 0f;

            if (index < 0 || index >= _effectValueArr.Length)
                return 0f;

            return _effectValueArr[index];
        }

        public string GetId()
        {
            return name;
        }

        public string GetNameKey()
        {
            return _nameKey;
        }

        public string GetAbilityKey()
        {
            return _abilityKey;
        }

        public string GetDrawbackKey()
        {
            return _drawbackKey;
        }

        public Sprite GetIcon()
        {
            return _icon;
        }

        public Tuning GetTuning()
        {
            switch (_effectType)
            {
                case EEffectType.PerfectImpulseScale:
                    return DEFAULT_TUNING with
                    {
                        PerfectImpulseScale = GetScaleValue(MAIN_VALUE_INDEX)
                    };
                case EEffectType.PerfectTimingRelax:
                    return DEFAULT_TUNING with
                    {
                        PerfectTimingWindowAdd = GetEffectValue(MAIN_VALUE_INDEX),
                        PerfectImpulseScale = GetScaleValue(SUB_VALUE_INDEX)
                    };
                case EEffectType.FlipperReachExtend:
                    return DEFAULT_TUNING with
                    {
                        LengthScale = GetScaleValue(MAIN_VALUE_INDEX),
                        RotateSpeedScale = GetScaleValue(SUB_VALUE_INDEX)
                    };
                case EEffectType.StartBallAdd:
                    return DEFAULT_TUNING with
                    {
                        FlipPowerScale = GetScaleValue(SUB_VALUE_INDEX)
                    };
                default:
                    return DEFAULT_TUNING;
            }
        }

        public RunSetup GetRunSetup()
        {
            switch (_effectType)
            {
                case EEffectType.StartBallAdd:
                    return DEFAULT_RUN_SETUP with
                    {
                        StartBallAdd = GetCountValue(MAIN_VALUE_INDEX)
                    };
                case EEffectType.BallResupplyMoneyAdd:
                    return DEFAULT_RUN_SETUP with
                    {
                        BallResupplyMoneyAdd = GetCountValue(MAIN_VALUE_INDEX),
                        StartBallAdd = GetCountValue(SUB_VALUE_INDEX)
                    };
                case EEffectType.StartPassiveGrant:
                    return DEFAULT_RUN_SETUP with
                    {
                        StartPassiveCount = GetCountValue(MAIN_VALUE_INDEX),
                        ShopOfferSlotAdd = GetCountValue(SUB_VALUE_INDEX)
                    };
                case EEffectType.PassiveSlotAdd:
                    return DEFAULT_RUN_SETUP with
                    {
                        PassiveSlotAdd = GetCountValue(MAIN_VALUE_INDEX),
                        StageClearMoneyAdd = GetCountValue(SUB_VALUE_INDEX)
                    };
                case EEffectType.StartMoneyNoInterest:
                    return DEFAULT_RUN_SETUP with
                    {
                        StartMoneyAdd = GetCountValue(MAIN_VALUE_INDEX),
                        IsInterestDisabled = true
                    };
                case EEffectType.ShopOfferSlotAdd:
                    return DEFAULT_RUN_SETUP with
                    {
                        ShopOfferSlotAdd = GetCountValue(MAIN_VALUE_INDEX)
                    };
                default:
                    return DEFAULT_RUN_SETUP;
            }
        }

        float GetScaleValue(int index)
        {
            if (null == _effectValueArr)
                return DEFAULT_SCALE;

            if (index < 0 || index >= _effectValueArr.Length)
                return DEFAULT_SCALE;

            return _effectValueArr[index];
        }

        int GetCountValue(int index)
        {
            return Mathf.RoundToInt(GetEffectValue(index));
        }
    }
}
