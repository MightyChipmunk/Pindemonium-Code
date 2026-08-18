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

        const string KEY_PREFIX = "flipper.";
        const string NAME_KEY_SUFFIX = ".name";
        const string ABILITY_KEY_SUFFIX = ".ability";

        [SerializeField] string _displayName = string.Empty;
        [SerializeField, TextArea] string _abilityDescription = string.Empty;
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
            return LocalizationTable.Get(KEY_PREFIX + GetId() + NAME_KEY_SUFFIX, _displayName);
        }

        public string GetAbilityDescription()
        {
            return LocalizationTable.Get(KEY_PREFIX + GetId() + ABILITY_KEY_SUFFIX, _abilityDescription);
        }

        public Sprite GetIcon()
        {
            return _icon;
        }
    }
}
