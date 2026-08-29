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
        const float LAUNCH_OVERSHOOT_SCALE_Y = 1.18f;
        const float LAUNCH_SETTLE_DURATION = 0.12f;
        const float LAUNCH_WOBBLE_ANGLE = 7f;
        const float LAUNCH_WOBBLE_DURATION = 0.45f;
        const int LAUNCH_WOBBLE_VIBRATO = 10;
        const float LAUNCH_WOBBLE_ELASTICITY = 0.6f;
        const float FULL_SCALE_Y = 1f;
        const float MIN_DURATION = 0.0001f;
        const float EMPTY_CHARGE_RATIO = 0f;
        const float FULL_RATIO = 1f;
        const float NO_IMPULSE = 0f;
        const float NO_OFFSET = 0f;
        const float CANCEL_PUNCH_SCALE = 0.35f;
        const float CANCEL_PUNCH_DURATION = 0.4f;
        const int CANCEL_PUNCH_VIBRATO = 8;

        [SerializeField] Transform _ballAnchor = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] SpringConfigData _config = null;

        Action<EChargeEvent, Vector2> _chargeEventPlayer = null;
        int _chargeLoopId = AudioManager.INVALID_LOOP_ID;
        Ball _loadedBall = null;
        Ball _launchingBall = null;
        Tween _launchTween = null;
        float _visualTopOffset = NO_OFFSET;
        float _launchAccelForce = NO_IMPULSE;
        float _launchAccelRemainTime = 0f;
        float _chargeElapsed = 0f;
        bool _isCharging = false;
        bool _isPeakReached = false;
        bool _isChargeCanceled = false;

        public void Init(Action<EChargeEvent, Vector2> chargeEventPlayer)
        {
            _chargeEventPlayer = chargeEventPlayer;
            _loadedBall = null;
            ResetCharge();
            StopLaunchAccel();
            ApplyChargeVisual(EMPTY_CHARGE_RATIO);
            _visualTopOffset = CalculateVisualTopOffset();
        }

        public void Release()
        {
            _chargeEventPlayer = null;
            _loadedBall = null;
            ResetCharge();
            StopLaunchAccel();
            KillLaunchTween(false);
        }

        public void LoadBall(Ball ball)
        {
            if (null == ball)
                return;

            _loadedBall = ball;
            ResetCharge();
            ball.SetKinematic(true);
            ball.MoveTo(GetBallHoldPosition());
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

            float totalImpulse = _config.LaunchImpulse * launchPowerRatio;
            float accelDuration = _config.LaunchAccelDuration;
            float initialRatio = accelDuration < MIN_DURATION ? FULL_RATIO : Mathf.Clamp01(_config.LaunchInitialImpulseRatio);

            ball.SetKinematic(false);
            ball.AddImpulse(Vector2.up * (totalImpulse * initialRatio));
            StartLaunchAccel(ball, totalImpulse * (FULL_RATIO - initialRatio), accelDuration);

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

        void FixedUpdate()
        {
            if (null == _launchingBall)
                return;

            if (!_launchingBall.IsSimulated())
            {
                StopLaunchAccel();
                return;
            }

            float step = Mathf.Min(Time.fixedDeltaTime, _launchAccelRemainTime);
            _launchAccelRemainTime -= step;
            _launchingBall.AddImpulse(Vector2.up * (_launchAccelForce * step));

            if (_launchAccelRemainTime <= 0f)
                StopLaunchAccel();
        }

        void StartLaunchAccel(Ball ball, float impulse, float duration)
        {
            if (duration < MIN_DURATION || impulse <= NO_IMPULSE)
            {
                StopLaunchAccel();
                return;
            }

            _launchingBall = ball;
            _launchAccelForce = impulse / duration;
            _launchAccelRemainTime = duration;
        }

        void StopLaunchAccel()
        {
            _launchingBall = null;
            _launchAccelForce = NO_IMPULSE;
            _launchAccelRemainTime = 0f;
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
            FollowBallToVisualTop();
        }

        void FollowBallToVisualTop()
        {
            if (null == _loadedBall)
                return;

            _loadedBall.SetPosition(GetBallHoldPosition());
        }

        Vector3 GetBallHoldPosition()
        {
            Vector3 position = _ballAnchor.position;
            if (null == _visual)
                return position;

            position.y -= _visualTopOffset * (FULL_SCALE_Y - _visual.localScale.y);
            return position;
        }

        float CalculateVisualTopOffset()
        {
            if (null == _visual)
                return NO_OFFSET;

            if (null == _visualRenderer || null == _visualRenderer.sprite)
                return NO_OFFSET;

            Vector3 topLocalPoint = new Vector3(0f, _visualRenderer.sprite.bounds.max.y, 0f);
            return _visualRenderer.transform.TransformPoint(topLocalPoint).y - _visual.position.y;
        }

        void PlayLaunchMotion()
        {
            if (null == _visual)
                return;

            KillLaunchTween(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_visual.DOScaleY(COMPRESS_SCALE_Y, COMPRESS_DURATION).SetEase(Ease.OutQuad));
            sequence.Append(_visual.DOScaleY(LAUNCH_OVERSHOOT_SCALE_Y, RESTORE_DURATION).SetEase(Ease.OutQuad));
            sequence.Append(_visual.DOScaleY(FULL_SCALE_Y, LAUNCH_SETTLE_DURATION).SetEase(Ease.OutBack));
            sequence.Insert(COMPRESS_DURATION, _visual.DOPunchRotation(new Vector3(0f, 0f, LAUNCH_WOBBLE_ANGLE), LAUNCH_WOBBLE_DURATION, LAUNCH_WOBBLE_VIBRATO, LAUNCH_WOBBLE_ELASTICITY));
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
