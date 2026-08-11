using System;
using Pinball.FX;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public abstract class MapInteractable : BehaviourBase
    {
        public enum EGimmickType
        {
            Bumper = 0,
            Slingshot = 1,
            Spinner = 2,
            DropTarget = 3,
            StandupTarget = 4,
            LampBank = 5,
            Lane = 6,
        }

        public enum EScoreSource
        {
            BallHit = 0,
            PassiveEffect = 1,
        }

        [SerializeField] LayerMask _ballLayerMask = default;

        Action<EGimmickType, int, Vector2, EScoreSource> _onScoreGained = null;
        Func<float, float> _damageEvaluator = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;

        public abstract EGimmickType GimmickType { get; }

        public void Init(Action<EGimmickType, int, Vector2, EScoreSource> onScoreGained, Func<float, float> damageEvaluator, Action<PoolableFX.EFXType, Vector2> fxPlayer)
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
            GainScore(score, transform.position, EScoreSource.BallHit);
        }

        protected void GainScore(int score, Vector2 worldPoint)
        {
            GainScore(score, worldPoint, EScoreSource.BallHit);
        }

        protected void GainScore(int score, Vector2 worldPoint, EScoreSource scoreSource)
        {
            if (score <= 0)
                return;

            _onScoreGained?.Invoke(GimmickType, score, worldPoint, scoreSource);
        }

        protected void PlayFX(PoolableFX.EFXType fxType, Vector2 worldPoint)
        {
            if (fxType == PoolableFX.EFXType.NonePooling)
                return;

            _fxPlayer?.Invoke(fxType, worldPoint);
        }
    }
}
