using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame.Shop
{
    [CreateAssetMenu(fileName = "VoucherData", menuName = "Pinball/Ingame/VoucherData")]
    public class VoucherData : ScriptableObject
    {
        public enum EEffect
        {
            BallResupplyMoney = 0,
            PassiveSlotAdd = 1,
            TopUpgradeDoubleBuy = 2,
            BallAdd = 3,
            InterestRateBoost = 4,
            InterestCapBoost = 5,
            ShopDiscount = 6,
            ShopFirstRerollFree = 7,
            StageFirstRerollFree = 8,
            BonusUpgradeOnBuy = 9,
            PassiveSellPriceBoost = 10,
        }

        public const int MAIN_VALUE_INDEX = 0;

        [SerializeField] string _nameKey = string.Empty;
        [SerializeField] string _descriptionKey = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] EEffect _effect = EEffect.BallResupplyMoney;
        [SerializeField] float[] _effectValueArr = null;

        object[] _textArgArr = null;

        public EEffect Effect => _effect;

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
            return GetNameRef().Resolve();
        }

        public LocalizedTextRef GetNameRef()
        {
            return new LocalizedTextRef(_nameKey, GetTextArgArr());
        }

        public LocalizedTextRef GetDescriptionRef()
        {
            return new LocalizedTextRef(_descriptionKey, GetTextArgArr());
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
