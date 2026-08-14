using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    public class HitStopPlayer
    {
        const float DEFAULT_TIME_SCALE = 1f;
        const float MIN_DURATION = 0.0001f;

        bool _isActive = false;
        CancellationTokenSource _playTokenSource = null;

        public void Init()
        {
            _isActive = true;
        }

        public void Release()
        {
            CancelPlaying();
            _isActive = false;
            Time.timeScale = DEFAULT_TIME_SCALE;
        }

        public void Play(HitPresentationData presentation)
        {
            if (!_isActive)
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
