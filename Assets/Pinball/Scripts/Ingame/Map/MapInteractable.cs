using System;
using Pinball.FX;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class MapInteractable : BehaviourBase
    {
        [SerializeField] LayerMask _ballLayerMask = default;

        Action<int, Vector2> _onScoreGained = null;
        Func<float, float> _damageEvaluator = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;

        public void Init(Action<int, Vector2> onScoreGained, Func<float, float> damageEvaluator, Action<PoolableFX.EFXType, Vector2> fxPlayer)
        {
            _onScoreGained = onScoreGained;
            _damageEvaluator = damageEvaluator;
            _fxPlayer = fxPlayer;
            OnInit();
        }

        public void Release()
        {
            OnRelease();
            _onScoreGained = null;
            _damageEvaluator = null;
            _fxPlayer = null;
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
            GainScore(score, transform.position);
        }

        protected void GainScore(int score, Vector2 worldPoint)
        {
            if (score <= 0)
                return;

            _onScoreGained?.Invoke(score, worldPoint);
        }

        protected void PlayFX(PoolableFX.EFXType fxType, Vector2 worldPoint)
        {
            if (fxType == PoolableFX.EFXType.NonePooling)
                return;

            _fxPlayer?.Invoke(fxType, worldPoint);
        }
    }
}
