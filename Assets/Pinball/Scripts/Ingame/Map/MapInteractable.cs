using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class MapInteractable : BehaviourBase
    {
        [SerializeField] LayerMask _ballLayerMask = default;

        Action<int> _onScoreGained = null;
        Func<float, float> _damageEvaluator = null;

        public void Init(Action<int> onScoreGained, Func<float, float> damageEvaluator)
        {
            _onScoreGained = onScoreGained;
            _damageEvaluator = damageEvaluator;
            OnInit();
        }

        public void Release()
        {
            OnRelease();
            _onScoreGained = null;
            _damageEvaluator = null;
        }

        protected virtual void OnInit() { }

        protected virtual void OnRelease() { }

        protected bool IsBallLayer(int layer)
        {
            return _ballLayerMask.HasLayer(layer);
        }

        protected float EvaluateDamage(float collisionImpulse)
        {
            if (null == _damageEvaluator)
                return 0f;

            return _damageEvaluator(collisionImpulse);
        }

        protected void GainScore(int score)
        {
            if (score <= 0)
                return;

            _onScoreGained?.Invoke(score);
        }
    }
}
