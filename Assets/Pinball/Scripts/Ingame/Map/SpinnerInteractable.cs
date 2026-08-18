using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SpinnerInteractable : MapInteractable
    {
        const float FULL_ROTATION = 360f;
        const float REST_ANGLE = 0f;
        const float MIN_SPIN_SPEED = 240f;
        const int MIN_REVOLUTION_DIVISOR = 1;

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
        bool _isSpinning = false;

        public override EGimmickType GimmickType => EGimmickType.Spinner;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _remainingRevolutions = 0f;
            _currentAngle = REST_ANGLE;
            _isSpinning = false;
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
            _isSpinning = false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (null == _cancellationTokenSource)
                return;

            if (!IsBallLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body)
                return;

            _remainingRevolutions += GetRevolutions(body.linearVelocity.magnitude);
            PlayGimmickHitSound();

            if (_isSpinning)
                return;

            SpinAsync(_cancellationTokenSource.Token).Forget();
        }

        float GetRevolutions(float entrySpeed)
        {
            return Mathf.Lerp(_minRevolutions, _maxRevolutions, Mathf.InverseLerp(_minEntrySpeed, _maxEntrySpeed, entrySpeed));
        }

        float GetSpinSpeed()
        {
            float remainingRatio = Mathf.Clamp01(_remainingRevolutions / Mathf.Max(_maxRevolutions, MIN_REVOLUTION_DIVISOR));
            return Mathf.Lerp(MIN_SPIN_SPEED, _maxSpinSpeed, remainingRatio);
        }

        async UniTaskVoid SpinAsync(CancellationToken cancellationToken)
        {
            _isSpinning = true;

            while (_remainingRevolutions > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                float deltaAngle = GetSpinSpeed() * Time.deltaTime;
                _currentAngle += deltaAngle;
                _remainingRevolutions -= deltaAngle / FULL_ROTATION;

                if (_currentAngle >= FULL_ROTATION)
                {
                    _currentAngle -= FULL_ROTATION;
                    GainScore(_scoreRewardPerRevolution);
                }

                ApplyVisualAngle(_currentAngle);
            }

            _remainingRevolutions = 0f;
            _currentAngle = REST_ANGLE;
            ApplyVisualAngle(REST_ANGLE);
            _isSpinning = false;
        }

        void ApplyVisualAngle(float angle)
        {
            if (null == _visual)
                return;

            _visual.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
