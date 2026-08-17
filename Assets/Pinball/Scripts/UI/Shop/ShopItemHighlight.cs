using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.Util;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pinball.UI
{
    public class ShopItemHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        const float HOVER_SCALE = 1.06f;
        const float HOVER_LIFT = 16f;
        const float ENTER_DURATION = 0.18f;
        const float EXIT_DURATION = 0.12f;
        const float PULSE_FREQUENCY = 1.4f;
        const float PULSE_MIN_ALPHA = 0.55f;
        const float TAU = Mathf.PI * 2f;

        [SerializeField] RectTransform _visual = null;
        [SerializeField] CanvasGroup _fxGroup = null;

        Vector2 _basePosition = Vector2.zero;
        float _progress = 0f;
        bool _isInteractable = true;
        bool _isHovered = false;
        CancellationTokenSource _tweenTokenSource = null;

        public void Init()
        {
            _basePosition = _visual.anchoredPosition;
            ApplyProgress(0f);
        }

        public void Release()
        {
            CancelTween();
            _isHovered = false;
            ApplyProgress(0f);
        }

        public void SetInteractable(bool isInteractable)
        {
            _isInteractable = isInteractable;
            if (isInteractable)
                return;

            _isHovered = false;
            if (_progress > 0f)
            {
                StartTween();
                return;
            }

            CancelTween();
            ApplyProgress(0f);
        }

        void OnDestroy()
        {
            CancelTween();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
                return;

            _isHovered = true;
            StartTween();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            StartTween();
        }

        void StartTween()
        {
            CancelTween();
            _tweenTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TweenAsync(_isHovered, _tweenTokenSource.Token).Forget();
        }

        void CancelTween()
        {
            if (null == _tweenTokenSource)
                return;

            _tweenTokenSource.Cancel();
            _tweenTokenSource.Dispose();
            _tweenTokenSource = null;
        }

        async UniTaskVoid TweenAsync(bool toHovered, CancellationToken cancellationToken)
        {
            float start = _progress;
            float target = toHovered ? 1f : 0f;
            float duration = toHovered ? ENTER_DURATION : EXIT_DURATION;
            EaseType ease = toHovered ? EaseType.BackOut : EaseType.QuadOut;
            float time = 0f;
            while (time < duration)
            {
                float ratio = EaseUtil.Interpolate(ease, Mathf.Clamp01(time / duration));
                ApplyProgress(Mathf.LerpUnclamped(start, target, ratio));
                time += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

            ApplyProgress(target);
            if (!toHovered)
                return;

            float pulseTime = 0f;
            while (true)
            {
                pulseTime += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Cos(pulseTime * PULSE_FREQUENCY * TAU);
                _fxGroup.alpha = Mathf.Lerp(PULSE_MIN_ALPHA, 1f, pulse);
                await UniTask.Yield(cancellationToken);
            }
        }

        void ApplyProgress(float progress)
        {
            _progress = progress;
            _visual.localScale = Vector3.one * Mathf.LerpUnclamped(1f, HOVER_SCALE, progress);
            _visual.anchoredPosition = _basePosition + Vector2.up * (HOVER_LIFT * progress);
            _fxGroup.alpha = Mathf.Clamp01(progress);
        }
    }
}
