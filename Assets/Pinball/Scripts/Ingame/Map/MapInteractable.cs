using System;
using Pinball.Audio;
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
            Saucer = 7,
        }

        public enum EScoreSource
        {
            BallHit = 0,
            PassiveEffect = 1,
        }

        public record InitParam(
            Action<EGimmickType, int, Vector2, EScoreSource> OnScoreGained,
            Func<Collider2D, float, float> DamageEvaluator,
            Action<PoolableFX.EFXType, Vector2> FXPlayer,
            Action<Collider2D> OnBallHit,
            Action GimmickHitPresenter,
            int ScoreGainLimit);

        public const int NO_SCORE_GAIN_LIMIT = 0;

        [SerializeField] LayerMask _ballLayerMask = default;

        Action<EGimmickType, int, Vector2, EScoreSource> _onScoreGained = null;
        Func<Collider2D, float, float> _damageEvaluator = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;
        Action<Collider2D> _onBallHit = null;
        Action _gimmickHitPresenter = null;
        int _scoreGainLimit = NO_SCORE_GAIN_LIMIT;
        int _scoreGainCount = 0;
        bool _isDeactivated = false;

        public abstract EGimmickType GimmickType { get; }
        public virtual bool IsDeactivatable => true;
        public bool IsDeactivated => _isDeactivated;

        public void Init(InitParam param)
        {
            _onScoreGained = param.OnScoreGained;
            _damageEvaluator = param.DamageEvaluator;
            _fxPlayer = param.FXPlayer;
            _onBallHit = param.OnBallHit;
            _gimmickHitPresenter = param.GimmickHitPresenter;
            _scoreGainLimit = param.ScoreGainLimit;
            _scoreGainCount = 0;
            _isDeactivated = false;
            OnInit();
        }

        public void Release()
        {
            OnRelease();
            _onScoreGained = null;
            _damageEvaluator = null;
            _fxPlayer = null;
            _onBallHit = null;
            _gimmickHitPresenter = null;
        }

        public void Deactivate()
        {
            if (!IsDeactivatable)
                return;

            if (_isDeactivated)
                return;

            _isDeactivated = true;
            OnDeactivated();
        }

        protected virtual void OnInit() { }

        protected virtual void OnRelease() { }

        protected virtual void OnDeactivated()
        {
            gameObject.SetActive(false);
        }

        protected bool IsBallLayer(int layer)
        {
            return _ballLayerMask.HasLayer(layer);
        }

        protected float EvaluateDamage(Collider2D ballCollider, float collisionImpulse)
        {
            if (null == _damageEvaluator)
                return 0f;

            return _damageEvaluator(ballCollider, collisionImpulse);
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
            if (_isDeactivated)
                return;

            if (score <= 0)
                return;

            _onScoreGained?.Invoke(GimmickType, score, worldPoint, scoreSource);
            CountScoreGain(scoreSource);
        }

        protected void NotifyBallHit(Collider2D ballCollider)
        {
            _onBallHit?.Invoke(ballCollider);
        }

        protected void PlayGimmickHit()
        {
            if (_isDeactivated)
                return;

            AudioManager.Play(GetHitClipType());
            _gimmickHitPresenter?.Invoke();
        }

        protected void PlayBankClearedSound()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.GimmickBankCleared);
        }

        AudioStorage.EAudioClipType GetHitClipType()
        {
            switch (GimmickType)
            {
                case EGimmickType.Bumper:
                    return AudioStorage.EAudioClipType.GimmickHitBumper;
                case EGimmickType.Slingshot:
                    return AudioStorage.EAudioClipType.GimmickHitSlingshot;
                case EGimmickType.Spinner:
                    return AudioStorage.EAudioClipType.GimmickHitSpinner;
                case EGimmickType.DropTarget:
                    return AudioStorage.EAudioClipType.GimmickHitDropTarget;
                case EGimmickType.StandupTarget:
                    return AudioStorage.EAudioClipType.GimmickHitStandupTarget;
                case EGimmickType.LampBank:
                    return AudioStorage.EAudioClipType.GimmickHitLampBank;
                case EGimmickType.Lane:
                    return AudioStorage.EAudioClipType.GimmickHitLane;
                case EGimmickType.Saucer:
                    return AudioStorage.EAudioClipType.GimmickSaucerCapture;
                default:
                    return AudioStorage.EAudioClipType.None;
            }
        }

        protected void PlayFX(PoolableFX.EFXType fxType, Vector2 worldPoint)
        {
            if (fxType == PoolableFX.EFXType.NonePooling)
                return;

            _fxPlayer?.Invoke(fxType, worldPoint);
        }

        void CountScoreGain(EScoreSource scoreSource)
        {
            if (scoreSource != EScoreSource.BallHit)
                return;

            if (_scoreGainLimit <= NO_SCORE_GAIN_LIMIT)
                return;

            ++_scoreGainCount;
            if (_scoreGainCount < _scoreGainLimit)
                return;

            Deactivate();
        }
    }
}
