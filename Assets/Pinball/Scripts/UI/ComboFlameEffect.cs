using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ComboFlameEffect : MonoBehaviour
    {
        const int START_COMBO = 10;
        const int MAX_COMBO = 50;
        const int BURST_INTERVAL = 10;
        const float MIN_SCALE = 1f;
        const float MAX_SCALE = 1.38f;
        const float MIN_ALPHA = 0.7f;
        const float MAX_ALPHA = 1f;
        const float MIN_SHAKE_STRENGTH = 0.4f;
        const float FRAME_INTERVAL = 0.07f;
        const int NO_BURST_STEP = 0;

        [SerializeField] RectTransform _flameRoot = null;
        [SerializeField] Image _flameImage = null;
        [SerializeField] Sprite[] _flameFrames = null;
        [SerializeField] UIIntensityShake _shake = null;

        Vector3 _baseScale = Vector3.one;
        int _lastBurstStep = NO_BURST_STEP;
        CancellationTokenSource _tokenSource = null;

        void Awake()
        {
            _baseScale = _flameRoot.localScale;
            _lastBurstStep = NO_BURST_STEP;
            _flameRoot.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            Cancel();
        }

        public void Hide()
        {
            Cancel();
            _lastBurstStep = NO_BURST_STEP;

            if (null != _flameRoot)
                _flameRoot.gameObject.SetActive(false);
        }

        public void SetCombo(int comboCount)
        {
            if (comboCount < START_COMBO)
            {
                Hide();
                return;
            }

            float intensity = GetIntensity(comboCount);
            _flameRoot.gameObject.SetActive(true);
            _flameRoot.localScale = _baseScale * Mathf.Lerp(MIN_SCALE, MAX_SCALE, intensity);

            Color color = _flameImage.color;
            color.a = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, intensity);
            _flameImage.color = color;

            StartFlicker();
            TryBurst(comboCount, intensity);
        }

        static float GetIntensity(int comboCount)
        {
            return Mathf.Clamp01((comboCount - START_COMBO) / (float)(MAX_COMBO - START_COMBO));
        }

        void TryBurst(int comboCount, float intensity)
        {
            int burstStep = comboCount / BURST_INTERVAL;
            if (burstStep <= _lastBurstStep)
                return;

            _lastBurstStep = burstStep;
            _shake.Play(Mathf.Lerp(MIN_SHAKE_STRENGTH, 1f, intensity));
        }

        void StartFlicker()
        {
            if (null != _tokenSource)
                return;

            if (null == _flameFrames || _flameFrames.Length <= 0)
                return;

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            FlickerAsync(_tokenSource.Token).Forget();
        }

        void Cancel()
        {
            if (null == _tokenSource)
                return;

            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
        }

        async UniTaskVoid FlickerAsync(CancellationToken cancellationToken)
        {
            int frameIndex = 0;
            while (null != _flameImage)
            {
                _flameImage.sprite = _flameFrames[frameIndex];
                frameIndex = (frameIndex + 1) % _flameFrames.Length;
                await UniTask.Delay(TimeSpan.FromSeconds(FRAME_INTERVAL), cancellationToken: cancellationToken);
            }
        }
    }
}
