using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SaucerInteractable : MapInteractable
    {
        const float CAPTURE_PUNCH_RATIO = 0.4f;
        const float CAPTURE_PUNCH_DURATION = 0.13f;
        const int CAPTURE_PUNCH_VIBRATO = 8;
        const float HOLD_PULSE_RATIO = 0.14f;
        const float HOLD_PULSE_DURATION = 0.2f;
        const float EJECT_PUNCH_RATIO = 0.3f;
        const float EJECT_PUNCH_DURATION = 0.1f;
        const int EJECT_PUNCH_VIBRATO = 6;
        const float SPIN_STOP_LEAD_TIME = 0.3f;
        const float SPIN_BASE_INTERVAL = 0.0275f;
        const float SPIN_SLOWDOWN_RATIO = 3.5f;
        const float MIN_SPIN_TIME = 0.2f;
        const float MIN_DIRECTION_SQR = 0.0001f;
        const int NO_LIT_INDEX = -1;
        const float INDICATOR_STAGGER_INTERVAL = 0.035f;
        const float NO_COOLDOWN = 0f;
        const float SPIN_TICK_MIN_PITCH = 0.85f;
        const float SPIN_TICK_MAX_PITCH = 1.6f;
        const float SPIN_TICK_END_PROGRESS = 1f;
        const int NO_BONUS_INDEX = -1;
        const int MIN_BONUS_INDICATOR_COUNT = 3;
        const float NO_BONUS_MULTIPLIER = 1f;
        const float STRONG_TARGET_DIM_ALPHA_RATIO = 3f;
        const float NO_CAPTURE_RADIUS = 0f;
        const float WEAK_TARGET_DIM_ALPHA_RATIO = 2f;
        const int NO_HELD_COLLIDER_ID = 0;

        static readonly Color STRONG_TARGET_TINT = new Color(0.45f, 1f, 0.5f, 1f);
        static readonly Color WEAK_TARGET_TINT = new Color(0.42f, 0.78f, 0.45f, 1f);

        [SerializeField] SaucerGradeData _gradeData = null;
        [SerializeField] SaucerGradeData.EGrade _grade = SaucerGradeData.EGrade.Normal;
        [SerializeField] CircleCollider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] Lamp[] _indicators = null;
        [SerializeField] int _scoreReward = 0;
        [SerializeField] float _holdDuration = 0.75f;
        [SerializeField] float _cooldownDuration = 3f;
        [SerializeField] float _ejectSpeed = 14f;
        [SerializeField] EFXType _captureFX = EFXType.NonePooling;

        CancellationTokenSource _cancellationTokenSource = null;
        Rigidbody2D _heldBody = null;
        Tween _visualTween = null;
        SaucerGradeData.GradeSpec _spec = default;
        Vector3 _visualBaseScale = Vector3.one;
        float _baseCaptureRadius = NO_CAPTURE_RADIUS;
        float[] _spinIntervals = null;
        int _litIndex = NO_LIT_INDEX;
        int _bonusCenterIndex = NO_BONUS_INDEX;
        int _heldColliderInstanceId = NO_HELD_COLLIDER_ID;
        int _holdSequence = 0;

        public override EGimmickType GimmickType => EGimmickType.Saucer;

        public void SetGrade(SaucerGradeData.EGrade grade)
        {
            _grade = grade;
        }

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _heldBody = null;
            _heldColliderInstanceId = NO_HELD_COLLIDER_ID;
            _litIndex = NO_LIT_INDEX;

            if (null != _gradeData)
            {
                _spec = _gradeData.GetSpec(_grade);
                transform.localScale = Vector3.one * _spec.RootScale;
            }

            if (null != _visual)
                _visualBaseScale = _visual.localScale;

            ApplyCaptureRadius();
            InitIndicators();
            BuildSpinIntervals();
            RefreshBonusTargets();
            _collider.enabled = true;
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            KillVisualTween();
            ReleaseHeldBody(transform.up);
            ReleaseIndicators();
            _spinIntervals = null;
        }

        protected override void OnDeactivated()
        {
            ReleaseHeldBody(transform.up);
            base.OnDeactivated();
        }

        void ApplyCaptureRadius()
        {
            if (null == _collider)
                return;

            if (_baseCaptureRadius <= NO_CAPTURE_RADIUS)
                _baseCaptureRadius = _collider.radius;

            _collider.radius = _baseCaptureRadius * StatProvider.GetSaucerCaptureScale();
        }

        void InitIndicators()
        {
            if (null == _indicators)
                return;

            for (int i = 0; i < _indicators.Length; ++i)
                _indicators[i].Init();
        }

        void ReleaseIndicators()
        {
            if (null == _indicators)
                return;

            for (int i = 0; i < _indicators.Length; ++i)
                _indicators[i].Release();

            _litIndex = NO_LIT_INDEX;
            _bonusCenterIndex = NO_BONUS_INDEX;
        }

        void BuildSpinIntervals()
        {
            if (null == _indicators || _indicators.Length == 0)
            {
                _spinIntervals = null;
                return;
            }

            float spinTime = Mathf.Max(GetHoldDuration() - SPIN_STOP_LEAD_TIME, MIN_SPIN_TIME);
            int stepCount = Mathf.Max(_indicators.Length, Mathf.RoundToInt(spinTime / SPIN_BASE_INTERVAL));
            _spinIntervals = new float[stepCount];

            float weightSum = 0f;
            for (int i = 0; i < stepCount; ++i)
            {
                float ratio = stepCount <= 1 ? 0f : (float)i / (stepCount - 1);
                _spinIntervals[i] = Mathf.Lerp(1f, SPIN_SLOWDOWN_RATIO, ratio * ratio);
                weightSum += _spinIntervals[i];
            }

            float scale = spinTime / weightSum;
            for (int i = 0; i < stepCount; ++i)
                _spinIntervals[i] *= scale;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (null == _cancellationTokenSource)
                return;

            if (null != _heldBody)
                return;

            if (!IsBallLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body)
                return;

            if (body.bodyType != RigidbodyType2D.Dynamic)
                return;

            HoldAsync(body, other, _cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid HoldAsync(Rigidbody2D body, Collider2D ballCollider, CancellationToken cancellationToken)
        {
            int holdSequence = ++_holdSequence;
            _heldBody = body;
            _heldColliderInstanceId = ballCollider.GetInstanceID();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.MovePosition(transform.position);

            NotifyBallHit(ballCollider);
            if (holdSequence != _holdSequence)
                return;

            PlayFX(_captureFX, transform.position);
            PlayGimmickHit();
            PlayCaptureMotion();

            int targetIndex = PickTargetIndex();
            await SpinIndicatorsAsync(holdSequence, targetIndex, cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(SPIN_STOP_LEAD_TIME), cancellationToken: cancellationToken);

            if (holdSequence != _holdSequence)
                return;

            int reward = Mathf.RoundToInt(GetScoreReward() * GetTargetBonusMultiplier(targetIndex));
            ReleaseHeldBody(GetEjectDirection(targetIndex));
            SetLitIndex(NO_LIT_INDEX);
            PlayEjectMotion();
            AudioManager.Play(AudioStorage.EAudioClipType.GimmickSaucerEject);
            GainScore(reward);

            await CooldownAsync(cancellationToken);
        }

        public void CancelBallHold(int colliderInstanceId)
        {
            if (null == _heldBody)
                return;

            if (_heldColliderInstanceId != colliderInstanceId)
                return;

            ++_holdSequence;
            _heldBody = null;
            _heldColliderInstanceId = NO_HELD_COLLIDER_ID;
            KillVisualTween();
            ResetVisualScale();
            SetLitIndex(NO_LIT_INDEX);
        }

        async UniTask CooldownAsync(CancellationToken cancellationToken)
        {
            float cooldownDuration = GetCooldownDuration();
            if (cooldownDuration <= NO_COOLDOWN)
                return;

            _collider.enabled = false;
            PlayIndicatorsHide();
            await UniTask.Delay(TimeSpan.FromSeconds(cooldownDuration), cancellationToken: cancellationToken);
            RefreshBonusTargets();
            PlayIndicatorsShow();
            _collider.enabled = true;
        }

        void PlayIndicatorsHide()
        {
            if (null == _indicators)
                return;

            for (int i = 0; i < _indicators.Length; ++i)
                _indicators[i].PlayHide(i * INDICATOR_STAGGER_INTERVAL);
        }

        void PlayIndicatorsShow()
        {
            if (null == _indicators)
                return;

            for (int i = 0; i < _indicators.Length; ++i)
                _indicators[i].PlayShow(i * INDICATOR_STAGGER_INTERVAL);
        }

        void RefreshBonusTargets()
        {
            _bonusCenterIndex = NO_BONUS_INDEX;

            if (null == _indicators || _indicators.Length < MIN_BONUS_INDICATOR_COUNT)
                return;

            if (StatProvider.HasSaucerTargetBonus())
                _bonusCenterIndex = UnityEngine.Random.Range(0, _indicators.Length);

            ApplyBonusTints();
        }

        void ApplyBonusTints()
        {
            for (int i = 0; i < _indicators.Length; ++i)
            {
                if (i == _bonusCenterIndex)
                {
                    _indicators[i].SetTint(STRONG_TARGET_TINT, STRONG_TARGET_DIM_ALPHA_RATIO);
                    continue;
                }

                if (IsWeakTargetIndex(i))
                {
                    _indicators[i].SetTint(WEAK_TARGET_TINT, WEAK_TARGET_DIM_ALPHA_RATIO);
                    continue;
                }

                _indicators[i].ClearTint();
            }
        }

        bool IsWeakTargetIndex(int index)
        {
            if (_bonusCenterIndex == NO_BONUS_INDEX)
                return false;

            int count = _indicators.Length;
            return index == (_bonusCenterIndex + 1) % count || index == (_bonusCenterIndex - 1 + count) % count;
        }

        float GetTargetBonusMultiplier(int targetIndex)
        {
            if (_bonusCenterIndex == NO_BONUS_INDEX || targetIndex == NO_LIT_INDEX)
                return NO_BONUS_MULTIPLIER;

            if (targetIndex == _bonusCenterIndex)
                return StatProvider.GetSaucerTargetBonus(true);

            if (IsWeakTargetIndex(targetIndex))
                return StatProvider.GetSaucerTargetBonus(false);

            return NO_BONUS_MULTIPLIER;
        }

        int PickTargetIndex()
        {
            if (null == _indicators || _indicators.Length == 0)
                return NO_LIT_INDEX;

            return UnityEngine.Random.Range(0, _indicators.Length);
        }

        async UniTask SpinIndicatorsAsync(int holdSequence, int targetIndex, CancellationToken cancellationToken)
        {
            if (null == _spinIntervals || targetIndex == NO_LIT_INDEX)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(GetHoldDuration() - SPIN_STOP_LEAD_TIME, MIN_SPIN_TIME)), cancellationToken: cancellationToken);
                return;
            }

            int count = _indicators.Length;
            int stepCount = _spinIntervals.Length;
            int startIndex = ((targetIndex - (stepCount - 1)) % count + count) % count;

            for (int i = 0; i < stepCount; ++i)
            {
                if (holdSequence != _holdSequence)
                    return;

                SetLitIndex((startIndex + i) % count);
                PlaySpinTick(stepCount <= 1 ? SPIN_TICK_END_PROGRESS : (float)i / (stepCount - 1));
                await UniTask.Delay(TimeSpan.FromSeconds(_spinIntervals[i]), cancellationToken: cancellationToken);
            }
        }

        static void PlaySpinTick(float progress)
        {
            AudioManager.PlaySequenced(AudioStorage.EAudioClipType.GimmickSaucerSpinTick, Mathf.Lerp(SPIN_TICK_MIN_PITCH, SPIN_TICK_MAX_PITCH, progress));
        }

        void SetLitIndex(int index)
        {
            if (null == _indicators)
                return;

            if (_litIndex != NO_LIT_INDEX)
                _indicators[_litIndex].SetLit(false);

            if (index != NO_LIT_INDEX)
                _indicators[index].SetLit(true);

            _litIndex = index;
        }

        Vector2 GetEjectDirection(int index)
        {
            if (null == _indicators || index == NO_LIT_INDEX)
                return transform.up;

            Vector2 offset = (Vector2)_indicators[index].transform.position - (Vector2)transform.position;
            if (offset.sqrMagnitude <= MIN_DIRECTION_SQR)
                return transform.up;

            return offset.normalized;
        }

        int GetScoreReward()
        {
            return null == _gradeData ? _scoreReward : _spec.ScoreReward;
        }

        float GetHoldDuration()
        {
            return null == _gradeData ? _holdDuration : _spec.HoldDuration;
        }

        float GetCooldownDuration()
        {
            float baseDuration = null == _gradeData ? _cooldownDuration : _spec.CooldownDuration;
            return baseDuration * StatProvider.GetSaucerCooldownScale();
        }

        float GetEjectSpeed()
        {
            return null == _gradeData ? _ejectSpeed : _spec.EjectSpeed;
        }

        void ReleaseHeldBody(Vector2 ejectDirection)
        {
            if (null == _heldBody)
                return;

            ++_holdSequence;
            Rigidbody2D body = _heldBody;
            _heldBody = null;
            _heldColliderInstanceId = NO_HELD_COLLIDER_ID;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = ejectDirection * GetEjectSpeed();
        }

        void PlayCaptureMotion()
        {
            if (null == _visual)
                return;

            KillVisualTween();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_visual.DOPunchScale(_visualBaseScale * CAPTURE_PUNCH_RATIO, CAPTURE_PUNCH_DURATION, CAPTURE_PUNCH_VIBRATO));
            sequence.AppendCallback(PlayHoldPulse);
            _visualTween = sequence;
        }

        void PlayHoldPulse()
        {
            _visualTween = _visual.DOScale(_visualBaseScale * (1f + HOLD_PULSE_RATIO), HOLD_PULSE_DURATION).SetLoops(-1, LoopType.Yoyo);
        }

        void PlayEjectMotion()
        {
            if (null == _visual)
                return;

            KillVisualTween();
            _visual.localScale = _visualBaseScale;
            _visualTween = _visual.DOPunchScale(_visualBaseScale * EJECT_PUNCH_RATIO, EJECT_PUNCH_DURATION, EJECT_PUNCH_VIBRATO);
        }

        void ResetVisualScale()
        {
            if (null == _visual)
                return;

            _visual.localScale = _visualBaseScale;
        }

        void KillVisualTween()
        {
            if (null == _visualTween)
                return;

            _visualTween.Kill();
            _visualTween = null;
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            base.OnBindSerializedField();
            _collider = GetComponent<CircleCollider2D>();
            _indicators = GetComponentsInChildren<Lamp>(true);

            SpriteRenderer visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (null != visualRenderer)
                _visual = visualRenderer.transform;
        }
#endif // UNITY_EDITOR
    }
}
