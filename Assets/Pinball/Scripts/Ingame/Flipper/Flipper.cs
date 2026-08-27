using System;
using System.Collections.Generic;
using Pinball.Audio;
using Pinball.Ingame.Stat;
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
        const float TIMING_RELAX_SIDE_RATIO = 0.5f;
        const float MIN_FALLOFF_RANGE = 0.0001f;
        const float SWING_ANGLE_EPSILON = 0.01f;
        const string HIT_LOG_FORMAT = "[Flipper] {0} → {1} | 타이밍 {2:F3}s (퍼펙트 {3:F2}~{4:F2}s) | 위치 {5:F2} (스위트스팟 {6:F2}~{7:F2}) | 세기 x{8:F2}";

        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] FlipperConfigData _config = null;
        [SerializeField] LayerMask _ballLayerMask = default;
        [SerializeField] ESide _side = ESide.Left;

        public event Action<EHitGrade, Vector2> OnBallStruck;

        Vector3 _baseLocalScale = Vector3.one;
        float _rotateSpeedScale = NORMAL_MULTIPLIER;
        List<Rigidbody2D> _touchingBodies = null;
        List<Rigidbody2D> _struckBodies = null;
        bool _isPressed = false;
        bool _isSwingActive = false;
        float _pressedElapsed = 0f;

        public ESide Side => _side;

        float SideDirection => _side == ESide.Left ? NORMAL_MULTIPLIER : MIRRORED_DIRECTION;
        float RestAngle => _config.RestAngle * SideDirection;
        float PressedAngle => _config.PressedAngle * SideDirection;

        public void Init()
        {
            _baseLocalScale = transform.localScale;
            _rotateSpeedScale = StatProvider.GetFlipperRotateSpeedScale();
            ApplyLengthScale(StatProvider.GetFlipperLengthScale());
            _touchingBodies = new List<Rigidbody2D>();
            _struckBodies = new List<Rigidbody2D>();
            _isPressed = false;
            _isSwingActive = false;
            _pressedElapsed = 0f;

            if (null == _config)
                return;

            _rigidbody.rotation = RestAngle;
        }

        public void Release()
        {
            if (null != _touchingBodies)
            {
                _touchingBodies.Clear();
                _touchingBodies = null;
            }

            if (null != _struckBodies)
            {
                _struckBodies.Clear();
                _struckBodies = null;
            }

            transform.localScale = _baseLocalScale;
            _rotateSpeedScale = NORMAL_MULTIPLIER;
            OnBallStruck = null;
        }

        public void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed)
                return;

            _isPressed = isPressed;

            if (!isPressed)
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.FlipperSwing);

            if (null != _struckBodies)
                _struckBodies.Clear();

            _isSwingActive = true;
            _pressedElapsed = 0f;
            StrikeTouchingBodies();
        }

        void StrikeTouchingBodies()
        {
            if (null == _touchingBodies)
                return;

            for (int i = _touchingBodies.Count - 1; i >= 0; --i)
            {
                Rigidbody2D body = _touchingBodies[i];
                if (null == body)
                {
                    _touchingBodies.RemoveAt(i);
                    continue;
                }

                Strike(body);
            }
        }

        void FixedUpdate()
        {
            if (null == _config)
                return;

            if (_isPressed || _isSwingActive)
                _pressedElapsed += Time.deltaTime;

            float targetAngle = _isPressed || _isSwingActive ? PressedAngle : RestAngle;
            float nextAngle = Mathf.MoveTowards(_rigidbody.rotation, targetAngle, _config.RotateSpeed * _rotateSpeedScale * Time.deltaTime);
            _rigidbody.MoveRotation(nextAngle);

            if (_isSwingActive && Mathf.Abs(Mathf.DeltaAngle(nextAngle, PressedAngle)) <= SWING_ANGLE_EPSILON)
                _isSwingActive = false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (null == collision.rigidbody)
                return;

            if (collision.rigidbody.bodyType != RigidbodyType2D.Dynamic)
                return;

            if (!_ballLayerMask.HasLayer(collision.gameObject.layer))
                return;

            if (null != _touchingBodies && !_touchingBodies.Contains(collision.rigidbody))
                _touchingBodies.Add(collision.rigidbody);

            Strike(collision.rigidbody);
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (null == _touchingBodies)
                return;

            if (null == collision.rigidbody)
                return;

            _touchingBodies.Remove(collision.rigidbody);
        }

        bool IsSwinging()
        {
            if (!_isPressed && !_isSwingActive)
                return false;

            return Mathf.Abs(Mathf.DeltaAngle(_rigidbody.rotation, PressedAngle)) > SWING_ANGLE_EPSILON;
        }

        void Strike(Rigidbody2D body)
        {
            if (null == _config)
                return;

            if (!IsSwinging())
                return;

            if (null == _struckBodies || _struckBodies.Contains(body))
                return;

            _struckBodies.Add(body);

            float hitRatio = GetHitRatio(body.position);
            EHitGrade grade = EvaluateHitGrade(hitRatio);
            float impulseScale = GetImpulseScale(grade, hitRatio);
            body.AddForce((Vector2)transform.up * (StatProvider.GetFlipPower() * impulseScale), ForceMode2D.Impulse);
            LogHit(grade, hitRatio, impulseScale);
            OnBallStruck?.Invoke(grade, body.position);
        }

        float GetHitRatio(Vector2 worldContactPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldContactPoint);
            return Mathf.Abs(localPoint.x) / _config.FlipperLength;
        }

        EHitGrade EvaluateHitGrade(float hitRatio)
        {
            bool isSweetSpot = hitRatio >= _config.SweetSpotMinRatio && hitRatio <= _config.SweetSpotMaxRatio;
            if (!isSweetSpot)
                return EHitGrade.Normal;

            float timingRelax = StatProvider.GetPerfectTimingRelax() * TIMING_RELAX_SIDE_RATIO;
            bool isPerfectTiming = _pressedElapsed >= _config.PerfectTimingMin - timingRelax && _pressedElapsed <= _config.PerfectTimingMax + timingRelax;
            return isPerfectTiming ? EHitGrade.Perfect : EHitGrade.SweetSpot;
        }

        void LogHit(EHitGrade grade, float hitRatio, float impulseScale)
        {
#if UNITY_EDITOR
            if (!_config.IsHitLogEnabled)
                return;

            Debug.LogFormat(HIT_LOG_FORMAT,
                name,
                grade,
                _pressedElapsed,
                _config.PerfectTimingMin,
                _config.PerfectTimingMax,
                hitRatio,
                _config.SweetSpotMinRatio,
                _config.SweetSpotMaxRatio,
                impulseScale);
#endif
        }

        float GetImpulseScale(EHitGrade grade, float hitRatio)
        {
            switch (grade)
            {
                case EHitGrade.Perfect:
                    return _config.PerfectBonusMultiplier * StatProvider.GetPerfectImpulseScale() * StatProvider.GetHitGradeFlipPowerScale(grade);
                case EHitGrade.SweetSpot:
                    return _config.SweetSpotBonusMultiplier * GetTimingScale(_config.SweetSpotTimingMinScale);
                default:
                    return NORMAL_MULTIPLIER * GetSpotScale(hitRatio) * GetTimingScale(_config.NormalTimingMinScale) * StatProvider.GetHitGradeFlipPowerScale(grade);
            }
        }

        float GetTimingScale(float minScale)
        {
            return GetFalloffScale(GetPerfectTimingDistance(), _config.TimingFalloffRange, minScale);
        }

        float GetSpotScale(float hitRatio)
        {
            return GetFalloffScale(GetSweetSpotDistance(hitRatio), _config.SpotFalloffRange, _config.NormalSpotMinScale);
        }

        float GetPerfectTimingDistance()
        {
            if (_pressedElapsed < _config.PerfectTimingMin)
                return _config.PerfectTimingMin - _pressedElapsed;

            if (_pressedElapsed > _config.PerfectTimingMax)
                return _pressedElapsed - _config.PerfectTimingMax;

            return 0f;
        }

        float GetSweetSpotDistance(float hitRatio)
        {
            if (hitRatio < _config.SweetSpotMinRatio)
                return _config.SweetSpotMinRatio - hitRatio;

            if (hitRatio > _config.SweetSpotMaxRatio)
                return hitRatio - _config.SweetSpotMaxRatio;

            return 0f;
        }

        static float GetFalloffScale(float distance, float falloffRange, float minScale)
        {
            if (distance <= 0f)
                return NORMAL_MULTIPLIER;

            if (falloffRange < MIN_FALLOFF_RANGE)
                return minScale;

            return Mathf.Lerp(NORMAL_MULTIPLIER, minScale, Mathf.Clamp01(distance / falloffRange));
        }

        void ApplyLengthScale(float lengthScale)
        {
            transform.localScale = new Vector3(_baseLocalScale.x * lengthScale, _baseLocalScale.y, _baseLocalScale.z);
        }
    }
}
