using System;
using Pinball.Ingame.Shop;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    [CreateAssetMenu(fileName = "MapSlotReplacementData", menuName = "Pinball/Ingame/MapSlotReplacementData")]
    public class MapSlotReplacementData : ScriptableObject
    {
        [Serializable]
        public struct ReplacementRule
        {
            public ShopItemData.EEffectType EffectType;
            public BumperGradeData.EGrade SourceBumperGrade;
            public SaucerGradeData.EGrade TargetSaucerGrade;
        }

        [SerializeField] SaucerInteractable _saucerPrefab = null;
        [SerializeField] ReplacementRule[] _rules = null;

        public SaucerInteractable GetSaucerPrefab()
        {
            return _saucerPrefab;
        }

        public bool TryGetRule(BumperGradeData.EGrade sourceGrade, out ReplacementRule rule)
        {
            rule = default;
            if (null == _rules)
                return false;

            for (int i = 0; i < _rules.Length; ++i)
            {
                if (_rules[i].SourceBumperGrade != sourceGrade)
                    continue;

                rule = _rules[i];
                return true;
            }

            return false;
        }
    }
}
