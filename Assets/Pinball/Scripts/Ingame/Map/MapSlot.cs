using Pinball.Ingame.Stat;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapSlot : BehaviourBase
    {
        [SerializeField] MapInteractable _defaultInteractable = null;

        MapInteractable _spawnedInteractable = null;

        public MapInteractable GetDefaultInteractable()
        {
            return _defaultInteractable;
        }

        public MapInteractable Resolve(MapSlotReplacementData replacementData)
        {
            Clear();

            BumperInteractable bumper = _defaultInteractable as BumperInteractable;
            if (null == bumper || null == replacementData)
                return _defaultInteractable;

            if (!replacementData.TryGetRule(bumper.GetGrade(), out MapSlotReplacementData.ReplacementRule rule))
                return _defaultInteractable;

            if (!StatProvider.HasEffect(rule.EffectType))
                return _defaultInteractable;

            SaucerInteractable saucerPrefab = replacementData.GetSaucerPrefab();
            if (null == saucerPrefab)
                return _defaultInteractable;

            _defaultInteractable.gameObject.SetActive(false);
            SaucerInteractable spawned = Instantiate(saucerPrefab, transform);
            spawned.transform.localPosition = Vector3.zero;
            spawned.SetGrade(rule.TargetSaucerGrade);
            _spawnedInteractable = spawned;
            return _spawnedInteractable;
        }

        public void Clear()
        {
            if (null != _spawnedInteractable)
            {
                Destroy(_spawnedInteractable.gameObject);
                _spawnedInteractable = null;
            }

            if (null != _defaultInteractable)
                _defaultInteractable.gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _defaultInteractable = GetComponentInChildren<MapInteractable>(true);
        }
#endif // UNITY_EDITOR
    }
}
