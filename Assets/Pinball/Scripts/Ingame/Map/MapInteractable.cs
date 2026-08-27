using System;
using System.Collections.Generic;
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
            GimmickBreak = 2,
        }

        public record InitParam(
            Action<EGimmickType, int, Vector2, EScoreSource> OnScoreGained,
            Func<Collider2D, float, float> DamageEvaluator,
            Func<Collider2D, float> AttackPowerEvaluator,
            Action<PoolableFX.EFXType, Vector2> FXPlayer,
            Action<Collider2D> OnBallHit,
            Action GimmickHitPresenter,
            int ScoreGainLimit);

        public record LineSnapshot(
            Vector3[] Points,
            float Width,
            Color Color,
            bool HasRoundCorner,
            bool HasRoundCap)
        {
            const int MIN_POINT_COUNT = 2;
            const int NO_ROUND_VERTEX = 0;

            public static bool TryCreate(Renderer renderer, out LineSnapshot snapshot)
            {
                snapshot = null;
                if (null == renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    return false;

                LineRenderer lineRenderer = renderer as LineRenderer;
                if (null == lineRenderer || lineRenderer.positionCount < MIN_POINT_COUNT)
                    return false;

                Vector3[] points = new Vector3[lineRenderer.positionCount];
                lineRenderer.GetPositions(points);
                float width = lineRenderer.startWidth * lineRenderer.widthMultiplier;
                if (!lineRenderer.useWorldSpace)
                {
                    Transform rendererTransform = lineRenderer.transform;
                    for (int i = 0; i < points.Length; ++i)
                        points[i] = rendererTransform.TransformPoint(points[i]);

                    width *= Mathf.Abs(rendererTransform.lossyScale.x);
                }

                snapshot = new LineSnapshot(points, width, lineRenderer.startColor, lineRenderer.numCornerVertices > NO_ROUND_VERTEX, lineRenderer.numCapVertices > NO_ROUND_VERTEX);
                return true;
            }
        }

        public record SpriteSnapshot(
            Sprite Sprite,
            Vector3 Position,
            Quaternion Rotation,
            Vector3 LossyScale,
            Color Color,
            bool FlipX,
            bool FlipY)
        {
            public static bool TryCreate(Renderer renderer, out SpriteSnapshot snapshot)
            {
                snapshot = null;
                if (null == renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    return false;

                SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
                if (null == spriteRenderer || null == spriteRenderer.sprite)
                    return false;

                Transform rendererTransform = renderer.transform;
                snapshot = new SpriteSnapshot(
                    Sprite: spriteRenderer.sprite,
                    Position: rendererTransform.position,
                    Rotation: rendererTransform.rotation,
                    LossyScale: rendererTransform.lossyScale,
                    Color: spriteRenderer.color,
                    FlipX: spriteRenderer.flipX,
                    FlipY: spriteRenderer.flipY);
                return true;
            }
        }

        public const int NO_SCORE_GAIN_LIMIT = 0;

        [SerializeField] LayerMask _ballLayerMask = default;
        [SerializeField] Renderer[] _boundsRenderers = null;
        [SerializeField] Collider2D[] _damageColliders = null;

        Action<EGimmickType, int, Vector2, EScoreSource> _onScoreGained = null;
        Func<Collider2D, float, float> _damageEvaluator = null;
        Func<Collider2D, float> _attackPowerEvaluator = null;
        Action<PoolableFX.EFXType, Vector2> _fxPlayer = null;
        Action<Collider2D> _onBallHit = null;
        Action _gimmickHitPresenter = null;
        int _scoreGainLimit = NO_SCORE_GAIN_LIMIT;
        int _scoreGainCount = 0;
        bool _isScoreGainBlocked = false;
        bool _isDeactivated = false;

        public abstract EGimmickType GimmickType { get; }
        public virtual bool IsDeactivatable => true;
        public bool IsDeactivated => _isDeactivated;

        public void Init(InitParam param)
        {
            _onScoreGained = param.OnScoreGained;
            _damageEvaluator = param.DamageEvaluator;
            _attackPowerEvaluator = param.AttackPowerEvaluator;
            _fxPlayer = param.FXPlayer;
            _onBallHit = param.OnBallHit;
            _gimmickHitPresenter = param.GimmickHitPresenter;
            _scoreGainLimit = param.ScoreGainLimit;
            _scoreGainCount = 0;
            _isScoreGainBlocked = false;
            _isDeactivated = false;
            OnInit();
        }

        public void Release()
        {
            OnRelease();
            _onScoreGained = null;
            _damageEvaluator = null;
            _attackPowerEvaluator = null;
            _fxPlayer = null;
            _onBallHit = null;
            _gimmickHitPresenter = null;
        }

        public bool TryGetWorldBounds(out Bounds worldBounds)
        {
            worldBounds = default;
            if (null == _boundsRenderers)
                return false;

            bool hasBounds = false;
            for (int i = 0; i < _boundsRenderers.Length; ++i)
            {
                Renderer boundsRenderer = _boundsRenderers[i];
                if (null == boundsRenderer || !boundsRenderer.enabled || !boundsRenderer.gameObject.activeInHierarchy)
                    continue;

                if (hasBounds)
                {
                    worldBounds.Encapsulate(boundsRenderer.bounds);
                }
                else
                {
                    worldBounds = boundsRenderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        public void CollectHighlightSprites(List<SpriteSnapshot> buffer)
        {
            if (null == _boundsRenderers)
                return;

            for (int i = 0; i < _boundsRenderers.Length; ++i)
                if (SpriteSnapshot.TryCreate(_boundsRenderers[i], out SpriteSnapshot snapshot))
                    buffer.Add(snapshot);
        }

        public void CollectHighlightLines(List<LineSnapshot> buffer)
        {
            if (null == _boundsRenderers)
                return;

            for (int i = 0; i < _boundsRenderers.Length; ++i)
                if (LineSnapshot.TryCreate(_boundsRenderers[i], out LineSnapshot snapshot))
                    buffer.Add(snapshot);
        }

        public void CollectDamageColliders(List<Collider2D> buffer)
        {
            if (null == _damageColliders)
                return;

            for (int i = 0; i < _damageColliders.Length; ++i)
                if (null != _damageColliders[i])
                    buffer.Add(_damageColliders[i]);
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

        protected float EvaluateAttackPower(Collider2D ballCollider)
        {
            if (null == _attackPowerEvaluator)
                return 0f;

            return _attackPowerEvaluator(ballCollider);
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

            if (_isScoreGainBlocked)
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
            PlayGimmickHitPresentation();
        }

        protected void PlayGimmickHitPresentation()
        {
            if (_isDeactivated)
                return;

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
            if (scoreSource == EScoreSource.PassiveEffect)
                return;

            if (_scoreGainLimit <= NO_SCORE_GAIN_LIMIT)
                return;

            ++_scoreGainCount;
            if (_scoreGainCount < _scoreGainLimit)
                return;

            _isScoreGainBlocked = true;
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _boundsRenderers = GetComponentsInChildren<Renderer>(true);
            _damageColliders = this is IDamageable ? GetComponentsInChildren<Collider2D>(true) : null;
        }
#endif // UNITY_EDITOR
    }
}
