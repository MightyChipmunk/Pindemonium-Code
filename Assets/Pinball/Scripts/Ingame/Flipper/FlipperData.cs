using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "FlipperData", menuName = "Pinball/Ingame/FlipperData")]
    public class FlipperData : ScriptableObject
    {
        public enum EAbilityType
        {
            None = 0,
            PerfectImpulseScale = 1,
        }

        [SerializeField] string _nameKey = string.Empty;
        [SerializeField] string _abilityKey = string.Empty;
        [SerializeField] Sprite _icon = null;
        [SerializeField] EAbilityType _abilityType = EAbilityType.None;
        [SerializeField] float _abilityValue = 0f;

        public EAbilityType AbilityType => _abilityType;
        public float AbilityValue => _abilityValue;

        public string GetId()
        {
            return name;
        }

        public string GetDisplayName()
        {
            return LocalizationTable.Get(_nameKey, _nameKey);
        }

        public string GetAbilityDescription()
        {
            return LocalizationTable.Get(_abilityKey, _abilityKey);
        }

        public Sprite GetIcon()
        {
            return _icon;
        }
    }
}
