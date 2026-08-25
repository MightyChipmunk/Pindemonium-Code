using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    public class HitStopPlayer : IDisposable
    {
        const float DEFAULT_TIME_SCALE = 1f;
        const float PAUSED_TIME_SCALE = 0f;
        const float MIN_DURATION = 0.0001f;

        bool _isActive = true;
        bool _isPaused = false;
        bool _isNudgeAiming = false;
        CancellationTokenSource _playTokenSource = null;

        public void Dispose()
        {
            CancelPlaying();
            _isActive = false;
            _isPaused = false;
            _isNudgeAiming = false;
            Time.timeScale = DEFAULT_TIME_SCALE;
        }

        public void SetPaused(bool isPaused)
        {
            if (!_isActive)
                return;

            _isPaused = isPaused;
            CancelPlaying();
            Time.timeScale = _isPaused || _isNudgeAiming ? PAUSED_TIME_SCALE : DEFAULT_TIME_SCALE;
        }

        public void SetNudgeAiming(bool isAiming)
        {
            if (!_isActive)
                return;

            _isNudgeAiming = isAiming;
            if (_isPaused)
                return;

            CancelPlaying();
            Time.timeScale = _isNudgeAiming ? PAUSED_TIME_SCALE : DEFAULT_TIME_SCALE;
        }

        public void Play(HitPresentationData presentation)
        {
            if (!_isActive)
                return;

            if (_isPaused)
                return;

            if (_isNudgeAiming)
                return;

            if (!presentation.IsHitStopEnabled)
                return;

            CancelPlaying();
            _playTokenSource = new CancellationTokenSource();
            PlayAsync(presentation, _playTokenSource.Token).Forget();
        }

        void CancelPlaying()
        {
            if (null == _playTokenSource)
                return;

            _playTokenSource.Cancel();
            _playTokenSource.Dispose();
            _playTokenSource = null;
        }

        async UniTaskVoid PlayAsync(HitPresentationData presentation, CancellationToken cancellationToken)
        {
            float timeScale = Mathf.Clamp01(presentation.HitStopTimeScale);
            Time.timeScale = timeScale;

            if (presentation.HitStopDuration >= MIN_DURATION)
                await UniTask.Delay(TimeSpan.FromSeconds(presentation.HitStopDuration), DelayType.UnscaledDeltaTime, cancellationToken: cancellationToken);

            float elapsed = 0f;
            while (elapsed < presentation.RecoverDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                elapsed += Time.unscaledDeltaTime;
                float ratio = EaseUtil.Interpolate(presentation.RecoverEase, Mathf.Clamp01(elapsed / presentation.RecoverDuration));
                Time.timeScale = Mathf.Lerp(timeScale, DEFAULT_TIME_SCALE, ratio);
            }

            Time.timeScale = DEFAULT_TIME_SCALE;
        }
    }
}
