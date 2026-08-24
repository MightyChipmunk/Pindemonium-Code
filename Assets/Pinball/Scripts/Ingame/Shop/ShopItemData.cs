using Alchemy.Inspector;
using Pinball.Ingame.Map;
using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame.Shop
{
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Pinball/Ingame/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public const int MAIN_VALUE_INDEX = 0;
        public const int SUB_VALUE_INDEX = 1;
        public const int EXTRA_VALUE_INDEX = 2;
        public const int BONUS_VALUE_INDEX = 3;

        public enum EItemKind
        {
            Passive = 0,
            Upgrade = 1,
        }

        public enum EPassiveCategory
        {
            Ball = 0,
            Map = 1,
            Common = 2,
        }

        public enum EEffectType
        {
            PerfectTimingRelax = 0,
            ComboMultiplierAdd = 1,
            BallAttackPowerAdd = 2,
            FlipPowerAdd = 3,
            ComboTimeoutAdd = 4,
            FragileBallScoreRatio = 5,
            PerfectParryScoreAdd = 6,
            BumperBurn = 7,
            BumperChainLightning = 8,
            GimmickScoreRatioAdd = 9,
            LowBallScoreRatio = 10,
            HitGradeScoreRatio = 11,
            ComboAttackPowerRatio = 12,
            GimmickBreakMoney = 13,
            BumperFreeze = 14,
            BallResupplyMoney = 15,
            TimedAttackPowerSurge = 16,
            BumperHitScoreAdd = 17,
            BallCloneSpawn = 18,
            UpgradeOnPassiveSell = 19,
            GimmickBreakScoreRatio = 20,
            BumperRespawnTimeRatio = 21,
            BumperSniperMark = 22,
            BumperBlast = 23,
            BallDamageRatio = 24,
            BumperFortify = 25,
            SpinnerScoreGrowth = 26,
            SlingshotScoreGrowth = 27,
            SaucerTargetBonus = 28,
            NonBumperScoreRatio = 29,
            LampRequirementUp = 30,
            LampRequirementDown = 31,
            StandupDamageScore = 32,
            DropTargetOrderBonus = 33,
        }

        [SerializeField] string _nameKey = string.Empty;
        [SerializeField] string _descriptionKey = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] int _price = 1;
        [SerializeField] EItemKind _kind = EItemKind.Upgrade;
        [SerializeField] EPassiveCategory _passiveCategory = EPassiveCategory.Common;
        [SerializeField] EEffectType _effectType = EEffectType.BallAttackPowerAdd;
        [SerializeField] float[] _effectValueArr = null;
        [SerializeField, ShowIf(nameof(ShowsGimmickType))] MapInteractable.EGimmickType _gimmickType = MapInteractable.EGimmickType.Bumper;

        object[] _textArgArr = null;

        bool ShowsGimmickType() => _passiveCategory == EPassiveCategory.Map || _effectType == EEffectType.GimmickScoreRatioAdd;

        public int Price => _price;
        public EItemKind Kind => _kind;
        public EPassiveCategory PassiveCategory => _passiveCategory;
        public EEffectType EffectType => _effectType;
        public MapInteractable.EGimmickType GimmickType => _gimmickType;

        public float GetEffectValue(int index)
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

        public LocalizedTextRef GetNameRef()
        {
            return new LocalizedTextRef(_nameKey, GetTextArgArr());
        }

        public LocalizedTextRef GetDescriptionRef()
        {
            return new LocalizedTextRef(_descriptionKey, GetTextArgArr());
        }

        public string GetDisplayName()
        {
            return GetNameRef().Resolve();
        }

        public string GetDescription()
        {
            return GetDescriptionRef().Resolve();
        }

        public Sprite GetIcon()
        {
            return _icon;
        }

        object[] GetTextArgArr()
        {
            if (null != _textArgArr)
                return _textArgArr;

            int valueCount = null == _effectValueArr ? 0 : _effectValueArr.Length;
            _textArgArr = new object[valueCount];
            for (int i = 0; i < valueCount; ++i)
                _textArgArr[i] = _effectValueArr[i];

            return _textArgArr;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _textArgArr = null;
        }
#endif // UNITY_EDITOR
    }
}
