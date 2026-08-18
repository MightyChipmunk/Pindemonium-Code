using Pinball.Ingame.Map;
using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Pinball/Ingame/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public const int MAIN_VALUE_INDEX = 0;
        public const int SUB_VALUE_INDEX = 1;

        const string KEY_PREFIX = "item.";
        const string NAME_KEY_SUFFIX = ".name";
        const string DESCRIPTION_KEY_SUFFIX = ".desc";

        public enum EItemKind
        {
            Passive = 0,
            Upgrade = 1,
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
            HitFlatDamageAdd = 17,
            BallCloneSpawn = 18,
        }

        [SerializeField] string _displayName = string.Empty;
        [SerializeField, TextArea] string _description = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] int _price = 1;
        [SerializeField] EItemKind _kind = EItemKind.Upgrade;
        [SerializeField] EEffectType _effectType = EEffectType.BallAttackPowerAdd;
        [SerializeField] float[] _effectValueArr = null;
        [SerializeField] MapInteractable.EGimmickType _gimmickType = MapInteractable.EGimmickType.Bumper;

        public int Price => _price;
        public EItemKind Kind => _kind;
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

        public string GetDisplayName()
        {
            return LocalizationTable.Get(KEY_PREFIX + GetId() + NAME_KEY_SUFFIX, _displayName);
        }

        public string GetDescription()
        {
            return LocalizationTable.Get(KEY_PREFIX + GetId() + DESCRIPTION_KEY_SUFFIX, _description);
        }

        public Sprite GetIcon()
        {
            return _icon;
        }
    }
}
