using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Audio;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SpinnerInteractable : MapInteractable, IDamageable
    {
        const float FULL_ROTATION = 360f;
        const float REST_ANGLE = 0f;
        const float MIN_SPIN_SPEED = 240f;
        const int MIN_REVOLUTION_DIVISOR = 1;
        const float TICK_MIN_PITCH = 0.85f;
        const float TICK_MAX_PITCH = 1.5f;
        const float BASE_SCORE_MULTIPLIER = 1f;

        [SerializeField] Transform _visual = null;
        [SerializeField] int _scoreRewardPerRevolution = 0;
        [SerializeField] float _minEntrySpeed = 4f;
        [SerializeField] float _maxEntrySpeed = 20f;
        [SerializeField] int _minRevolutions = 1;
        [SerializeField] int _maxRevolutions = 8;
        [SerializeField] float _maxSpinSpeed = 1440f;

        CancellationTokenSource _cancellationTokenSource = null;
        float _remainingRevolutions = 0f;
        float _currentAngle = 0f;
        float _spinScoreMultiplier = BASE_SCORE_MULTIPLIER;
        bool _isSpinning = false;
        bool _hasPendingBallHit = false;

        public override EGimmickType GimmickType => EGimmickType.Spinner;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _remainingRevolutions = 0f;
            _currentAngle = REST_ANGLE;
            _spinScoreMultiplier = BASE_SCORE_MULTIPLIER;
            _isSpinning = false;
            _hasPendingBallHit = false;
            ApplyVisualAngle(REST_ANGLE);
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _remainingRevolutions = 0f;
            _spinScoreMultiplier = BASE_SCORE_MULTIPLIER;
            _isSpinning = false;
            _hasPendingBallHit = false;
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            if (IsDeactivated)
                return;

            AddRevolutions(_minRevolutions);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsBallLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body)
                return;

            AddRevolutions(GetRevolutions(body.linearVelocity.magnitude));
        }

        void AddRevolutions(float revolutions)
        {
            if (null == _cancellationTokenSource)
                return;

            _remainingRevolutions += revolutions;
            _hasPendingBallHit = true;
            PlayGimmickHitPresentation();
            PlaySpinTick();

            if (_isSpinning)
                return;

            SpinAsync(_cancellationTokenSource.Token).Forget();
        }

        float GetRevolutions(float entrySpeed)
        {
            return Mathf.Lerp(_minRevolutions, _maxRevolutions, Mathf.InverseLerp(_minEntrySpeed, _maxEntrySpeed, entrySpeed));
        }

        float GetSpinRatio()
        {
            return Mathf.Clamp01(_remainingRevolutions / Mathf.Max(_maxRevolutions, MIN_REVOLUTION_DIVISOR));
        }

        float GetSpinSpeed()
        {
            return Mathf.Lerp(MIN_SPIN_SPEED, _maxSpinSpeed, GetSpinRatio());
        }

        void PlaySpinTick()
        {
            if (IsDeactivated)
                return;

            AudioManager.PlaySequenced(AudioStorage.EAudioClipType.GimmickHitSpinner, Mathf.Lerp(TICK_MIN_PITCH, TICK_MAX_PITCH, GetSpinRatio()));
        }

        async UniTaskVoid SpinAsync(CancellationToken cancellationToken)
        {
            _isSpinning = true;
            _spinScoreMultiplier = BASE_SCORE_MULTIPLIER;

            while (_remainingRevolutions > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                float deltaAngle = GetSpinSpeed() * Time.deltaTime;
                _currentAngle += deltaAngle;
                _remainingRevolutions -= deltaAngle / FULL_ROTATION;

                if (_currentAngle >= FULL_ROTATION)
                {
                    _currentAngle -= FULL_ROTATION;
                    GainScore(Mathf.RoundToInt(_scoreRewardPerRevolution * _spinScoreMultiplier), transform.position, ConsumeScoreSource());
                    _spinScoreMultiplier *= BASE_SCORE_MULTIPLIER + StatProvider.GetSpinnerScoreGrowth();
                    PlaySpinTick();
                }

                ApplyVisualAngle(_currentAngle);
            }

            _remainingRevolutions = 0f;
            _currentAngle = REST_ANGLE;
            _spinScoreMultiplier = BASE_SCORE_MULTIPLIER;
            ApplyVisualAngle(REST_ANGLE);
            _isSpinning = false;
        }

        EScoreSource ConsumeScoreSource()
        {
            if (!_hasPendingBallHit)
                return EScoreSource.PassiveEffect;

            _hasPendingBallHit = false;
            return EScoreSource.BallHit;
        }

        void ApplyVisualAngle(float angle)
        {
            if (null == _visual)
                return;

            _visual.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
