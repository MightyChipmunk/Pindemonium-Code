using System;
using DG.Tweening;
using Pinball.Audio;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Spring : MonoBehaviour
    {
        public enum EChargeEvent
        {
            Peaked = 0,
            Canceled = 1,
        }

        const float COMPRESS_SCALE_Y = 0.6f;
        const float COMPRESS_DURATION = 0.05f;
        const float RESTORE_DURATION = 0.15f;
        const float FULL_SCALE_Y = 1f;
        const float MIN_DURATION = 0.0001f;
        const float EMPTY_CHARGE_RATIO = 0f;
        const float CANCEL_PUNCH_SCALE = 0.35f;
        const float CANCEL_PUNCH_DURATION = 0.4f;
        const int CANCEL_PUNCH_VIBRATO = 8;

        [SerializeField] Transform _ballAnchor = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpringConfigData _config = null;

        Action<EChargeEvent, Vector2> _chargeEventPlayer = null;
        int _chargeLoopId = AudioManager.INVALID_LOOP_ID;
        Ball _loadedBall = null;
        Tween _launchTween = null;
        float _chargeElapsed = 0f;
        bool _isCharging = false;
        bool _isPeakReached = false;
        bool _isChargeCanceled = false;

        public void Init(Action<EChargeEvent, Vector2> chargeEventPlayer)
        {
            _chargeEventPlayer = chargeEventPlayer;
            _loadedBall = null;
            ResetCharge();
        }

        public void Release()
        {
            _chargeEventPlayer = null;
            _loadedBall = null;
            ResetCharge();
            KillLaunchTween(false);
        }

        public void LoadBall(Ball ball)
        {
            if (null == ball)
                return;

            _loadedBall = ball;
            ResetCharge();
            ball.SetKinematic(true);
            ball.MoveTo(_ballAnchor.position);
        }

        public void SetCharging(bool isCharging)
        {
            if (_isCharging == isCharging)
                return;

            if (isCharging && _isChargeCanceled)
                return;

            if (isCharging && null == _loadedBall)
                return;

            _isCharging = isCharging;
            _chargeElapsed = 0f;
            _isPeakReached = false;

            if (isCharging)
            {
                KillLaunchTween(true);
                AudioManager.Play(AudioStorage.EAudioClipType.SpringChargeStart);
                StartChargeLoop();
                return;
            }

            StopChargeLoop();
            ApplyChargeVisual(EMPTY_CHARGE_RATIO);
        }

        public bool TryLaunch(out float launchPowerRatio)
        {
            launchPowerRatio = EMPTY_CHARGE_RATIO;
            StopChargeLoop();

            if (null == _config)
                return false;

            if (_isChargeCanceled)
            {
                _isChargeCanceled = false;
                return false;
            }

            if (null == _loadedBall)
            {
                SetCharging(false);
                return false;
            }

            Ball ball = _loadedBall;
            launchPowerRatio = Mathf.Lerp(_config.MinPowerRatio, 1f, GetChargeRatio());
            _loadedBall = null;
            ResetCharge();
            ball.SetKinematic(false);
            ball.AddImpulse(Vector2.up * (_config.LaunchImpulse * launchPowerRatio));
            PlayLaunchMotion();
            AudioManager.Play(AudioStorage.EAudioClipType.SpringLaunch);
            return true;
        }

        void Update()
        {
            if (!_isCharging)
                return;

            if (null == _config)
                return;

            _chargeElapsed += Time.deltaTime;

            if (!_isPeakReached && _chargeElapsed >= _config.PeakChargeDuration)
            {
                _isPeakReached = true;
                _chargeEventPlayer?.Invoke(EChargeEvent.Peaked, transform.position);
            }

            if (_chargeElapsed >= _config.ResetChargeDuration)
            {
                CancelCharge();
                return;
            }

            float visualChargeRatio = GetVisualChargeRatio();
            ApplyChargeVisual(visualChargeRatio);
            AudioManager.SetLoopVolumeRatio(_chargeLoopId, visualChargeRatio);
        }

        void StartChargeLoop()
        {
            StopChargeLoop();
            _chargeLoopId = AudioManager.PlayLoop(AudioStorage.EAudioClipType.SpringChargeLoop);
        }

        void StopChargeLoop()
        {
            if (_chargeLoopId == AudioManager.INVALID_LOOP_ID)
                return;

            AudioManager.StopLoop(_chargeLoopId);
            _chargeLoopId = AudioManager.INVALID_LOOP_ID;
        }

        void CancelCharge()
        {
            ResetCharge();
            _isChargeCanceled = true;
            ApplyChargeVisual(EMPTY_CHARGE_RATIO);
            PlayCancelMotion();
            _chargeEventPlayer?.Invoke(EChargeEvent.Canceled, transform.position);
        }

        void ResetCharge()
        {
            StopChargeLoop();
            _isCharging = false;
            _isPeakReached = false;
            _isChargeCanceled = false;
            _chargeElapsed = 0f;
        }

        float GetVisualChargeRatio()
        {
            if (_config.PeakChargeDuration < MIN_DURATION)
                return 1f;

            return Mathf.Clamp01(_chargeElapsed / _config.PeakChargeDuration);
        }

        float GetChargeRatio()
        {
            if (_config.PeakChargeDuration < MIN_DURATION)
                return 1f;

            if (_chargeElapsed <= _config.PeakChargeDuration)
                return Mathf.Clamp01(_chargeElapsed / _config.PeakChargeDuration);

            float decayDuration = _config.ResetChargeDuration - _config.PeakChargeDuration;
            if (decayDuration < MIN_DURATION)
                return EMPTY_CHARGE_RATIO;

            return Mathf.Clamp01(1f - (_chargeElapsed - _config.PeakChargeDuration) / decayDuration);
        }

        void ApplyChargeVisual(float chargeRatio)
        {
            if (null == _visual)
                return;

            Vector3 scale = _visual.localScale;
            scale.y = Mathf.Lerp(FULL_SCALE_Y, COMPRESS_SCALE_Y, chargeRatio);
            _visual.localScale = scale;
        }

        void PlayLaunchMotion()
        {
            if (null == _visual)
                return;

            KillLaunchTween(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_visual.DOScaleY(COMPRESS_SCALE_Y, COMPRESS_DURATION));
            sequence.Append(_visual.DOScaleY(FULL_SCALE_Y, RESTORE_DURATION));
            _launchTween = sequence;
        }

        void PlayCancelMotion()
        {
            if (null == _visual)
                return;

            KillLaunchTween(true);
            _launchTween = _visual.DOPunchScale(Vector3.one * CANCEL_PUNCH_SCALE, CANCEL_PUNCH_DURATION, CANCEL_PUNCH_VIBRATO);
        }

        void KillLaunchTween(bool isComplete)
        {
            if (null == _launchTween)
                return;

            _launchTween.Kill(isComplete);
            _launchTween = null;
        }
    }
}
