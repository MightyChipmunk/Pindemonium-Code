using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "CardPackData", menuName = "Pinball/Ingame/CardPackData")]
    public class CardPackData : ScriptableObject
    {
        public enum EPackKind
        {
            Passive = 0,
            Upgrade = 1,
            Random = 2,
        }

        const int MIN_REVEAL_COUNT = 1;
        const int MIN_PICK_COUNT = 1;

        [SerializeField] string _nameKey = string.Empty;
        [SerializeField] string _descriptionKey = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] Sprite _bandSprite = null;
        [SerializeField] int _price = 4;
        [SerializeField] EPackKind _kind = EPackKind.Random;
        [SerializeField] int _revealCount = 3;
        [SerializeField] int _pickCount = 1;

        public int Price => _price;
        public Sprite BandSprite => _bandSprite;
        public int RevealCount => Mathf.Max(MIN_REVEAL_COUNT, _revealCount);
        public int PickCount => Mathf.Clamp(_pickCount, MIN_PICK_COUNT, RevealCount);

        public string GetDisplayName()
        {
            return LocalizationTable.Get(_nameKey, _nameKey);
        }

        public string GetDescription()
        {
            return LocalizationTable.Get(_descriptionKey, _descriptionKey);
        }

        public Sprite GetIcon()
        {
            return _icon;
        }

        public bool Accepts(ShopItemData item)
        {
            if (null == item)
                return false;

            if (_kind == EPackKind.Random)
                return true;

            if (_kind == EPackKind.Passive)
                return item.Kind == ShopItemData.EItemKind.Passive;

            return item.Kind == ShopItemData.EItemKind.Upgrade;
        }
    }
}
