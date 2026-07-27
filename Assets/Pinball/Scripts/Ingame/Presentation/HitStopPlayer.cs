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

        CancellationTokenSource _lifetimeTokenSource = null;
        CancellationTokenSource _playTokenSource = null;

        public void Init()
        {
            _lifetimeTokenSource = new CancellationTokenSource();
        }

        public void Release()
        {
            CancelPlaying();

            if (null != _lifetimeTokenSource)
            {
                _lifetimeTokenSource.Cancel();
                _lifetimeTokenSource.Dispose();
                _lifetimeTokenSource = null;
            }

            Time.timeScale = DEFAULT_TIME_SCALE;
        }

        public void Play(float timeScale, float holdDuration, float recoverDuration, EaseType recoverEase)
        {
            if (null == _lifetimeTokenSource)
                return;

            CancelPlaying();
            _playTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token);
            PlayAsync(Mathf.Clamp01(timeScale), holdDuration, recoverDuration, recoverEase, _playTokenSource.Token).Forget();
        }

        void CancelPlaying()
        {
            if (null == _playTokenSource)
                return;

            _playTokenSource.Cancel();
            _playTokenSource.Dispose();
            _playTokenSource = null;
        }

        async UniTaskVoid PlayAsync(float timeScale, float holdDuration, float recoverDuration, EaseType recoverEase, CancellationToken cancellationToken)
        {
            Time.timeScale = timeScale;

            if (holdDuration >= MIN_DURATION)
                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), DelayType.UnscaledDeltaTime, cancellationToken: cancellationToken);

            float elapsed = 0f;
            while (elapsed < recoverDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                elapsed += Time.unscaledDeltaTime;
                float ratio = EaseUtil.Interpolate(recoverEase, Mathf.Clamp01(elapsed / recoverDuration));
                Time.timeScale = Mathf.Lerp(timeScale, DEFAULT_TIME_SCALE, ratio);
            }

            Time.timeScale = DEFAULT_TIME_SCALE;
        }
    }
}
