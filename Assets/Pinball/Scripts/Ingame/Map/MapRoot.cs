using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class MapRoot : BehaviourBase
    {
        [SerializeField] MapInteractable[] _interactables = null;

        public void Init(Action<int> onScoreGained, Func<float, float> damageEvaluator)
        {
            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Init(onScoreGained, damageEvaluator);
        }

        public void Release()
        {
            if (null == _interactables)
                return;

            for (int i = 0; i < _interactables.Length; ++i)
                _interactables[i].Release();
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _interactables = GetComponentsInChildren<MapInteractable>(true);
        }
#endif // UNITY_EDITOR
    }
}
