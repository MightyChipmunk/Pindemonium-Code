using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Pinball/Ingame/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
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
        }

        [SerializeField] string _displayName = string.Empty;
        [SerializeField, TextArea] string _description = string.Empty;
        [SerializeField] int _price = 1;
        [SerializeField] EItemKind _kind = EItemKind.Upgrade;
        [SerializeField] EEffectType _effectType = EEffectType.BallAttackPowerAdd;
        [SerializeField] float _effectValue = 0f;
        [SerializeField] float _effectValueSub = 0f;
        [SerializeField] MapInteractable.EGimmickType _gimmickType = MapInteractable.EGimmickType.Bumper;

        public int Price => _price;
        public EItemKind Kind => _kind;
        public EEffectType EffectType => _effectType;
        public float EffectValue => _effectValue;
        public float EffectValueSub => _effectValueSub;
        public MapInteractable.EGimmickType GimmickType => _gimmickType;

        public string GetDisplayName()
        {
            return _displayName;
        }

        public string GetDescription()
        {
            return _description;
        }
    }
}
