using System;
using UnityEngine;

namespace Pinball.Ingame
{
    public class MissileBody : EffectBodyBase
    {
        const float LAUNCH_SPEED = 7f;
        const float MAX_SPEED = 17f;
        const float ACCELERATION = 22f;
        const float TURN_DEGREES_PER_SECOND = 480f;
        const float MIN_STEER_SQR_DISTANCE = 0.0001f;
        const float UP_ANGLE_OFFSET_DEGREE = 90f;

        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] TrailRenderer _trail = null;
        [SerializeField] Transform _visual = null;

        Vector2 _targetPosition = Vector2.zero;
        Vector2 _moveDirection = Vector2.up;
        float _speed = 0f;
        bool _isExpired = false;

        public bool IsExpired => _isExpired;

        public event Action<MissileBody, Collider2D> OnContact;

        public void Launch(Vector2 startPosition, Vector2 launchDirection, Vector2 targetPosition)
        {
            _rigidbody.position = startPosition;
            MoveTo(startPosition);
            _moveDirection = launchDirection;
            _targetPosition = targetPosition;
            _speed = LAUNCH_SPEED;
            _isExpired = false;
            ApplyVisualRotation();

            if (null != _trail)
                _trail.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_isExpired)
                return;

            Vector2 toTarget = _targetPosition - GetPosition();
            if (toTarget.sqrMagnitude > MIN_STEER_SQR_DISTANCE)
            {
                float signedAngle = Vector2.SignedAngle(_moveDirection, toTarget);
                float stepDegree = Mathf.Clamp(signedAngle, -TURN_DEGREES_PER_SECOND * deltaTime, TURN_DEGREES_PER_SECOND * deltaTime);
                _moveDirection = Quaternion.Euler(0f, 0f, stepDegree) * _moveDirection;
            }

            _speed = Mathf.Min(MAX_SPEED, _speed + ACCELERATION * deltaTime);
            _rigidbody.MovePosition(GetPosition() + _moveDirection * (_speed * deltaTime));
            ApplyVisualRotation();
        }

        public void Expire()
        {
            _isExpired = true;
        }

        public override void OnReturn()
        {
            base.OnReturn();
            OnContact = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_isExpired)
                return;

            OnContact?.Invoke(this, other);
        }

        void ApplyVisualRotation()
        {
            if (null == _visual)
                return;

            float angleDegree = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg - UP_ANGLE_OFFSET_DEGREE;
            _visual.localRotation = Quaternion.Euler(0f, 0f, angleDegree);
        }
    }
}
