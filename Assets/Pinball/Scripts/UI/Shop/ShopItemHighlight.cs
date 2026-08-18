using DG.Tweening;
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
        const float PULSE_HALF_PERIOD = 0.36f;
        const float PULSE_MIN_ALPHA = 0.55f;
        const float HOVERED_PROGRESS = 1f;
        const float IDLE_PROGRESS = 0f;

        [SerializeField] RectTransform _visual = null;
        [SerializeField] CanvasGroup _fxGroup = null;

        Vector2 _basePosition = Vector2.zero;
        float _progress = IDLE_PROGRESS;
        bool _isInited = false;
        bool _isInteractable = true;
        bool _isHovered = false;
        Tween _tween = null;

        public void Init()
        {
            _basePosition = _visual.anchoredPosition;
            _isInited = true;
            ApplyProgress(IDLE_PROGRESS);
        }

        public void Release()
        {
            ResetState();
            _isInited = false;
        }

        public void SetInteractable(bool isInteractable)
        {
            _isInteractable = isInteractable;
            if (isInteractable)
                return;

            _isHovered = false;
            if (_progress > IDLE_PROGRESS)
            {
                PlayTween();
                return;
            }

            KillTween();
            ApplyProgress(IDLE_PROGRESS);
        }

        void OnDisable()
        {
            ResetState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
                return;

            _isHovered = true;
            PlayTween();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            PlayTween();
        }

        void ResetState()
        {
            KillTween();
            _isHovered = false;

            if (_isInited)
                ApplyProgress(IDLE_PROGRESS);
        }

        void PlayTween()
        {
            KillTween();

            bool toHovered = _isHovered;
            float target = toHovered ? HOVERED_PROGRESS : IDLE_PROGRESS;
            float duration = toHovered ? ENTER_DURATION : EXIT_DURATION;
            Ease ease = toHovered ? Ease.OutBack : Ease.OutQuad;

            Tween progressTween = DOTween.To(GetProgress, ApplyProgress, target, duration).SetEase(ease).SetUpdate(true);
            if (toHovered)
                progressTween.OnComplete(PlayPulse);

            _tween = progressTween;
        }

        void PlayPulse()
        {
            _tween = _fxGroup
                .DOFade(PULSE_MIN_ALPHA, PULSE_HALF_PERIOD)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        void KillTween()
        {
            if (null == _tween)
                return;

            _tween.Kill();
            _tween = null;
        }

        float GetProgress()
        {
            return _progress;
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
