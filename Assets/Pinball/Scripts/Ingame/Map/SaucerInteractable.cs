using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.FX;
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
        const int MIN_REWARD_STEP = 0;
        const float SPIN_STOP_LEAD_TIME = 0.3f;
        const float SPIN_BASE_INTERVAL = 0.0275f;
        const float SPIN_SLOWDOWN_RATIO = 3.5f;
        const float MIN_SPIN_TIME = 0.2f;
        const float MIN_DIRECTION_SQR = 0.0001f;
        const int NO_LIT_INDEX = -1;
        const float INDICATOR_STAGGER_INTERVAL = 0.035f;
        const float NO_COOLDOWN = 0f;

        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] Lamp[] _indicators = null;
        [SerializeField] int _scoreReward = 0;
        [SerializeField] int _scoreRewardIncrement = 0;
        [SerializeField] int _maxRewardStep = 4;
        [SerializeField] float _holdDuration = 0.75f;
        [SerializeField] float _cooldownDuration = 3f;
        [SerializeField] float _ejectSpeed = 14f;
        [SerializeField] PoolableFX.EFXType _captureFX = PoolableFX.EFXType.NonePooling;

        CancellationTokenSource _cancellationTokenSource = null;
        Rigidbody2D _heldBody = null;
        Tween _visualTween = null;
        Vector3 _visualBaseScale = Vector3.one;
        float[] _spinIntervals = null;
        int _litIndex = NO_LIT_INDEX;
        int _captureCount = 0;

        public override EGimmickType GimmickType => EGimmickType.Saucer;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _heldBody = null;
            _captureCount = 0;
            _litIndex = NO_LIT_INDEX;

            if (null != _visual)
                _visualBaseScale = _visual.localScale;

            InitIndicators();
            BuildSpinIntervals();
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
        }

        void BuildSpinIntervals()
        {
            if (null == _indicators || _indicators.Length == 0)
            {
                _spinIntervals = null;
                return;
            }

            float spinTime = Mathf.Max(_holdDuration - SPIN_STOP_LEAD_TIME, MIN_SPIN_TIME);
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
            _heldBody = body;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.MovePosition(transform.position);

            NotifyBallHit(ballCollider);
            PlayFX(_captureFX, transform.position);
            PlayGimmickHitSound();
            PlayCaptureMotion();

            int targetIndex = PickTargetIndex();
            await SpinIndicatorsAsync(targetIndex, cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(SPIN_STOP_LEAD_TIME), cancellationToken: cancellationToken);

            int reward = GetCurrentReward();
            ReleaseHeldBody(GetEjectDirection(targetIndex));
            SetLitIndex(NO_LIT_INDEX);
            PlayEjectMotion();
            ++_captureCount;
            GainScore(reward);

            await CooldownAsync(cancellationToken);
        }

        async UniTask CooldownAsync(CancellationToken cancellationToken)
        {
            if (_cooldownDuration <= NO_COOLDOWN)
                return;

            _collider.enabled = false;
            PlayIndicatorsHide();
            await UniTask.Delay(TimeSpan.FromSeconds(_cooldownDuration), cancellationToken: cancellationToken);
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

        int PickTargetIndex()
        {
            if (null == _indicators || _indicators.Length == 0)
                return NO_LIT_INDEX;

            return UnityEngine.Random.Range(0, _indicators.Length);
        }

        async UniTask SpinIndicatorsAsync(int targetIndex, CancellationToken cancellationToken)
        {
            if (null == _spinIntervals || targetIndex == NO_LIT_INDEX)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(_holdDuration - SPIN_STOP_LEAD_TIME, MIN_SPIN_TIME)), cancellationToken: cancellationToken);
                return;
            }

            int count = _indicators.Length;
            int stepCount = _spinIntervals.Length;
            int startIndex = ((targetIndex - (stepCount - 1)) % count + count) % count;

            for (int i = 0; i < stepCount; ++i)
            {
                SetLitIndex((startIndex + i) % count);
                await UniTask.Delay(TimeSpan.FromSeconds(_spinIntervals[i]), cancellationToken: cancellationToken);
            }
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

        int GetCurrentReward()
        {
            int step = Mathf.Clamp(_captureCount, MIN_REWARD_STEP, _maxRewardStep);
            return _scoreReward + _scoreRewardIncrement * step;
        }

        void ReleaseHeldBody(Vector2 ejectDirection)
        {
            if (null == _heldBody)
                return;

            Rigidbody2D body = _heldBody;
            _heldBody = null;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = ejectDirection * _ejectSpeed;
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
            _collider = GetComponent<Collider2D>();
            _indicators = GetComponentsInChildren<Lamp>(true);

            SpriteRenderer visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (null != visualRenderer)
                _visual = visualRenderer.transform;
        }
#endif // UNITY_EDITOR
    }
}
