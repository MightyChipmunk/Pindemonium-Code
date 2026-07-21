using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Flipper : MonoBehaviour
    {
        public enum EHitGrade
        {
            Normal = 0,
            SweetSpot = 1,
            Perfect = 2,
        }

        public enum ESide
        {
            Left = 0,
            Right = 1,
        }

        const float NORMAL_MULTIPLIER = 1f;
        const float MIRRORED_DIRECTION = -1f;

        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] FlipperConfigData _config = null;
        [SerializeField] LayerMask _ballLayerMask = default;
        [SerializeField] ESide _side = ESide.Left;

        public event Action<EHitGrade, Vector2> OnBallStruck;

        Func<float> _flipPowerGetter = null;
        bool _isPressed = false;
        float _pressedElapsed = 0f;

        float SideDirection => _side == ESide.Left ? NORMAL_MULTIPLIER : MIRRORED_DIRECTION;
        float RestAngle => _config.RestAngle * SideDirection;
        float PressedAngle => _config.PressedAngle * SideDirection;

        public void Init(Func<float> flipPowerGetter)
        {
            _flipPowerGetter = flipPowerGetter;
            _isPressed = false;
            _pressedElapsed = 0f;

            if (null == _config)
                return;

            _rigidbody.rotation = RestAngle;
        }

        public void Release()
        {
            _flipPowerGetter = null;
            OnBallStruck = null;
        }

        public void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed)
                return;

            _isPressed = isPressed;
            if (isPressed)
                _pressedElapsed = 0f;
        }

        void FixedUpdate()
        {
            if (null == _config)
                return;

            if (_isPressed)
                _pressedElapsed += Time.deltaTime;

            float targetAngle = _isPressed ? PressedAngle : RestAngle;
            float nextAngle = Mathf.MoveTowards(_rigidbody.rotation, targetAngle, _config.RotateSpeed * Time.deltaTime);
            _rigidbody.MoveRotation(nextAngle);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isPressed)
                return;

            if (null == _config)
                return;

            if (null == collision.rigidbody)
                return;

            if (!_ballLayerMask.HasLayer(collision.gameObject.layer))
                return;

            ContactPoint2D contact = collision.GetContact(0);
            EHitGrade grade = EvaluateHitGrade(contact.point);
            float impulse = GetFlipPower() * GetGradeMultiplier(grade);
            collision.rigidbody.AddForce((Vector2)transform.up * impulse, ForceMode2D.Impulse);
            OnBallStruck?.Invoke(grade, contact.point);
        }

        EHitGrade EvaluateHitGrade(Vector2 worldContactPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldContactPoint);
            float ratio = Mathf.Abs(localPoint.x) / _config.FlipperLength;

            bool isSweetSpot = ratio >= _config.SweetSpotMinRatio && ratio <= _config.SweetSpotMaxRatio;
            if (!isSweetSpot)
                return EHitGrade.Normal;

            bool isPerfectTiming = _pressedElapsed >= _config.PerfectTimingMin && _pressedElapsed <= _config.PerfectTimingMax;
            return isPerfectTiming ? EHitGrade.Perfect : EHitGrade.SweetSpot;
        }

        float GetGradeMultiplier(EHitGrade grade)
        {
            switch (grade)
            {
                case EHitGrade.SweetSpot:
                    return _config.SweetSpotBonusMultiplier;
                case EHitGrade.Perfect:
                    return _config.PerfectBonusMultiplier;
                default:
                    return NORMAL_MULTIPLIER;
            }
        }

        float GetFlipPower()
        {
            if (null == _flipPowerGetter)
                return 0f;

            return _flipPowerGetter();
        }
    }
}
