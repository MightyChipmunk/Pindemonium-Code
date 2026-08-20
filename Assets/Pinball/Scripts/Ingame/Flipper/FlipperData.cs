using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "FlipperData", menuName = "Pinball/Ingame/FlipperData")]
    public class FlipperData : ScriptableObject
    {
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
        [SerializeField] float _perfectImpulseScale = 1f;
        [SerializeField] float _perfectTimingWindowAdd = 0f;
        [SerializeField] float _flipPowerScale = 1f;
        [SerializeField] float _lengthScale = 1f;
        [SerializeField] float _rotateSpeedScale = 1f;
        [SerializeField] int _passiveSlotAdd = 0;
        [SerializeField] int _startMoneyAdd = 0;
        [SerializeField] int _startBallAdd = 0;
        [SerializeField] int _shopOfferSlotAdd = 0;
        [SerializeField] int _stageClearMoneyAdd = 0;
        [SerializeField] int _startPassiveCount = 0;
        [SerializeField] int _ballResupplyMoneyAdd = 0;
        [SerializeField] bool _isInterestDisabled = false;

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
            return new Tuning(
                PerfectImpulseScale: _perfectImpulseScale,
                PerfectTimingWindowAdd: _perfectTimingWindowAdd,
                FlipPowerScale: _flipPowerScale,
                LengthScale: _lengthScale,
                RotateSpeedScale: _rotateSpeedScale);
        }

        public RunSetup GetRunSetup()
        {
            return new RunSetup(
                PassiveSlotAdd: _passiveSlotAdd,
                StartMoneyAdd: _startMoneyAdd,
                StartBallAdd: _startBallAdd,
                ShopOfferSlotAdd: _shopOfferSlotAdd,
                StageClearMoneyAdd: _stageClearMoneyAdd,
                StartPassiveCount: _startPassiveCount,
                BallResupplyMoneyAdd: _ballResupplyMoneyAdd,
                IsInterestDisabled: _isInterestDisabled);
        }
    }
}
